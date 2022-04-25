import 'dart:async';
import 'dart:io';
import 'package:path/path.dart' as path;

import 'package:code_builder/code_builder.dart';
import 'package:recase/recase.dart';

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
  
  List<String> categoryNames = [];
  for (ApiCategory category in categories) {
    String categoryName = "${category.name.pascalCase}Category";
    categoryNames.add(categoryName);

    ConsoleUtilities.info("$categoryName:");
    Library categoryAbstraction = await _generateCategoryAbstraction(categoryName, category, model);
    ConsoleUtilities.info("|--Abstraction code graph is generated");

    String abstractionContents = "${categoryAbstraction.accept(codeEmitter)}".insertGeneratedFileHeader();
    ConsoleUtilities.info("|--Abstraction file content is generated"); 

    Library categoryImplementation = await _generateCategoryImplementation(categoryName, category, categoryAbstraction, model);
    ConsoleUtilities.info("|--Implementation code graph is generated");

    String implementationContents = "${categoryImplementation.accept(codeEmitter)}".insertGeneratedFileHeader();
    ConsoleUtilities.info("|--Implementation file content is generated"); 

    File abstractionFile = File(path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, CATEGORIES_ABSTRACTION_FOLDER_NAME, "i_${categoryName.snakeCase}.dart"));
    if (!abstractionFile.existsSync()) {
      abstractionFile.createSync(recursive: true);
    }

    File implementationFile = File(path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, "${categoryName.snakeCase}.dart"));
    if (!implementationFile.existsSync()) {
      implementationFile.createSync(recursive: true);
    }

    abstractionFile.writeAsStringSync(codeFormatter.format(abstractionContents));  
    implementationFile.writeAsStringSync(codeFormatter.format(implementationContents));  
  }

  ConsoleUtilities.info("Exports file:");

  Library exportsLib = await _generateExportsFile(categoryNames);
  ConsoleUtilities.info("|--Code graph is generated");

  String exportsLibContent = "${exportsLib.accept(codeEmitter)}".insertGeneratedFileHeader();
  ConsoleUtilities.info("|--File content is generated"); 

  File exportsFile = File(path.join(libraryPath, LIBRARY_FOLDER, "$CATEGORIES_FOLDER_NAME.dart"));
  if (!exportsFile.existsSync()) {
    exportsFile.createSync(recursive: true);
  }
  exportsFile.writeAsStringSync(codeFormatter.format(exportsLibContent));
}

