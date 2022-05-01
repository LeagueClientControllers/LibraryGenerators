import 'dart:async';
import 'dart:convert';
import 'dart:io';

import '../core/config.dart';
import '../core/generator.dart';
import '../scheme_model/api_scheme.dart';
import '../utils/console_utilities.dart';
import '../utils/library_generator_exception.dart';

Future main(List<String> arguments) async {
  try {
    await handledArea(arguments);
  } on Exception catch (e) {
    if (e is LibraryGeneratorException) {
      ConsoleUtilities.error(e.toString());
    } else {
      ConsoleUtilities.error("Unhandled exception: ${e.toString()}");
    }
  }
}

FutureOr handledArea(List<String> arguments) async {
  ConsoleUtilities.info("Parsing API scheme...");

  String apiSchemePath = "D:\\Development\\GitHub\\LARC\\WebServer\\api-scheme.json";
  File apiSchemeFile = File(apiSchemePath);
  if (!apiSchemeFile.existsSync()) {
    throw LibraryGeneratorException(message: "File with API scheme not found at provided path.");
  }

  ApiScheme scheme;
  try {
     scheme = ApiScheme.fromJson(jsonDecode(apiSchemeFile.readAsStringSync()));
  } on Exception {
    throw LibraryGeneratorException(message: "Scheme decoding and parsing error.");
  }

  if (scheme.schemeVersion != SUPPORTED_SCHEME_VERSION) {
    throw LibraryGeneratorException(message: "Scheme version is unsupported: $SUPPORTED_SCHEME_VERSION required, ${scheme.schemeVersion} requested.");
  }

  print("");
  printHeader(scheme);
  print("");

  await generateLibrary(scheme);
}

void printHeader(ApiScheme scheme) {
  int maxLineLength = " SCHEME GENERATED: ${scheme.generatedAt} ".length + 7 * 2;

  void writeCenter(String line) {
      String starsString = "";
      int starsCount = (maxLineLength - line.length - 2) ~/ 2;
      for (int i = 0; i < starsCount; i++)
      {
          starsString += "*";
      }

      print("/$starsString $line $starsString/");
  }

  void writeLeft(String line) {
      String starsString = "";
      int starsCount = maxLineLength - line.length - 6;
      for (int i = 0; i < starsCount; i++)
      {
          starsString += "*";
      }

      print("/**** $line $starsString/");
  }

  String starsString = "";
  for (int i = 0; i < maxLineLength; i++) {
      starsString += "*";
  }

  print("/$starsString/");
  writeCenter("DART LARC API LIBRARY GENERATOR");
  print("/$starsString/");
  print("/$starsString/");
  writeLeft("SCHEME VERSION: ${scheme.schemeVersion}");
  writeLeft("API VERSION: ${scheme.apiVersion}");
  writeLeft("SCHEME GENERATED: ${scheme.generatedAt}");
  print("/$starsString/");
}
