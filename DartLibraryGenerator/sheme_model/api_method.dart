import '../utilities/json_utilities.dart';
import 'api_documentation_node.dart';

class ApiMethod {
  ApiMethod({
    this.parametersId,
    this.accessibleFrom,
    required this.name,
    required this.docs,
    required this.responseId,
    required this.requireAccessToken,
  });

  String name;
  List<ApiDocumentationNode> docs;
  int? parametersId;
  int responseId;
  bool requireAccessToken;
  MethodAccessPolicy? accessibleFrom;

  factory ApiMethod.fromJson(Map<String, dynamic> json) => ApiMethod(
    name: json["name"],
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
    parametersId: json["parametersId"],
    responseId: json["responseId"],
    requireAccessToken: json["requireAccessToken"],
    accessibleFrom: JsonUtilities.jsonToEnumN(json["accessibleFrom"], MethodAccessPolicy.values),
  );

  Map<String, dynamic> toJson() => {
    "name": name,
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
    "parametersId": parametersId,
    "responseId": responseId,
    "requireAccessToken": requireAccessToken,
    "accessibleFrom": JsonUtilities.enumToJson(accessibleFrom),
  };
}

enum MethodAccessPolicy {
  Controller,
  Device,
  Both
}