FutureOr<Library> _generateExportsFile(List<String> categoryNames) {
  return Library((library) {
    for (String categoryName in categoryNames) {
      library.directives.add(Directive.export(path.join(LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, "${categoryName.snakeCase}.dart").replaceAll(r'\', r'\\')));
      library.directives.add(Directive.export(path.join(LIBRARY_SOURCE_FOLDER_NAME, CATEGORIES_FOLDER_NAME, CATEGORIES_ABSTRACTION_FOLDER_NAME, "i_${categoryName.snakeCase}.dart").replaceAll(r'\', r'\\')));
    }
  });
}

FutureOr<Library> _generateCategoryAbstraction(String categoryName, ApiCategory category, LocalModel model) async {
  return new Library((library) => library.body.add(
    new Class((categoryClass) {
      categoryClass.name = "I$categoryName";
      categoryClass.abstract = true;

      for (ApiMethod method in category.methods) {
        categoryClass.methods.add(new Method((categoryMethod) {
          categoryMethod.name = method.name;
          categoryMethod.docs.addAll(method.docs.toDartDocs());

          if (method.accessibleFrom == MethodAccessPolicy.Controller) {
            categoryMethod.annotations.add(InvokeExpression.newOf(refer("ControllerOnly", "package:dart_library_generator/annotations.dart"), []));
          } else if (method.accessibleFrom == MethodAccessPolicy.Device) {
            categoryMethod.annotations.add(InvokeExpression.newOf(refer("DeviceOnly", "package:dart_library_generator/annotations.dart"), []));
          }

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
                  innerType.url = MODEL_EXPORTS_URL;
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

FutureOr<Library> _generateCategoryImplementation(String categoryName, ApiCategory category, Library abstraction, LocalModel model) {
  return new Library((library) {
    library.body.add(new Class((categoryClass) {
      categoryClass.name = categoryName;
      categoryClass.implements.add(refer("I$categoryName", "package:$LIBRARY_PACKAGE_NAME/$CATEGORIES_FOLDER_NAME.dart"));
      categoryClass.fields.add(new Field((coreInstance) {
        coreInstance.name = "_api";
        coreInstance.modifier = FieldModifier.final$;
        coreInstance.type = refer("I$LIBRARY_CORE_CLASS", "package:$LIBRARY_PACKAGE_NAME/$CORE_LIBRARY_FILE_NAME.dart");
      }));

      categoryClass.constructors.add(new Constructor((constructor) {
        constructor.requiredParameters.add(new Parameter((parameter) {
          parameter.name = "_api";
          parameter.toThis = true; 
        }));
      }));

      Iterable<Method> abstractionMethods = (abstraction.body[0] as Class).methods;
      for (var i = 0; i < category.methods.length; i++) {
        Method abstractionMethod = abstractionMethods.elementAt(i);
        ApiMethod method = category.methods[i];
        
        categoryClass.methods.add(new Method((categoryMethod) {
          categoryMethod.name = abstractionMethod.name;
          categoryMethod.modifier = MethodModifier.async;
          categoryMethod.returns = abstractionMethod.returns;
          categoryMethod.optionalParameters.addAll(abstractionMethod.optionalParameters);
          categoryMethod.requiredParameters.addAll(abstractionMethod.requiredParameters);
          categoryMethod.annotations.add(new CodeExpression(new Code("override")));
          categoryMethod.docs.add("");
          
          categoryMethod.body = new Block((methodBody) {
            if (method.parametersId != null) {
              methodBody.statements.add(_generateParametersInitCode(model[method.parametersId!]));
              methodBody.statements.add(new Code("\t\t/* <auto-generated-safe-area> Code within tag borders shouldn't cause incorrect behavior and will be preserved. */"));
              methodBody.statements.add(new Code("\t\t/* \tTODO: Add parameters validation */"));
              methodBody.statements.add(new Code("\t\t/* </auto-generated-safe-area> */"));
            }
            
            LocalModelEntity responseEntity = model[method.responseId];
            methodBody.statements.add(_generateApiMethodInvocationCode("${category.name}/${method.name}", method, responseEntity));

            if (responseEntity.declaration.name != API_RESPONSE_MODEL_NAME) {
              methodBody.statements.add(new Code("\t\t/* <auto-generated-safe-area> Code within tag borders shouldn't cause incorrect behavior and will be preserved. */"));
              methodBody.statements.add(new Code("\t\t/* \tTODO: Add response validation */"));
              methodBody.statements.add(new Code("\t\t/* </auto-generated-safe-area> */"));
              
              if (responseEntity.properties.length > 1) {
                methodBody.statements.add(refer("response").returned.code);
                return;
              }

              methodBody.statements.add(
                refer("response")
                    .property(responseEntity.properties[0].initialProperty.name)
                    .returned
                    .statement
              );
            }
          });
        }));
      } 
    }));
  });
}

Code _generateParametersInitCode(LocalModelEntity parameters) {
  Reference paramRef = refer(parameters.declaration.name, MODEL_EXPORTS_URL);
  return refer(parameters.declaration.name, MODEL_EXPORTS_URL)
      .newInstance(parameters.properties.map((p) => refer(p.initialProperty.name)))
      .assignFinal("parameters", paramRef)
      .statement;
}

Code _generateApiMethodInvocationCode(String methodPath, ApiMethod method, LocalModelEntity response) {
  String executeMethodName;
  bool parametersIncluded = method.parametersId != null;
  bool responseIncluded = response.declaration.name != API_RESPONSE_MODEL_NAME;
  if (parametersIncluded && responseIncluded) {
    executeMethodName = "executeWithParametersAndResponse";
  } else if (!parametersIncluded && responseIncluded) {
    executeMethodName = "executeWithResponse";
  } else if (parametersIncluded && !responseIncluded) {
    executeMethodName = "executeWithParameters";
  } else {
    executeMethodName = "execute";
  }

  List<Expression> requiredArguments = [ literalString(methodPath, raw: true) ];
  Map<String, Expression> namedArguments = {};  

  if (parametersIncluded) {
    requiredArguments.add(refer("parameters"));
  }

  if (responseIncluded) {
    requiredArguments.add(new Method((responseFactory) {
      responseFactory.lambda = true;
      responseFactory.requiredParameters.add(new Parameter((factoryParameter) {
        factoryParameter.name = "json";
        factoryParameter.type = TypeReference((jsonType) {
          jsonType.symbol = "Map";
          jsonType.types..add(refer("String"))..add(refer("dynamic"));
        });
      }));

      responseFactory.body = InvokeExpression.newOf(
        refer(response.declaration.name, MODEL_EXPORTS_URL), 
        [refer("json")], 
        {}, 
        [], 
        "fromJson"
      ).code; 
    }).closure);
  }
  
  if (method.requireAccessToken) {
    namedArguments["withAccessToken"] = literalBool(true);
  }

  Expression methodCall = refer("_api")
      .property(executeMethodName)
      .call(requiredArguments, namedArguments)
      .awaited;

  if (responseIncluded) {
    return methodCall
        .assignFinal("response", refer(response.declaration.name, MODEL_EXPORTS_URL))
        .statement;
  }

  return methodCall.statement;
}