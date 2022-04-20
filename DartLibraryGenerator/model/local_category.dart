import 'package:code_builder/code_builder.dart';

class LocalCategory {
  final String name;
  final Library abstraction;
  final Library implementation;

  LocalCategory(this.name, this.abstraction, this.implementation);
}