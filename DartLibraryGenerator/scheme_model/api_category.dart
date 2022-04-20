import 'api_documentation_node.dart';
import 'api_method.dart';

class ApiCategory {
  ApiCategory({
    required this.name,
    required this.docs,
    required this.methods,
  });

  String name;
  List<ApiDocumentationNode> docs;
  List<ApiMethod> methods;

  factory ApiCategory.fromJson(Map<String, dynamic> json) => ApiCategory(
    name: json["name"],
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
    methods: List<ApiMethod>.from(json["methods"].map((x) => ApiMethod.fromJson(x))),
  );

  Map<String, dynamic> toJson() => {
    "name": name,
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
    "methods": List<dynamic>.from(methods.map((x) => x.toJson())),
  };
}