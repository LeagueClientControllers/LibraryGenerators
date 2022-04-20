class LibraryGeneratorException implements Exception {
  String? message;

  LibraryGeneratorException({this.message});

  @override
  String toString() {
    return "Error while generating library. ${message ?? ""}";
  }
}