import 'api_documentation_node.dart';
import 'api_property_type.dart';

class ApiEntityProperty {
  ApiEntityProperty({
    required this.name,
    required this.jsonName,
    required this.docs,
    required this.type,
  });

  String name;
  String jsonName;
  List<ApiDocumentationNode> docs;
  ApiPropertyType type;

  factory ApiEntityProperty.fromJson(Map<String, dynamic> json) => ApiEntityProperty(
    name: json["name"],
    jsonName: json["jsonName"],
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
    type: ApiPropertyType.fromJson(json["type"]),
  );

  Map<String, dynamic> toJson() => {
    "name": name,
    "jsonName": jsonName,
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
    "type": type.toJson(),
  };
}