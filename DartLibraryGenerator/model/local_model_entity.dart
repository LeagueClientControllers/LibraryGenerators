import 'package:code_builder/code_builder.dart';

import 'local_entity_declaration.dart';
import 'local_entity_property.dart';

class LocalModelEntity {
  Library entityImplementation;
  LocalEntityDeclaration declaration;
  List<LocalEntityProperty> properties;

  LocalModelEntity(this.entityImplementation, this.declaration, this.properties);
}