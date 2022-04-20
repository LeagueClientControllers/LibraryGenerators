import '../utils/json_utilities.dart';

class ApiEntityDeclaration {
  ApiEntityDeclaration({
    required this.id,
    required this.kind,
    required this.name,
    required this.path,
  });

  final int id;
  final ApiEntityKind kind;
  final String name;
  final String path;

  factory ApiEntityDeclaration.fromJson(Map<String, dynamic> json) => ApiEntityDeclaration(
    id: json["id"],
    kind: JsonUtilities.jsonToEnum(json["kind"], ApiEntityKind.values),
    name: json["name"],
    path: json["path"],
  );

  Map<String, dynamic> toJson() => {
    "id": id,
    "kind": JsonUtilities.enumToJson(kind),
    "name": name,
    "path": path,
  };
}

enum ApiEntityKind {
  Enum,
  Event,
  Simple,
  Response,
  Parameters,
}