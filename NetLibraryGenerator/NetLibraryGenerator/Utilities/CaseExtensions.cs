namespace NetLibraryGenerator.Utilities
{
    public static class CaseExtensions
    {
        public static string CaseTransform(this IEnumerable<char> input, Case from, Case to)
        {
            return CaseTransform(new string(input.ToArray()), from, to);
        }

        public static string CaseTransform(this string input, Case from, Case to)
        {
            string[] splitted;
            switch (from) {
                case Case.UnderScore:
                    splitted = input.Split('_');
                    break;
                case Case.PascalCase:
                    int indexer = -1;
                    List<string> _splitted = new();
                    foreach (char c in input) {
                        if (char.IsUpper(c)) {
                            indexer++;
                            _splitted.Add(new string(new[] { Char.ToLower(c) }));
                        } else {
                            _splitted[indexer] += c;
                        }
                    }

                    splitted = _splitted.ToArray();
                    break;
                case Case.CamelCase:
                    indexer = 0;
                    _splitted = new List<string>();
                    _splitted.Add("");
                    foreach (char c in input) {
                        if (char.IsUpper(c)) {
                            indexer++;
                            _splitted.Add(new string(new[] { char.ToLower(c) }));
                        } else {
                            _splitted[indexer] += c;
                        }
                    }

                    splitted = _splitted.ToArray();
                    break;
                default:
                    throw new ArgumentException("Input case is not recognizable.");
            }

            switch (to) {
                case Case.UnderScore:
                    return string.Join("_", splitted);
                case Case.PascalCase:
                    return string.Join("", splitted.Select(s => s.FirstCharToUpper()));
                case Case.CamelCase:
                    return $"{splitted[0].ToLower()}{string.Join("", splitted.Skip(1).Select(s => s.FirstCharToUpper()))}";
                default:
                    throw new ArgumentException("Output case is not recognizable.");
            }
        }

        private static string FirstCharToUpper(this string input) =>
            input switch {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty.", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }

    public enum Case
    {
        UnderScore,
        PascalCase,
        CamelCase
    }
}