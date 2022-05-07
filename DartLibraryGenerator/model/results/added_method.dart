class AddedMethod {
    AddedMethod({
        required this.category,
        required this.name,
    });

    final String category;
    final String name;

    factory AddedMethod.fromJson(Map<String, dynamic> json) => AddedMethod(
        category: json["category"],
        name: json["name"],
    );

    Map<String, dynamic> toJson() => {
        "category": category,
        "name": name,
    };
}


