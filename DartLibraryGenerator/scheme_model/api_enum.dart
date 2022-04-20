
import 'api_documentation_node.dart';
import 'api_enum_member.dart';

class ApiEnum {
  ApiEnum({
    required this.id,
    required this.docs,
    required this.members,
  });

  int id;
  List<ApiDocumentationNode> docs;
  List<ApiEnumMember> members;

  factory ApiEnum.fromJson(Map<String, dynamic> json) => ApiEnum(
    id: json["id"],
    docs: List<ApiDocumentationNode>.from(json["docs"].map((x) => ApiDocumentationNode.fromJson(x))),
    members: List<ApiEnumMember>.from(json["members"].map((x) => ApiEnumMember.fromJson(x))),
  );

  Map<String, dynamic> toJson() => {
    "id": id,
    "docs": List<dynamic>.from(docs.map((x) => x.toJson())),
    "members": List<dynamic>.from(members.map((x) => x.toJson())),
  };
}