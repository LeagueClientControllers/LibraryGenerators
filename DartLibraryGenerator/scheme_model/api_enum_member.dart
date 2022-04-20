import 'api_documentation_node.dart';

class ApiEnumMember {
  ApiEnumMember({
    required this.name,
    required this.value,
    required this.docs,
  });

  final String name;
  final String value;
  final List<ApiDocumentationNode> docs;

  factory ApiEnumMember.fromJson(Map<String, dynamic> json) => ApiEnumMember(
    name: json["name"],
    value: json["value"],
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
  );

  Map<String, dynamic> toJson() => {
    "name": name,
    "value": value,
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
  };
}