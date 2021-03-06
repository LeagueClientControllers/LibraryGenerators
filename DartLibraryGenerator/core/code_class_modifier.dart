import 'dart:async';
import 'dart:io';

import 'package:analyzer/dart/analysis/utilities.dart';
import 'package:analyzer/dart/ast/ast.dart';
import 'package:code_builder/code_builder.dart';
import 'package:collection/collection.dart';
import 'package:path/path.dart' as path;
import 'package:recase/recase.dart';
import '../model/region.dart';
import '../scheme_model/api_category.dart';
import '../utils/console_utilities.dart';
import '../utils/generation_utilities.dart';
import 'categories_merger.dart';
import 'config.dart';
import 'generator.dart';

FutureOr modifyCore(String libraryPath, List<ApiCategory> categories) async {
  print("");
  print("------------------------------ MODIFYING API CORE ----------------------------------");
  
  final String abstractionPath = path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, "i_${LIBRARY_CORE_CLASS.snakeCase}.dart");
  final String implementationPath = path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, "${LIBRARY_CORE_CLASS.snakeCase}.dart");

  final File abstractionFile = new File(abstractionPath);
  final File implementationFile = new File(implementationPath);

  final String abstractionFileContent = await abstractionFile.readAsString();
  final String implementationFileContent = await implementationFile.readAsString();

  final CompilationUnit abstraction = parseString(content: abstractionFileContent, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("Core abstraction is parsed");
  
  final CompilationUnit implementation = parseString(content: implementationFileContent, featureSet: langFeatureSet).unit;
  ConsoleUtilities.info("Core implementation is parsed");


  ConsoleUtilities.info("Abstraction:");
  String modifiedAbstractionContent = _modifyCoreAbstraction(abstractionFileContent, abstraction, categories);
  await abstractionFile.writeAsString(modifiedAbstractionContent);
  ConsoleUtilities.info("|--Modified code is written");

  ConsoleUtilities.info("Implementation:");
  String modifiedImplementationContent = _modifyCoreImplementation(implementationFileContent, implementation, categories);
  await implementationFile.writeAsString(modifiedImplementationContent);
  ConsoleUtilities.info("|--Modified code is written");
}

String _modifyCoreAbstraction(String content, CompilationUnit abstraction, List<ApiCategory> categories) {
  List<String> tabularContent = content.split("\r\n");
  Region categoriesSection = extractRegions(tabularContent).firstWhere((el) => el.name.startsWith("<auto-generated>"));
  
  tabularContent.removeRange(categoriesSection.startLineIndex, categoriesSection.endLineIndex + 1);
  ConsoleUtilities.info("|--Old categories section is cleared");

  List<String> categoryGetters = [];
  for (ApiCategory category in categories) {
    Method categoryGetter = new Method((getter) {
      getter.type = MethodType.getter;
      getter.returns = refer("I${category.name.pascalCase}$CATEGORY_IDENTIFIER");
      getter.name = category.name;
      getter.docs.addAll(category.docs.toDartDocs());
    });

    categoryGetters.add("${categoryGetter.accept(codeEmitter)}");
  }

  ConsoleUtilities.info("|--New categories section is generated");
  tabularContent.insertAll(categoriesSection.startLineIndex, categoryGetters);
  return codeFormatter.format(tabularContent.join("\r\n"));
}

String _modifyCoreImplementation(String content, CompilationUnit implementation, List<ApiCategory> categories) {
  List<String> tabularContent = content.split("\r\n");
  List<Region> autoGeneratedSections = extractRegions(tabularContent).where((el) => el.name.startsWith("<auto-generated>")).toList(growable: false);
  
  Region categoriesImplementationSection = autoGeneratedSections[0];
  tabularContent.removeRange(categoriesImplementationSection.startLineIndex, categoriesImplementationSection.endLineIndex + 1);
  ConsoleUtilities.info("|--Old categories implementation section is cleared");

  List<String> categoryImplementations = [];
  for (ApiCategory category in categories) {
    Method public = new Method((categoryImpl) {
      categoryImpl.lambda = true;
      categoryImpl.type = MethodType.getter;
      categoryImpl.name = category.name;
      categoryImpl.annotations.add(refer("override"));
      categoryImpl.returns = refer("I${category.name.pascalCase}$CATEGORY_IDENTIFIER");
      categoryImpl.body = refer("_${category.name}").statement;
    });

    Field private = Field((privateCategory) {
      privateCategory.name = "_${category.name}";
      privateCategory.late = true;
      privateCategory.type = refer("I${category.name.pascalCase}$CATEGORY_IDENTIFIER");
    });

    categoryImplementations.add("${public.accept(codeEmitter)}\r\n${private.accept(codeEmitter)}");
  }

  ConsoleUtilities.info("|--New categories implementation section is generated");
  tabularContent.insertAll(categoriesImplementationSection.startLineIndex, categoryImplementations);
  int offset = (categoryImplementations.length - (categoriesImplementationSection.endLineIndex + 1 - categoriesImplementationSection.startLineIndex));
  
  Region categoriesInitializationSection = autoGeneratedSections[1];
  tabularContent.removeRange(categoriesInitializationSection.startLineIndex + offset, categoriesInitializationSection.endLineIndex + 1 + offset);
  ConsoleUtilities.info("|--Old categories initialization section is cleared");

  List<String> categoryInitializations = [];
  for (ApiCategory category in categories) {
    categoryInitializations.add("${
      refer("_${category.name}")
        .assign(refer("${category.name.pascalCase}$CATEGORY_IDENTIFIER").newInstance([ refer("this") ]))
        .statement
        .accept(codeEmitter)}");
  }

  ConsoleUtilities.info("|--New categories initialization section is generated");
  tabularContent.insertAll(categoriesInitializationSection.startLineIndex + offset, categoryInitializations);

  String formattedContent = codeFormatter.format(tabularContent.join("\r\n"));
  List<String> tabularFormatted = formattedContent.split("\r\n");
  for (int i = 0; i < tabularFormatted.length; i++) {
    if (tabularFormatted[i].contains("#endregion")) {
      if (tabularFormatted[i - 1].trim().isEmpty) {
        tabularFormatted.removeAt(i - 1);
        break;
      }
    }
  }

  return codeFormatter.format(tabularFormatted.join("\r\n"));
}

List<Region> extractRegions(List<String> tabularContent) {
  List<Region> result = [];
  for (var i = 0; i < tabularContent.length; i++) {
    String line = tabularContent[i];
    
    int regionIndex = line.indexOf("#region");
    if (regionIndex != -1) {
      Region newRegion = new Region();
      newRegion.startLineIndex = i + 1;
      newRegion.name = line.substring(regionIndex + 8);
      result.add(newRegion);
    }

    if (line.contains("#endregion")) {
      Region lastCompleted = result.lastWhere((r) => !r.completed);
      lastCompleted.endLineIndex = i - 1;
      lastCompleted.completed = true;
    } 
  }

  return result;
}