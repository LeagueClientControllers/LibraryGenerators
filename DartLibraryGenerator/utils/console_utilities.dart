import 'dart:io';

class ConsoleUtilities {
  static void info(String message) {
    stdout.writeln("[INFO]\t$message");
  }

  static void error(String message) {
    stderr.writeln("[ERROR]\t$message");
  }
}