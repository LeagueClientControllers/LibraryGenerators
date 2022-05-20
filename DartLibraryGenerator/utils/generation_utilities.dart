import 'package:code_builder/code_builder.dart';

import '../core/config.dart';
import '../model/local_entity_declaration.dart';
import '../scheme_model/api_documentation_node.dart';
import '../scheme_model/api_property_type.dart';
import 'library_generator_exception.dart';


extension ApiTypeExtension on ApiPropertyType {
  void fillBuilder(TypeReferenceBuilder referenceBuilder, List<LocalEntityDeclaration> modelDeclarations) {
    if (primitive != null) {
      switch (primitive) {
        case PrimitiveType.Number:
          referenceBuilder.symbol = "int";
          break;

        case PrimitiveType.Decimal:
          referenceBuilder.symbol = "Decimal";
          referenceBuilder.url = "package:decimal/decimal.dart"; 
          break;

        case PrimitiveType.String:
          referenceBuilder.symbol = "String";
          break;

        case PrimitiveType.Boolean:
          referenceBuilder.symbol = "bool";
          break;

        case PrimitiveType.Object:
          referenceBuilder.symbol = "Object";
          break;
          
        case PrimitiveType.Date:
          referenceBuilder.symbol = "DateTime";
          break;

        case PrimitiveType.Array:
          referenceBuilder.symbol = "List";  
          break;
        
        case PrimitiveType.Dictionary:
          referenceBuilder.symbol = "Map";
          break;

        default:
          throw LibraryGeneratorException(message: "Primitive type is not supported.");
      }
    } else if (referenceId != null) {
      LocalEntityDeclaration declaration = modelDeclarations[referenceId! - 1];
      referenceBuilder.symbol = declaration.name;
    } else {
      throw LibraryGeneratorException(message: "Property type should be defined by either a primitive type or a model reference.");
    }

    referenceBuilder.isNullable = nullable;
    for (var typeArgument in genericTypeArguments) {
      referenceBuilder.types.add(TypeReference((b) => typeArgument.fillBuilder(b, modelDeclarations)));
    }
  }
}

extension ApiDocsExtension on List<ApiDocumentationNode> {
  List<String> toDartDocs() {
    List<String> result = List.empty(growable: true);

    String combined = ""; 
    for (ApiDocumentationNode node in this) {
      if (node.isReference) {
        combined += "[${node.text}]";
      } else {
        combined += node.text;
      }
    }
    
    for (var line in combined.split("\r\n")) {
      result.add("/// $line");
    }

    return result;
  }
}

extension ImportExtension on LibraryBuilder {
  void addModelImports() {
    directives
        ..add(Directive.import(MODEL_EXPORTS_URL))
        ..add(Directive.import(JSON_ANNOTATIONS_URL))
        ..add(Directive.import(SERIALIZABLE_CLASS_URL))
        ..add(Directive.import("package:dart_library_generator/utilities.dart"))
        ..add(Directive.import("package:decimal/decimal.dart"));
  }

  void addEnumImports() {
    directives.add(Directive.import(JSON_ANNOTATIONS_URL));
  }

  void addCategoryAbstractionImports() {
    directives
        ..add(Directive.import("dart:async"))
        ..add(Directive.import("package:dart_library_generator/utilities.dart"))
        ..add(Directive.import(MODEL_EXPORTS_URL));
  }

  void addCategoryImplementationImports() {
    directives
        ..add(Directive.import("dart:async"))
        ..add(Directive.import("package:$LIBRARY_PACKAGE_NAME/exceptions.dart"))
        ..add(Directive.import(CORE_EXPORTS_URL))
        ..add(Directive.import(MODEL_EXPORTS_URL))
        ..add(Directive.import(CATEGORIES_EXPORTS_URL));
  }

  void addEventsHandlerImports() {
    directives
        ..add(Directive.import("dart:async"))
        ..add(Directive.import("package:$LIBRARY_PACKAGE_NAME/$LIBRARY_SOURCE_FOLDER_NAME/$MODEL_FOLDER_NAME/local/event_message.dart"))
        ..add(Directive.import(MODEL_EXPORTS_URL));
  }
} 

extension GeneratedFileHeader on String {
  String insertGeneratedFileHeader() {
    String header = 
          "//------------------------------------------------------------------------------" 
      "\r\n// <auto-generated>"
      "\r\n//     This code was generated by a tool."
      "\r\n//"
      "\r\n//     Changes to this file may cause incorrect behavior and will be lost if"
      "\r\n//     the code is regenerated."
      "\r\n// </auto-generated>"
      "\r\n//------------------------------------------------------------------------------";

    return "$header\r\n\r\n$this";
  }
}
