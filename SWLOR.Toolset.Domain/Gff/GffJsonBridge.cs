using Nwn.Authoring.Documents.NimGff;
using SWLOR.NWN.Formats.Gff;
using NativeDocument = Nwn.Formats.Gff.GffDocument;
using NativeField = Nwn.Formats.Gff.GffField;
using NativeFieldType = Nwn.Formats.Gff.GffFieldType;
using NativeStruct = Nwn.Formats.Gff.GffStruct;
using NativeLocString = Nwn.Formats.Gff.GffLocString;
using NativeLocStringEntry = Nwn.Formats.Gff.GffLocStringEntry;

namespace SWLOR.Toolset.Domain.Gff;

/// <summary>Adapts the existing read-only SWLOR format model to the shared native JSON bridge.</summary>
public static class GffJsonBridge
{
    public static JsonGffDocument ToJsonDocument(GffFile file) => ToJsonDocument(file, false);

    public static JsonGffDocument ToJsonDocument(GffFile file, bool encodeTextAsUtf8)
    {
        ArgumentNullException.ThrowIfNull(file);
        return NativeGffBridge.ToJsonDocument(new NativeDocument
        {
            FileType = file.FileType,
            FileVersion = file.FileVersion,
            Root = ConvertStruct(file.RootStruct)
        }, encodeTextAsUtf8);
    }

    private static NativeStruct ConvertStruct(GffStruct source)
    {
        var result = new NativeStruct(source.Type);
        foreach (var field in source.Fields)
        {
            var type = (NativeFieldType)field.Type;
            var value = type switch
            {
                NativeFieldType.Struct => ConvertStruct((GffStruct)field.Value!),
                NativeFieldType.List => ((GffList)field.Value!).Elements.Select(ConvertStruct).ToArray(),
                NativeFieldType.LocString => ConvertLocString((CExoLocString)field.Value!),
                _ => field.Value ?? throw new FormatException("A native GFF field has no value.")
            };
            result.Add(new NativeField { Label = field.Label, Type = type, Value = value });
        }
        return result;
    }

    private static NativeLocString ConvertLocString(CExoLocString source) => new()
    {
        StringRef = source.StrRef,
        Strings = source.LocalizedStrings.Select(pair => new NativeLocStringEntry(pair.Key, pair.Value)).ToArray()
    };
}