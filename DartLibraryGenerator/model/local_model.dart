import 'package:collection/collection.dart';

import 'local_model_entity.dart';

class LocalModel {
  List<LocalModelEntity> entities = [];

  LocalModelEntity operator[](int id) {
    return 
      entities.firstWhereOrNull((el) => el.declaration.id == id)  ?? 
      (() => throw new UnsupportedError("Entity with id '$id' is not found in the local model."))();
  }
}