import 'dart:async';
import 'dart:io';
import 'package:path/path.dart' as path;

import 'package:code_builder/code_builder.dart';
import 'package:dart_style/dart_style.dart';
import 'package:recase/recase.dart';

import '../model/local_entity_declaration.dart';
import '../model/local_entity_property.dart';
import '../model/local_model.dart';
import '../model/local_model_entity.dart';
import '../scheme_model/api_entity.dart';
import '../scheme_model/api_entity_declaration.dart';
import '../scheme_model/api_entity_property.dart';
import '../scheme_model/api_enum.dart';
import '../scheme_model/api_enum_member.dart';
import '../scheme_model/api_model.dart';
import '../utils/console_utilities.dart';
import '../utils/generation_utilities.dart';
import 'config.dart';
import 'generator.dart';

FutureOr<LocalModel> generateModel(String libraryPath, ApiModel model, List<LocalEntityDeclaration> modelDeclarations) {
  print("------------------------------ GENERATING API MODEL ------------------------------");

  String modelFolderPath = path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, MODEL_FOLDER_NAME);
  Directory modelFolder = new Directory(modelFolderPath);
  if (modelFolder.existsSync()) {
    for (var folderEntity in modelFolder.listSync(followLinks: false)) {
      if (folderEntity.statSync().type == FileSystemEntityType.directory && path.basename(folderEntity.path) == LOCAL_MODEL_FOLDER_NAME) {
        continue;
      }

      folderEntity.deleteSync(recursive: true);
    }
  }

  ConsoleUtilities.info("Exports file:");

  Library exportsLib = _generateExportsFile(modelDeclarations);
  ConsoleUtilities.info("|--Code graph is generated");

  String exportsLibContent = "${exportsLib.accept(codeEmitter)}".insertGeneratedFileHeader();
  ConsoleUtilities.info("|--File content is generated"); 
  
  File exportsFile = File(path.join(libraryPath, LIBRARY_FOLDER, "$MODEL_FOLDER_NAME.dart"));
  if (!exportsFile.existsSync()) {
    exportsFile.createSync(recursive: true);
  }
  exportsFile.writeAsStringSync(codeFormatter.format(exportsLibContent));

  LocalModel localModel = new LocalModel();
  for (ApiEntity entity in model.entities) {
    LocalEntityDeclaration declaration = modelDeclarations[entity.id - 1];

    ConsoleUtilities.info("${declaration.name} | ${declaration.kind.toString()} | ${declaration.localPath}:");
    LocalModelEntity modelEntity = _generateEntity(entity, modelDeclarations);
    ConsoleUtilities.info("|--Code graph is generated");

    String entityClassContents = "${modelEntity.entityImplementation.accept(codeEmitter)}".insertGeneratedFileHeader();
    ConsoleUtilities.info("|--File content is generated"); 

    File outputFile = File(path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, MODEL_FOLDER_NAME, declaration.localPath));
    if (!outputFile.existsSync()) {
      outputFile.createSync(recursive: true);
    }

    outputFile.writeAsStringSync(codeFormatter.format(entityClassContents));
    localModel.entities.add(modelEntity);
  }

  for (ApiEnum entity in model.enums) {
    LocalEntityDeclaration declaration = modelDeclarations[entity.id - 1];

    ConsoleUtilities.info("${declaration.name} | ${declaration.kind.toString()} | ${declaration.localPath}:");
    Library entityEnum = _generateEnum(entity, modelDeclarations);
    ConsoleUtilities.info("|--Code graph is generated");

    String entityClassContents = "${entityEnum.accept(codeEmitter)}".insertGeneratedFileHeader();
    ConsoleUtilities.info("|--File content is generated"); 

    File outputFile = File(path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, MODEL_FOLDER_NAME, declaration.localPath));
    if (!outputFile.existsSync()) {
      outputFile.createSync(recursive: true); 
    }

    outputFile.writeAsStringSync(codeFormatter.format(entityClassContents));
  }

  return localModel;
}

