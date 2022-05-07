import 'added_method.dart';
import 'changed_method.dart';

class GenerationResults {
    GenerationResults._();

    static int modelEntitiesCount = 0;
    static int modelEventsCount = 0;
    static int modelResponsesCount = 0;
    static int modelParametersCount = 0;
    static int modelEnumsCount = 0;
    
    static final List<ChangedMethod> changedMethods = [];
    static final List<AddedMethod> addedMethods = [];
    static final List<String> addedCategories = [];
    
    static Map<String, dynamic> toJson() => {
        "changedMethods": List<dynamic>.from(changedMethods.map((x) => x.toJson())),
        "addedMethods": List<dynamic>.from(addedMethods.map((x) => x.toJson())),
        "addedCategories": List<dynamic>.from(addedCategories.map((x) => x)),
        "modelEntitiesCount": modelEntitiesCount,
        "modelEventsCount": modelEventsCount,
        "modelResponsesCount": modelResponsesCount,
        "modelParametersCount": modelParametersCount,
        "modelEnumsCount": modelEnumsCount,
    };
}