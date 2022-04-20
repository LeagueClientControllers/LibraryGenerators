import 'api_category.dart';
import 'api_model.dart';

class ApiScheme {
  ApiScheme({
    required this.apiVersion,
    required this.schemeVersion,
    required this.generatedAt,
    required this.categories,
    required this.model,
  });

  final String apiVersion;
  final String schemeVersion;
  final String generatedAt;
  final List<ApiCategory> categories;
  final ApiModel model;

  factory ApiScheme.fromJson(Map<String, dynamic> json) => ApiScheme(
    apiVersion: json["apiVersion"],
    schemeVersion: json["schemeVersion"],
    generatedAt: json["generatedAt"],
    categories: List<ApiCategory>.from(json["categories"].map((x) => ApiCategory.fromJson(x))),
    model: ApiModel.fromJson(json["model"]),
  );

  Map<String, dynamic> toJson() => {
    "apiVersion": apiVersion,
    "schemeVersion": schemeVersion,
    "generatedAt": generatedAt,
    "categories": List<dynamic>.from(categories.map((x) => x.toJson())),
    "model": model.toJson(),
  };
}