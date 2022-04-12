import 'package:collection/collection.dart';
import 'library_generator_exception.dart';

class JsonUtilities {
  static TEnum jsonToEnum<TEnum>(Object jsonValue, List<TEnum> values) {
    if (jsonValue.runtimeType != String) {
      throw SchemeParsingException("Error when parsing ${TEnum.runtimeType} enum. Json value for the enum should be a string.");
    }

    final TEnum? value = values.where((element) => element.toString() == "${TEnum.runtimeType}.${jsonValue.toString()}").firstOrNull;
    if (value == null) {
      throw SchemeParsingException("Error when parsing ${TEnum.runtimeType} enum. Member '${jsonValue.toString()}' doesn't exists in the enum");
    }

    return value;
  }

  static TEnum? jsonToEnumN<TEnum>(Object? jsonValue, List<TEnum> values) {
    return jsonValue == null ? null : jsonToEnum(jsonValue, values);
  }

  static String? enumToJson<TEnum>(final TEnum? enumValue) {
    if (enumValue == null) {
      return null;
    }

    return enumValue.toString().substring(TEnum.runtimeType.toString().length + 1);
  }
}

class SchemeParsingException extends LibraryGeneratorException {
  SchemeParsingException(String message): super(message: message);
}