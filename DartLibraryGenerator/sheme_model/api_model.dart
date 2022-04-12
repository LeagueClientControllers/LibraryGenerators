import 'api_entity.dart';
import 'api_entity_declaration.dart';
import 'api_enum.dart';

class ApiModel {
  ApiModel({
    required this.declarations,
    required this.entities,
    required this.enums,
  });

  List<ApiEntityDeclaration> declarations;
  List<ApiEntity> entities;
  List<ApiEnum> enums;

  factory ApiModel.fromJson(Map<String, dynamic> json) => ApiModel(
    declarations: List<ApiEntityDeclaration>.from(json["declarations"].map((x) => ApiEntityDeclaration.fromJson(x))),
    entities: List<ApiEntity>.from(json["entities"].map((x) => ApiEntity.fromJson(x))),
    enums: List<ApiEnum>.from(json["enums"].map((x) => ApiEnum.fromJson(x))),
  );

  Map<String, dynamic> toJson() => {
    "declarations": List<dynamic>.from(declarations.map((x) => x.toJson())),
    "entities": List<dynamic>.from(entities.map((x) => x.toJson())),
    "enums": List<dynamic>.from(enums.map((x) => x.toJson())),
  };
}