import 'package:path/path.dart' as path;
import 'package:recase/recase.dart';
import '../scheme_model/api_entity_declaration.dart';

class LocalEntityDeclaration {
  int id;       
  String name;     
  ApiEntityKind kind;
  late String localPath;

  LocalEntityDeclaration(ApiEntityDeclaration declaration) 
    : id = declaration.id, 
      name = declaration.name, 
      kind = declaration.kind,
      localPath = _toLocalPath(declaration.path);

  static String _toLocalPath(String entityPath) {
    String result = "";

    List<String> splittedPath = entityPath.split(r'\');
    for (var i = 0; i < splittedPath.length; i++) {
      if (i != splittedPath.length - 1) {
        result = path.join(result, splittedPath[i].snakeCase);
      } else {
        result = path.join(result, "${path.withoutExtension(splittedPath[i]).snakeCase}.dart");
      }
    }

    return result;
  }
}