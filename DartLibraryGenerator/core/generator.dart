import 'dart:async';
import 'dart:io';

import 'package:analyzer/dart/analysis/features.dart';
import 'package:code_builder/code_builder.dart';
import 'package:dart_style/dart_style.dart';
import 'package:pub_semver/pub_semver.dart';

import '../model/local_category.dart';
import '../model/local_entity_declaration.dart';
import '../model/local_model.dart';
import '../scheme_model/api_scheme.dart';
import 'categories_generator.dart';
import 'code_class_modifier.dart';
import 'events_generator.dart';
import 'model_generator.dart';

DartEmitter get codeEmitter => DartEmitter.scoped(orderDirectives: true, useNullSafetySyntax: true);
DartFormatter get codeFormatter => 
    DartFormatter(lineEnding: "\r\n", pageWidth: 140, fixes: [
  StyleFix.docComments,
  StyleFix.functionTypedefs,
  StyleFix.namedDefaultSeparator,
  StyleFix.optionalConst,
  StyleFix.singleCascadeStatements,
]);

final langFeatureSet = FeatureSet.fromEnableFlags2(
  sdkLanguageVersion: Version(2, 17, 0),
  flags: [
    'enhanced-enums',
    'named-arguments-anywhere',
    'super-parameters',
  ],
);

FutureOr generateLibrary(String libraryPath, ApiScheme scheme) async {
  List<LocalEntityDeclaration> modelDeclarations = 
      List.generate(scheme.model.declarations.length, (i) => LocalEntityDeclaration(scheme.model.declarations[i]));

  LocalModel model = await generateModel(libraryPath, scheme.model, modelDeclarations);
  
  await generateCategories(libraryPath, scheme.categories, model);

  await modifyCore(libraryPath, scheme.categories);

  await generateEventsHandler(libraryPath, model);
}