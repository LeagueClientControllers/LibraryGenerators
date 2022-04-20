import 'api_documentation_node.dart';
import 'api_entity_property.dart';

class ApiEntity {
  ApiEntity({
    required this.id,
    required this.properties,
    required this.docs,
  });

  final int id;
  final List<ApiEntityProperty> properties;
  final List<ApiDocumentationNode> docs;

  factory ApiEntity.fromJson(Map<String, dynamic> json) => ApiEntity(
    id: json["id"],
    properties: List<ApiEntityProperty>.from(json["properties"].map((x) => ApiEntityProperty.fromJson(x))),
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
  );

  Map<String, dynamic> toJson() => {
    "id": id,
    "properties": List<dynamic>.from(properties.map((x) => x.toJson())),
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
  };
}