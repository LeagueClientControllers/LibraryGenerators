import 'dart:async';
import 'dart:io';
import 'package:analyzer/dart/analysis/features.dart';
import 'package:analyzer/dart/analysis/utilities.dart';
import 'package:analyzer/dart/ast/ast.dart';
import 'package:path/path.dart' as path;
import 'package:pub_semver/pub_semver.dart';
import 'package:recase/recase.dart';

import '../model/local_category.dart';
import '../model/syntactic_method.dart';
import '../scheme_model/api_category.dart';
import '../scheme_model/api_method.dart';
import '../utils/console_utilities.dart';
import '../utils/generation_utilities.dart';
import '../utils/library_generator_exception.dart';
import 'config.dart';
import 'generator.dart';

FutureOr<bool> mergeCategory(String libraryPath, LocalCategory category, ApiCategory apiCategory) async {
  String abstractionsPath    = path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, CATEGORIES_ABSTRACTION_FOLDER_NAME);
  String implementationsPath = path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME);

  File oldAbstractionFile    = new File(path.join(abstractionsPath, "i_${category.name.snakeCase}.dart"));
  File oldImplementationFile = new File(path.join(implementationsPath, "${category.name.snakeCase}.dart"));
  
  if (!await oldAbstractionFile.exists() || !await oldImplementationFile.exists()) {
    return false;
  }

  ConsoleUtilities.info("|--Old abstraction and implementation are found");

  final String oldAbstractionContent = await oldAbstractionFile.readAsString();
  final String oldImplementationContent = await oldImplementationFile.readAsString();

  final CompilationUnit oldAbstraction = parseString(content: oldAbstractionContent, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("|--Old abstraction is parsed");
  
  final CompilationUnit oldImplementation = parseString(content: oldImplementationContent, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("|--Old implementation is parsed");

  final String generatedAbstraction = "${category.abstraction.accept(codeEmitter)}";
  ConsoleUtilities.info("|--New abstraction is generated");
  
  final String generatedImplementation = "${category.implementation.accept(codeEmitter)}";
  ConsoleUtilities.info("|--New implementation is generated");

  final CompilationUnit newAbstraction = parseString(content: generatedAbstraction, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("|--New abstraction is parsed");
  
  final CompilationUnit newImplementation = parseString(content: generatedImplementation, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("|--New implementation is parsed");

  ConsoleUtilities.info("|--Parsing abstraction methods...");
  List<SyntacticMethod> abstractionMethods = _findMethods("I${category.name}", oldAbstraction, newAbstraction, apiCategory.methods);
  List<ApiMethod> changedMethods = _checkAbstractionMethods(oldAbstractionContent, generatedAbstraction, oldAbstraction, newAbstraction, abstractionMethods);

  ConsoleUtilities.info("|--Parsing implementation methods...");
  List<SyntacticMethod> implementationMethods = _findMethods(category.name, oldImplementation, newImplementation, apiCategory.methods);
  String mergedImplementation = _mergeImplementationMethods(oldImplementationContent, generatedImplementation, oldImplementation, newImplementation, implementationMethods, changedMethods);

  await oldAbstractionFile.writeAsString(codeFormatter.format(generatedAbstraction.insertGeneratedFileHeader()));
  ConsoleUtilities.info("|--New abstraction is written");

  await oldImplementationFile.writeAsString(codeFormatter.format(mergedImplementation.insertGeneratedFileHeader()));
  ConsoleUtilities.info("|--Merged implementation is written");
  
  return true;
}

List<SyntacticMethod> _findMethods(String className, CompilationUnit oldUnit, CompilationUnit newUnit, List<ApiMethod> methods) {
  List<MethodDeclaration> oldAbstractionMethods = extractMethods(oldUnit, className); 
  List<MethodDeclaration> newAbstractionMethods = extractMethods(newUnit, className); 

  List<SyntacticMethod> result = [];
  for (MethodDeclaration method in newAbstractionMethods) {
    ApiMethod? apiMethod;
    for (apiMethod in methods) {
      if (apiMethod.name == method.name.name) {
        break;
      }
    }

    if (apiMethod == null) {
      continue;
    }

    MethodDeclaration? oldMethod;
    for (MethodDeclaration _oldMethod in oldAbstractionMethods) {
      if (_oldMethod.name.name == apiMethod.name) {
        oldMethod = _oldMethod;
        break;
      }
    }

    if (oldMethod == null) {
      continue;
    }

    result.add(new SyntacticMethod(apiMethod, oldMethod, method));
  }

  ConsoleUtilities.info("|--Found ${result.length} already implemented methods out of ${newAbstractionMethods.length}");

  return result;
}

List<ApiMethod> _checkAbstractionMethods(String oldAbstractionContent, String newAbstractionContent, CompilationUnit oldAbstraction, CompilationUnit newAbstraction, List<SyntacticMethod> methods) {
  List<ApiMethod> changedMethods = [];
  for (SyntacticMethod method in methods) {
    String oldMethodSyntax = oldAbstractionContent.substring(method.oldMethod.firstTokenAfterCommentAndMetadata.offset, method.oldMethod.end);
    String newMethodSyntax = newAbstractionContent.substring(method.newMethod.firstTokenAfterCommentAndMetadata.offset, method.newMethod.end);

    if (oldMethodSyntax.replaceAll(" ", "") == newMethodSyntax.replaceAll(" ", "")) {
      ConsoleUtilities.info("|--Abstract method '${method.apiMethod.name}' already has correct structure");
      continue;
    }

    changedMethods.add(method.apiMethod);
    ConsoleUtilities.info("|--Abstract method '${method.apiMethod.name}' signature has been changed");
  }

  return changedMethods;
}

String _mergeImplementationMethods(String oldImplementationContent, String newImplementationContent, CompilationUnit oldImplementation, CompilationUnit newImplementation, List<SyntacticMethod> methods, List<ApiMethod> changedMethods) {
  int offset = 0;
  String mergedImplementationContent = newImplementationContent;
  for (SyntacticMethod method in methods) {
    String oldMethodBody = oldImplementationContent.substring(method.oldMethod.body.offset, method.oldMethod.body.end);
    String newMethodBody = newImplementationContent.substring(method.newMethod.body.offset, method.newMethod.body.end);

    if (oldMethodBody.replaceAll(" ", "") == newMethodBody.replaceAll(" ", "")) {
      ConsoleUtilities.info("|--Method '${method.apiMethod.name}' hasn't been changed");
      continue;
    }

    mergedImplementationContent = mergedImplementationContent.replaceRange(method.newMethod.body.offset + offset, 
        method.newMethod.body.end + offset, oldMethodBody);

    for (ApiMethod changedMethod in changedMethods) {
      if (changedMethod.name == method.apiMethod.name) {
        mergedImplementationContent = mergedImplementationContent.replaceRange(method.newMethod.beginToken.offset + offset, 
            method.newMethod.beginToken.offset + offset, "  // TODO: Needs revision due to a new method signature\r\n");
        offset += 57;
        break;
      }
    }

    offset += oldMethodBody.length - newMethodBody.length;
    ConsoleUtilities.info("|--Method '${method.apiMethod.name}' is merged");
  }

  return mergedImplementationContent;
}

List<MethodDeclaration> extractMethods(CompilationUnit unit, String typeName) {
  List<MethodDeclaration> result = [];
  
  ClassDeclaration? target;
  for (CompilationUnitMember member in unit.declarations) {
    if (member is ClassDeclaration && member.name.name == typeName) {
      target = member;
    }
  }

  if (target == null) {
    throw new LibraryGeneratorException(message: "Error while merging categories. Can't extract methods because type with the name '$typeName' not found in compilation unit.");
  }

  for (ClassMember member in target.members) {
    if (member is MethodDeclaration) {
      result.add(member);
    }
  }

  return result;
}