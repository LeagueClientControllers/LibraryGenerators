import '../utilities/json_utilities.dart';

class ApiPropertyType {
  ApiPropertyType({
    this.primitive,
    this.referenceId,
    required this.nullable,
    required this.genericTypeArguments,
  });

  PrimitiveType? primitive;
  int? referenceId;
  bool nullable;
  List<ApiPropertyType> genericTypeArguments;

  factory ApiPropertyType.fromJson(Map<String, dynamic> json) => ApiPropertyType(
    primitive: JsonUtilities.jsonToEnumN(json["primitive"], PrimitiveType.values),
    nullable: json["nullable"],
    genericTypeArguments: List<ApiPropertyType>.from(json["genericTypeArguments"].map((x) => ApiPropertyType.fromJson(x))),
    referenceId: json["referenceId"],
  );

  Map<String, dynamic> toJson() => {
    "primitive": JsonUtilities.enumToJson(primitive),
    "nullable": nullable,
    "genericTypeArguments": List<dynamic>.from(genericTypeArguments.map((x) => x.toJson())),
    "referenceId": referenceId,
  };
}

enum PrimitiveType {
  Number,
  Decimal,
  String,
  Boolean,
  Object,
  Date,
  Array,
  Dictionary
}