Library _generateExportsFile(List<LocalEntityDeclaration> modelDeclarations) {
  return Library((library) => library.directives.addAll(
    modelDeclarations.map((d) =>  Directive.export(path.join(LIBRARY_SOURCE_FOLDER_NAME, MODEL_FOLDER_NAME, d.localPath).replaceAll(r'\', r'\\')))
  ));
}

LocalModelEntity _generateEntity(ApiEntity entity, List<LocalEntityDeclaration> modelDeclarations) {
  LocalEntityDeclaration declaration = modelDeclarations[entity.id - 1];

  List<LocalEntityProperty> localProperties = [];
  Library entityImplementation = new Library((library) => library
    ..addModelImports()
    ..directives.add(new Directive.part("${declaration.name.snakeCase}.g.dart"))
    ..body.add(
      new Class((entityClass) {
        entityClass.name = declaration.name;
        entityClass.docs.addAll(entity.docs.toDartDocs());
        entityClass.annotations.add(new InvokeExpression.newOf(refer(JSON_SERIALIZABLE_ANNOTATION_NAME), []));

        if (declaration.kind == ApiEntityKind.Response) {
          entityClass.extend = refer(API_RESPONSE_MODEL_NAME);
        } else {
          entityClass.implements.add(new TypeReference((serializableRef) {
            serializableRef.symbol = SERIALIZABLE_CLASS_NAME;
            serializableRef.types.add(refer(declaration.name));
          }));
        }

        for (ApiEntityProperty property in entity.properties) {
          entityClass.fields.add(new Field((entityField) {
            entityField.name = property.name;
            entityField.docs.addAll(property.docs.toDartDocs());
            entityField.type = new TypeReference((tB) => property.type.fillBuilder(tB, modelDeclarations));
            entityField.annotations.add(InvokeExpression.newOf(
              refer(JSON_KEY_ANNOTATION_NAME), 
              [], 
              { "name": new CodeExpression(new Code('"${property.jsonName}"')) }));

            if ((declaration.kind == ApiEntityKind.Response || declaration.name == API_RESPONSE_MODEL_NAME) && !property.type.nullable) {
              entityField.late = true;
            }

            localProperties.add(new LocalEntityProperty(entityField.type as TypeReference, property));
          }));
        }
        
        entityClass.constructors.add(new Constructor((constructor) {
          if (declaration.kind == ApiEntityKind.Response || declaration.name == API_RESPONSE_MODEL_NAME) {
            constructor.initializers.add(new Code("super()"));
            return;
          }

          for (ApiEntityProperty property in entity.properties) {
            Parameter constructorParameter = new Parameter((parameter) {
              parameter.toThis = true;
              parameter.name = property.name; 
            });

            if (!property.type.nullable) {
              constructor.requiredParameters.add(constructorParameter);
            } else {
              constructor.optionalParameters.add(constructorParameter);
            }
          } 
        }));

        entityClass.constructors.add(new Constructor((fromJsonConstructor) {
          fromJsonConstructor.name = "fromJson";
          fromJsonConstructor.factory = true;
          fromJsonConstructor.lambda = true;
          fromJsonConstructor.annotations.add(refer("override"));
          fromJsonConstructor.body = refer(r"_$" + "${declaration.name}FromJson").call([ refer("json") ]).code;
          fromJsonConstructor.requiredParameters.add(new Parameter((factoryParameter) {
            factoryParameter.name = "json";
            factoryParameter.type = TypeReference((jsonType) {
              jsonType.symbol = "Map";
              jsonType.types..add(refer("String"))..add(refer("dynamic"));
            });
          }));
        }));

        entityClass.methods.add(new Method((toJsonMethod) {
          toJsonMethod.name = "toJson";
          toJsonMethod.lambda = true;
          toJsonMethod.annotations.add(refer("override"));
          toJsonMethod.body = refer(r"_$" + "${declaration.name}ToJson").call([ refer("this") ]).code;
          toJsonMethod.returns = TypeReference((jsonType) {
            jsonType.symbol = "Map";
            jsonType.types..add(refer("String"))..add(refer("dynamic"));
          });
        })); 
      })
    )
  );

  return new LocalModelEntity(entityImplementation, declaration, localProperties);
}

Library _generateEnum(ApiEnum enumEntity, List<LocalEntityDeclaration> modelDeclarations) {
  LocalEntityDeclaration declaration = modelDeclarations[enumEntity.id - 1];

  return new Library((library) {
    library.body.add(
      new Enum((entityEnum) {
        entityEnum.name = declaration.name;
        entityEnum.docs.addAll(enumEntity.docs.toDartDocs());
        
        for (ApiEnumMember member in enumEntity.members) {
          entityEnum.values.add(new EnumValue((enumMember) {
            enumMember.docs.addAll(member.docs.toDartDocs());
            enumMember.name = member.name;
            enumMember.annotations.add(new InvokeExpression.newOf(
              refer(JSON_VALUE_ANNOTATION_NAME, JSON_ANNOTATIONS_URL), 
              [ new CodeExpression(new Code('"${member.value}"')) ]
            ));
          }));
        }
      })
    );
  });
}