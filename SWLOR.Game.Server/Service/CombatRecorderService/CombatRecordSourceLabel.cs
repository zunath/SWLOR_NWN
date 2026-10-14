namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Human-readable grouping keys for recording summaries.
    /// </summary>
    public static class CombatRecordSourceLabel
    {
        public static string Format(CombatRecordSourceKind kind, string name, int code = 0)
        {
            name = CombatRecordNames.GetSourceName(kind, name, code);
            var hasName = !string.IsNullOrWhiteSpace(name);

            return kind switch
            {
                CombatRecordSourceKind.Ability => hasName ? name : "Unknown ability",
                CombatRecordSourceKind.AbilityCost => hasName ? $"{name} (cost)" : "Ability cost",
                CombatRecordSourceKind.Autoattack => hasName ? $"Autoattack ({name})" : "Autoattack",
                CombatRecordSourceKind.StatusEffect => hasName ? $"{name} (status)" : "Status effect",
                CombatRecordSourceKind.Lifesteal => hasName ? $"Lifesteal ({name})" : "Lifesteal",
                CombatRecordSourceKind.OnKill => "On kill",
                CombatRecordSourceKind.Regeneration => "Natural regeneration",
                CombatRecordSourceKind.Rest => "Rest",
                CombatRecordSourceKind.Item => hasName ? $"Item: {name}" : "Item",
                CombatRecordSourceKind.StatTrigger => hasName ? $"Stat trigger: {name}" : "Stat trigger",
                CombatRecordSourceKind.DamageShare => hasName ? $"Damage share ({name})" : "Damage share",
                CombatRecordSourceKind.Respawn => "Respawn",
                CombatRecordSourceKind.Subdual => "Subdual",
                CombatRecordSourceKind.DungeonMaster => "DM",
                CombatRecordSourceKind.Trap => "Trap",
                CombatRecordSourceKind.Setup => "Setup",
                _ => hasName ? $"Unattributed ({name})" : "Unattributed"
            };
        }
    }
}
