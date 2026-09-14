using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>Typed view over a .utc (creature blueprint) nwn_gff JSON document.</summary>
    public sealed class UtcDocument : GffDocumentBase
    {
        public UtcDocument(JsonGffDocument document) : base(document)
        {
        }

        public static UtcDocument Load(string path) => new(JsonGffDocument.Load(path));

        public static UtcDocument Parse(byte[] content) => new(JsonGffDocument.Parse(content));

        public string? TemplateResRef
        {
            get => Root.GetStringOrNull("TemplateResRef");
            set => Root.SetString("TemplateResRef", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? Tag
        {
            get => Root.GetStringOrNull("Tag");
            set => Root.SetString("Tag", GffFieldType.CExoString, value ?? string.Empty);
        }

        public LocString FirstName => Root.GetOrAddLocString("FirstName");

        public LocString LastName => Root.GetOrAddLocString("LastName");

        public string? Conversation
        {
            get => Root.GetStringOrNull("Conversation");
            set => Root.SetString("Conversation", GffFieldType.ResRef, value ?? string.Empty);
        }

        public int? FactionID
        {
            get => Root.GetIntOrNull("FactionID");
            set => Root.SetInt("FactionID", GffFieldType.Word, value ?? 0);
        }

        public int? AppearanceType
        {
            get => Root.GetIntOrNull("Appearance_Type");
            set => Root.SetInt("Appearance_Type", GffFieldType.Word, value ?? 0);
        }

        public int? PortraitId
        {
            get => Root.GetIntOrNull("PortraitId");
            set => Root.SetInt("PortraitId", GffFieldType.Word, value ?? 0);
        }

        public string? ScriptAttacked
        {
            get => Root.GetStringOrNull("ScriptAttacked");
            set => Root.SetString("ScriptAttacked", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptDamaged
        {
            get => Root.GetStringOrNull("ScriptDamaged");
            set => Root.SetString("ScriptDamaged", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptDeath
        {
            get => Root.GetStringOrNull("ScriptDeath");
            set => Root.SetString("ScriptDeath", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptDialogue
        {
            get => Root.GetStringOrNull("ScriptDialogue");
            set => Root.SetString("ScriptDialogue", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptDisturbed
        {
            get => Root.GetStringOrNull("ScriptDisturbed");
            set => Root.SetString("ScriptDisturbed", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptHeartbeat
        {
            get => Root.GetStringOrNull("ScriptHeartbeat");
            set => Root.SetString("ScriptHeartbeat", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptSpawn
        {
            get => Root.GetStringOrNull("ScriptSpawn");
            set => Root.SetString("ScriptSpawn", GffFieldType.ResRef, value ?? string.Empty);
        }

        public string? ScriptUserDefine
        {
            get => Root.GetStringOrNull("ScriptUserDefine");
            set => Root.SetString("ScriptUserDefine", GffFieldType.ResRef, value ?? string.Empty);
        }

        public VarTable VarTable => new(Root);
    }
}
