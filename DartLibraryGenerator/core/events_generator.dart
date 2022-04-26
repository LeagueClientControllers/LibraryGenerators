import 'dart:async';
import 'dart:io';

import 'package:path/path.dart' as path;
import 'package:code_builder/code_builder.dart';
import 'package:recase/recase.dart';

import '../model/local_model.dart';
import '../model/local_model_entity.dart';
import '../scheme_model/api_entity_declaration.dart';
import '../utils/console_utilities.dart';
import '../utils/generation_utilities.dart';
import 'config.dart';
import 'generator.dart';

FutureOr generateEventsHandler(String libraryPath, LocalModel model) async {
  print("");
  print("--------------------------- GENERATING EVENTS HANDLER ------------------------------");

  Library handler = new Library((eventsHandlerLib) => eventsHandlerLib
    ..addEventsHandlerImports()
    ..body.add(new Class((eventsHandler) {
      eventsHandler.name = EVENTS_HANDLER_NAME;
      
      BlockBuilder disposeMethodCodeBuilder = new BlockBuilder();
      BlockBuilder handleMethodCodeBuilder = new BlockBuilder();
      for (LocalModelEntity event in model.entities.where((el) => el.declaration.kind == ApiEntityKind.Event)) {
        String streamName = event.declaration.name.camelCase;
        if (event.properties.isEmpty) {
          return;
        }
        
        eventsHandler.fields.add(new Field((eventStreamController) {
          TypeReference controllerType = TypeReference((streamControllerType) {
            streamControllerType.symbol = "StreamController";
            streamControllerType.types.add(refer(event.declaration.name));
          }); 

          eventStreamController.modifier = FieldModifier.final$;
          eventStreamController.name = "_${streamName}Controller";
          eventStreamController.assignment = controllerType.newInstanceNamed("broadcast", []).code;
          eventStreamController.type = controllerType;
        }));

        eventsHandler.methods.add(new Method((eventStream) {
          eventStream.lambda = true;
          eventStream.type = MethodType.getter;
          eventStream.name = streamName;
          eventStream.body = refer("_${streamName}Controller").property("stream").code;
          eventStream.returns = TypeReference((streamType) {
            streamType.symbol = "Stream";
            streamType.types.add(refer(event.declaration.name));
          }); 
        }));

        handleMethodCodeBuilder.statements.add(new Code(
          """
          if (message.eventType == EventType.${event.declaration.name.replaceFirst("Event", "")}) {
            _${streamName}Controller.add(${event.declaration.name}.fromJson(message.event));
            return;
          }

          """
        ));

        disposeMethodCodeBuilder.statements.add(refer("_${streamName}Controller").property("close").call([]).awaited.statement);
      }
      ConsoleUtilities.info("Streams are generated");

      eventsHandler.methods.add(new Method((disposeMethod) {
        disposeMethod.name = "dispose";
        disposeMethod.modifier = MethodModifier.async;
        disposeMethod.returns = refer("Future");
        disposeMethod.body = disposeMethodCodeBuilder.build();
      }));
      ConsoleUtilities.info("Dispose method is generated");

      eventsHandler.methods.add(new Method((handleMethod) {
        handleMethod.name = "handleMessage";
        handleMethod.body = handleMethodCodeBuilder.build();
        handleMethod.requiredParameters.add(new Parameter((messageParameter) {
          messageParameter.name = "message";
          messageParameter.type = refer("EventMessage");
        }));
      }));
      ConsoleUtilities.info("Handler method is generated");
    }))
  );

  File eventsHandlerFile = new File(path.join(libraryPath, LIBRARY_FOLDER, LIBRARY_SOURCE_FOLDER_NAME, SERVICES_FOLDER_NAME, "${EVENTS_HANDLER_NAME.snakeCase}.dart"));
  if (!await eventsHandlerFile.exists()) {
    await eventsHandlerFile.create(recursive: true);
  }

  String generatedEventsHandler = codeFormatter.format("${handler.accept(codeEmitter)}").insertGeneratedFileHeader();
  ConsoleUtilities.info("Events handler is generated");

  eventsHandlerFile.writeAsString(generatedEventsHandler);
  ConsoleUtilities.info("Events handler is written");
}