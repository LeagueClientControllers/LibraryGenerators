DateTime unixTimestampToDateTime(int timestamp) {
  return DateTime.fromMillisecondsSinceEpoch(timestamp * 1000, isUtc: true);
}

DateTime? unixTimestampToDateTimeNullable(int? timestamp) {
  if (timestamp == null) {
    return null;
  }

  return DateTime.fromMillisecondsSinceEpoch(timestamp * 1000, isUtc: true);
}

int dateTimeToUnixTimestamp(DateTime dateTime) {
  return dateTime.millisecondsSinceEpoch ~/ 1000;
}

int? dateTimeToUnixTimestampNullable(DateTime? dateTime) {
  if (dateTime == null)  {
    return null;
  }

  return dateTime.millisecondsSinceEpoch ~/ 1000;
}