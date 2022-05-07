import 'package:code_builder/code_builder.dart';

class LocalCategory {
  final String name;
  final String initialName;
  final Library abstraction;
  final Library implementation;

  LocalCategory(this.name, this.initialName, this.abstraction, this.implementation);
}