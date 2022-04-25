import 'package:analyzer/dart/ast/ast.dart';

import '../scheme_model/api_method.dart';

class SyntacticMethod {
  bool changed = false;
  ApiMethod apiMethod;
  MethodDeclaration oldMethod;
  MethodDeclaration newMethod;

  SyntacticMethod(this.apiMethod, this.oldMethod, this.newMethod);
}