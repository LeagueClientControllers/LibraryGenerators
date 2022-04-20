import 'dart:async';
import 'dart:io';
import 'package:path/path.dart' as path;

import 'package:code_builder/code_builder.dart';
import 'package:recase/recase.dart';

import '../model/local_category.dart';
import '../model/local_entity_property.dart';
import '../model/local_model.dart';
import '../model/local_model_entity.dart';
import '../scheme_model/api_category.dart';
import '../scheme_model/api_method.dart';
import '../utils/console_utilities.dart';
import '../utils/generation_utilities.dart';
import 'config.dart';
import 'generator.dart';

FutureOr generateCategories(String libraryPath, List<ApiCategory> categories, LocalModel model) async {
  print("");
  print("--------------------------- GENERATING API CATEGORIES ------------------------------");
  
  List<LocalCategory> localCategories = new List.empty(growable: true);
  for (ApiCategory category in categories) {
    String categoryName = "${category.name.pascalCase}Category";

    ConsoleUtilities.info("$categoryName:");
    Library entityClass = await _generateCategoryAbstraction(categoryName, category, model);
    ConsoleUtilities.info("|--Abstraction code graph is generated");

    String entityClassContents = "${entityClass.accept(codeEmitter)}".insertGeneratedFileHeader();
    ConsoleUtilities.info("|--Abstraction file content is generated"); 

    File outputFile = File(path.join(libraryPath, LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, CATEGORIES_ABSTRACTION_FOLDER_NAME, "i_${categoryName.snakeCase}.dart"));
    if (!outputFile.existsSync()) {
      outputFile.createSync(recursive: true);
    }

    outputFile.writeAsStringSync(codeFormatter.format(entityClassContents));  
  }
}

FutureOr<Library> _generateCategoryAbstraction(String categoryName, ApiCategory category, LocalModel model) async {
  return new Library((library) => library.body.add(
    new Class((categoryClass) {
      categoryClass.name = categoryName;
      categoryClass.abstract = true;

      for (ApiMethod method in category.methods) {
        categoryClass.methods.add(new Method((categoryMethod) {
          categoryMethod.name = method.name;
          categoryMethod.docs.addAll(method.docs.toDartDocs());

          LocalModelEntity returnType = model[method.responseId];
          if (returnType.declaration.name == API_RESPONSE_MODEL_NAME) {
            categoryMethod.returns = refer("Future", "dart:async");
          } else {
            if (returnType.properties.length == 1) {
              categoryMethod.returns = new TypeReference((methodReturnType) {
                methodReturnType.symbol = "Future";
                methodReturnType.url = "dart:async";
                methodReturnType.types.add(returnType.properties[0].type);
              });
            } else {
              categoryMethod.returns = new TypeReference((methodReturnType) {
                methodReturnType.symbol = "Future";
                methodReturnType.url = "dart:async";
                methodReturnType.types.add(new TypeReference((innerType) {
                  innerType.symbol = returnType.declaration.name;
                  innerType.url = "package:$LIBRARY_PACKAGE_NAME/$LIBRARY_SOURCE_FOLDER_NAME/$MODEL_FOLDER_NAME/$MODEL_FOLDER_NAME.dart";
                }));
              });
            }
          }

          if (method.parametersId == null) {
            return;
          }

          categoryMethod.docs.add("///");
          for (LocalEntityProperty property in model[method.parametersId!].properties) {
            Iterable<String> propertyDocs = property.initialProperty.docs.toDartDocs().map((e) => e.replaceAll(".", ";").replaceFirst("///", ""));
            if (propertyDocs.length == 1) {
              categoryMethod.docs.add("/// [${property.initialProperty.name}] -${propertyDocs.first}");
            } else {
              categoryMethod.docs.add("/// [${property.initialProperty.name}] -");
              for (String line in propertyDocs) {
                categoryMethod.docs.add("///\t$line");
              }
              categoryMethod.docs.add("///");
            }

            categoryMethod.requiredParameters.add(new Parameter((methodParameter) {
              methodParameter.name = property.initialProperty.name;
              methodParameter.type = property.type;
            }));
          }
        }));
      }
    })
  ));
}