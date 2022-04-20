class ApiDocumentationNode {
  ApiDocumentationNode({
    required this.text,
    required this.isReference,
  });

  String text;
  bool isReference;

  factory ApiDocumentationNode.fromJson(Map<String, dynamic> json) => ApiDocumentationNode(
    text: json["text"],
    isReference: json["isReference"],
  );

  Map<String, dynamic> toJson() => {
    "text": text,
    "isReference": isReference,
  };
}