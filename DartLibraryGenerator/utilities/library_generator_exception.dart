class LibraryGeneratorException implements Exception {
  String? message;

  LibraryGeneratorException({this.message});

  @override
  String toString() {
    return message ?? "";
  }
}