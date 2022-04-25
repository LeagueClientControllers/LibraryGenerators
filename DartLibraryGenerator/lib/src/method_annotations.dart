import 'package:meta/meta_meta.dart';

/// Annotates method as a device only method.
/// 
/// Marks method as a callable only from a device,
/// calling the method from a client controller will throw an exception
/// about access token used to execute the method.
@Target({TargetKind.method})
class DeviceOnly {
  const DeviceOnly();
}

/// Annotates method as a controller only method.
/// 
/// Marks method as a callable only from a client controller,
/// calling the method from a device will throw an exception
/// about access token used to execute the method.
@Target({TargetKind.method})
class ControllerOnly {
  const ControllerOnly();
}