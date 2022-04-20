import 'package:code_builder/code_builder.dart';

import '../scheme_model/api_entity_property.dart';

class LocalEntityProperty {
  TypeReference type;
  ApiEntityProperty initialProperty;

  LocalEntityProperty(this.type, this.initialProperty);
}