using System.Text;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Byte-level parser for nwn_gff JSON. Operates on raw bytes rather than decoded text
    /// because void fields embed raw binary (including invalid UTF-8) inside string tokens;
    /// scalar tokens are captured verbatim so serialization reproduces them exactly.
    /// </summary>
    public static class GffJsonReader
    {
        public static JsonGffDocument Read(byte[] content)
        {
            var scanner = new Scanner(content);

            scanner.SkipWhitespace();
            scanner.Expect((byte)'{');

            string? dataType = null;
            var root = new JsonGffStruct();

            ParseStructMembers(scanner, root, name =>
            {
                if (name != "__data_type")
                    return false;

                var token = scanner.ReadStringToken();
                dataType = JsonStringCodec.Decode(token);
                return true;
            });

            if (dataType == null)
                throw new FormatException("Document root has no __data_type member.");

            var document = new JsonGffDocument(dataType, root)
            {
                UsesCrLf = DetectCrLf(content),
                HasTrailingNewline = DetectTrailingNewline(content),
                TrailingNewlineUsesCrLf = DetectTrailingCrLf(content)
            };

            scanner.SkipWhitespace();
            if (!scanner.AtEnd)
                throw new FormatException($"Unexpected content after document root at offset {scanner.Position}.");

            return document;
        }

        /// <summary>
        /// Parses the members of an already-opened object ('{' consumed) into a struct.
        /// <paramref name="specialMember"/> lets callers intercept metadata keys such as
        /// __data_type; returning true means the member's value was consumed.
        /// </summary>
        private static void ParseStructMembers(Scanner scanner, JsonGffStruct target, Func<string, bool>? specialMember)
        {
            while (true)
            {
                scanner.SkipWhitespace();
                if (scanner.Peek() == (byte)'}')
                {
                    scanner.Advance();
                    return;
                }

                var name = JsonStringCodec.Decode(scanner.ReadStringToken());
                scanner.SkipWhitespace();
                scanner.Expect((byte)':');
                scanner.SkipWhitespace();

                if (specialMember != null && specialMember(name))
                {
                    // Value already consumed by the interceptor.
                }
                else if (name == "__struct_id")
                {
                    target.RawStructId = scanner.ReadNumberToken();
                }
                else
                {
                    target.AppendParsed(name, ParseField(scanner));
                }

                scanner.SkipWhitespace();
                if (scanner.Peek() == (byte)',')
                {
                    scanner.Advance();
                    continue;
                }

                scanner.Expect((byte)'}');
                return;
            }
        }

        private static JsonGffField ParseField(Scanner scanner)
        {
            scanner.Expect((byte)'{');

            byte[]? rawLocStringId = null;
            byte[]? rawFieldStructId = null;
            GffFieldType? type = null;
            JsonGffField? field = null;

            while (true)
            {
                scanner.SkipWhitespace();
                if (scanner.Peek() == (byte)'}')
                {
                    scanner.Advance();
                    break;
                }

                var key = JsonStringCodec.Decode(scanner.ReadStringToken());
                scanner.SkipWhitespace();
                scanner.Expect((byte)':');
                scanner.SkipWhitespace();

                switch (key)
                {
                    case "id":
                        rawLocStringId = scanner.ReadNumberToken();
                        break;
                    case "__struct_id":
                        rawFieldStructId = scanner.ReadNumberToken();
                        break;
                    case "type":
                        type = GffFieldTypeNames.Parse(JsonStringCodec.Decode(scanner.ReadStringToken()));
                        break;
                    case "value":
                        if (type == null)
                            throw new FormatException($"Field value precedes its type at offset {scanner.Position}.");
                        field = ParseValue(scanner, type.Value);
                        break;
                    default:
                        throw new FormatException($"Unknown field member '{key}' at offset {scanner.Position}.");
                }

                scanner.SkipWhitespace();
                if (scanner.Peek() == (byte)',')
                {
                    scanner.Advance();
                    continue;
                }

                scanner.Expect((byte)'}');
                break;
            }

            if (field == null)
                throw new FormatException($"Field has no value member near offset {scanner.Position}.");

            field.RawLocStringId = rawLocStringId;
            field.RawFieldStructId = rawFieldStructId;
            return field;
        }

        private static JsonGffField ParseValue(Scanner scanner, GffFieldType type)
        {
            switch (type)
            {
                case GffFieldType.Struct:
                {
                    scanner.Expect((byte)'{');
                    var child = new JsonGffStruct();
                    ParseStructMembers(scanner, child, null);
                    return new JsonGffField(GffFieldType.Struct) { Struct = child };
                }
                case GffFieldType.List:
                {
                    scanner.Expect((byte)'[');
                    var elements = new List<JsonGffStruct>();
                    scanner.SkipWhitespace();
                    if (scanner.Peek() == (byte)']')
                    {
                        scanner.Advance();
                    }
                    else
                    {
                        while (true)
                        {
                            scanner.SkipWhitespace();
                            scanner.Expect((byte)'{');
                            var element = new JsonGffStruct();
                            ParseStructMembers(scanner, element, null);
                            elements.Add(element);

                            scanner.SkipWhitespace();
                            if (scanner.Peek() == (byte)',')
                            {
                                scanner.Advance();
                                continue;
                            }

                            scanner.Expect((byte)']');
                            break;
                        }
                    }

                    return new JsonGffField(GffFieldType.List) { Elements = elements };
                }
                case GffFieldType.CExoLocString:
                {
                    scanner.Expect((byte)'{');
                    var entries = new List<LocStringEntry>();
                    while (true)
                    {
                        scanner.SkipWhitespace();
                        if (scanner.Peek() == (byte)'}')
                        {
                            scanner.Advance();
                            break;
                        }

                        var languageKey = JsonStringCodec.Decode(scanner.ReadStringToken());
                        scanner.SkipWhitespace();
                        scanner.Expect((byte)':');
                        scanner.SkipWhitespace();
                        entries.Add(new LocStringEntry(languageKey, scanner.ReadStringToken()));

                        scanner.SkipWhitespace();
                        if (scanner.Peek() == (byte)',')
                        {
                            scanner.Advance();
                            continue;
                        }

                        scanner.Expect((byte)'}');
                        break;
                    }

                    return new JsonGffField(GffFieldType.CExoLocString) { LocStringEntries = entries };
                }
                case GffFieldType.CExoString:
                case GffFieldType.ResRef:
                case GffFieldType.Void:
                    return new JsonGffField(type) { RawValue = scanner.ReadStringToken() };
                default:
                    return new JsonGffField(type) { RawValue = scanner.ReadNumberToken() };
            }
        }

        private static bool DetectCrLf(byte[] content)
        {
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] != (byte)'\n')
                    continue;

                return i > 0 && content[i - 1] == (byte)'\r';
            }

            return true;
        }

        private static bool DetectTrailingNewline(byte[] content)
        {
            return content.Length > 0 && content[^1] == (byte)'\n';
        }

        private static bool DetectTrailingCrLf(byte[] content)
        {
            return content.Length > 1 && content[^1] == (byte)'\n' && content[^2] == (byte)'\r';
        }

        private sealed class Scanner
        {
            private readonly byte[] _content;

            public int Position { get; private set; }

            public bool AtEnd => Position >= _content.Length;

            public Scanner(byte[] content)
            {
                _content = content;
            }

            public byte Peek()
            {
                if (AtEnd)
                    throw new FormatException("Unexpected end of document.");

                return _content[Position];
            }

            public void Advance()
            {
                Position++;
            }

            public void Expect(byte expected)
            {
                if (Peek() != expected)
                    throw new FormatException(
                        $"Expected '{(char)expected}' but found '{(char)Peek()}' at offset {Position}.");

                Position++;
            }

            public void SkipWhitespace()
            {
                while (!AtEnd)
                {
                    var b = _content[Position];
                    if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                        Position++;
                    else
                        break;
                }
            }

            /// <summary>Reads a string token, returning its exact bytes including quotes.</summary>
            public byte[] ReadStringToken()
            {
                if (Peek() != (byte)'"')
                    throw new FormatException($"Expected string token at offset {Position}.");

                var start = Position;
                Position++;
                while (true)
                {
                    if (AtEnd)
                        throw new FormatException("Unterminated string token.");

                    var b = _content[Position];
                    if (b == (byte)'\\')
                    {
                        Position += 2;
                        continue;
                    }

                    Position++;
                    if (b == (byte)'"')
                        break;
                }

                return _content[start..Position];
            }

            /// <summary>Reads a number token, returning its exact bytes.</summary>
            public byte[] ReadNumberToken()
            {
                var start = Position;
                while (!AtEnd)
                {
                    var b = _content[Position];
                    var isNumberByte = b is >= (byte)'0' and <= (byte)'9'
                        or (byte)'-' or (byte)'+' or (byte)'.' or (byte)'e' or (byte)'E';
                    if (!isNumberByte)
                        break;

                    Position++;
                }

                if (Position == start)
                    throw new FormatException($"Expected number token at offset {Position}.");

                return _content[start..Position];
            }
        }
    }
}
