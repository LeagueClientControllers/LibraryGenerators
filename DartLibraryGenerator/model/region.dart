class Region {
  bool completed = false;
  late String name;
  late int startLineIndex;
  late int endLineIndex;

  @override
  String toString() {
    return "{ $name: [$startLineIndex, $endLineIndex] }";
  }
}