import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:args/args.dart';
import 'package:collection/collection.dart';
import 'package:darq/darq.dart';
import 'package:pubspec_parse/pubspec_parse.dart';
import 'package:path/path.dart' as path;

import '../core/config.dart';
import '../core/generator.dart';
import '../model/results/generation_results.dart';
import '../scheme_model/api_scheme.dart';
import '../utils/console_utilities.dart';
import '../utils/library_generator_exception.dart';

Future main(List<String> args) async {
  try {
    await handledArea(args);
  } on Error catch (e) {
    String firstStackMessage = e.stackTrace.toString()
        .replaceAll("#0", "")
        .replaceAll(new RegExp(r"\s+"), " ")
        .iterableRunes
        .takeWhile((v) => v != "#")
        .join("");

    ConsoleUtilities.error("$e Occurred at$firstStackMessage");
  } on LibraryGeneratorException catch (e) {
    ConsoleUtilities.error(e.toString());
  }
}

FutureOr handledArea(List<String> args) async {
  ArgParser parser = new ArgParser()
    ..addOption("scheme",  abbr: "s",  defaultsTo: "", help: "Path to API scheme.")
    ..addOption("library", abbr: "l",  defaultsTo: "", help: "Path to the LarcApiDart project folder.")
    ..addOption("output",  abbr: "o",  defaultsTo: "", help: "Path to an output json file.")
    ..addFlag(  "help",    abbr: "h")
    ..addFlag(  "version", abbr: "v");

  ArgResults? arguments;
  try {
    arguments = parser.parse(args);
  } on ArgParserException catch (e) {
    print("ERROR:");
    print("  ${e.message}\n");
    printHelp();
    return;
  }
  
  String schemePath = arguments["scheme"].toString();
  String libraryPath = arguments["library"].toString();
  String outputPath = arguments["output"].toString();

  if (arguments["help"]) { 
    printHelp();
    return;
  }
  
  if (arguments["version"]) {
    print("Version: ${Pubspec.parse(
      path.join(Platform.script.toFilePath(windows: Platform.isWindows), "../pubspec.yaml")).version}");
    return;
  }

  if (schemePath == "") { 
    printArgsError("Required option 's, scheme' is missing.");
    return;
  }

  if (libraryPath == "") { 
    printArgsError("Required option 'l, library' is missing.");
    return;
  }

  if (outputPath == "") { 
    printArgsError("Required option 'o, output' is missing.");
    return;
  }
  
  File schemeFile = new File(schemePath);
  Directory libraryDirectory = new Directory(libraryPath);
  File outputFile = new File(outputPath);

  if (!await schemeFile.exists()) { 
    throw new UnsupportedError("API scheme path should be a valid path to an existing file.");
  }

  if (!await libraryDirectory.exists()) { 
    throw new UnsupportedError("Library path should be a valid path to an existing directory.");
  }

  if (!await outputFile.parent.exists()) { 
    throw new UnsupportedError("Json output directory should be a valid path to an existing directory.");
  }

  ConsoleUtilities.info("Parsing API scheme...");

  ApiScheme scheme;
  try {
     scheme = ApiScheme.fromJson(jsonDecode(schemeFile.readAsStringSync()));
  } on Exception {
    throw LibraryGeneratorException(message: "Scheme decoding and parsing error.");
  }

  if (scheme.schemeVersion != SUPPORTED_SCHEME_VERSION) {
    throw LibraryGeneratorException(message: "Scheme version is unsupported: $SUPPORTED_SCHEME_VERSION required, ${scheme.schemeVersion} requested.");
  }

  ConsoleUtilities.info("API scheme is parsed");
  print("");
  printHeader(scheme);
  print("");

  await generateLibrary(libraryPath, scheme);
  await outputFile.writeAsString(jsonEncode(GenerationResults.toJson()));
  ConsoleUtilities.info("Generation results are stored at [$outputPath]");
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

void printArgsError(String error) {
  print("ERROR:\n  $error");
  printHelp();
}

void printHelp() {
  print("""
  -s, --scheme     Required. Path to API scheme

  -l, --library    Required. Path to the LarcApiNet project folder

  -o, --output     Required. Path to an output json file

  --help           Display this help screen.

  --version        Display version information.
  """);
}