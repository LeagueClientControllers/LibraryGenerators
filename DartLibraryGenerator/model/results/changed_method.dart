class ChangedMethod {
    ChangedMethod({
        required this.category,
        required this.name,
        required this.oldSignature,
        required this.newSignature,
    });

    final String category;
    final String name;
    final String oldSignature;
    final String newSignature;

    factory ChangedMethod.fromJson(Map<String, dynamic> json) => ChangedMethod(
        category: json["category"],
        name: json["name"],
        oldSignature: json["oldSignature"],
        newSignature: json["newSignature"],
    );

    Map<String, dynamic> toJson() => {
        "category": category,
        "name": name,
        "oldSignature": oldSignature,
        "newSignature": newSignature,
    };
}