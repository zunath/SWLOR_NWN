using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Writes a recording summary as indented JSON.
    /// </summary>
    public static class CombatRecordSummaryWriter
    {
        public static void Write(Utf8JsonWriter writer, CombatRecordSummary summary)
        {
            writer.WriteStartObject();
            writer.WriteString("label", summary.Label);
            writer.WriteString("area_name", summary.AreaName);
            writer.WriteString("area_resref", summary.AreaResref);
            writer.WriteString("started_utc", summary.StartedUtc.ToString("O"));
            writer.WriteString("stopped_utc", summary.StoppedUtc.ToString("O"));
            writer.WriteNumber("duration_s", Math.Round(summary.DurationSeconds, 3));
            writer.WriteNumber("events", summary.EventCount);

            writer.WriteStartArray("participants");
            foreach (var participant in summary.Participants)
            {
                WriteParticipant(writer, participant);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void WriteParticipant(Utf8JsonWriter writer, CombatRecordParticipantSummary participant)
        {
            writer.WriteStartObject();
            writer.WriteNumber("id", participant.Id);
            if (participant.Name == null)
                writer.WriteNull("name");
            else
                writer.WriteString("name", participant.Name);
            writer.WriteBoolean("player", participant.IsPlayer);
            if (participant.Resref != null)
                writer.WriteString("resref", participant.Resref);

            writer.WriteNumber("seconds_observed", Math.Round(participant.SecondsObserved, 1));
            writer.WriteNumber("seconds_in_combat", Math.Round(participant.SecondsInCombat, 1));
            writer.WriteNumber("deaths", participant.Deaths);
            writer.WriteNumber("kills", participant.Kills);
            writer.WriteNumber("subdued", participant.Subdued);
            writer.WriteNumber("revives_received", participant.RevivesReceived);
            writer.WriteNumber("revives_performed", participant.RevivesPerformed);

            writer.WriteStartObject("ability_uses");
            foreach (var (label, count) in participant.AbilityUses.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteNumber(label, count);
            }
            writer.WriteEndObject();
            WriteNestedCounts(writer, "ability_denials", participant.AbilityDenials);
            WriteNestedCounts(writer, "ability_interruptions", participant.AbilityInterruptions);

            WriteDamage(writer, "damage_dealt", participant.DamageDealt);
            WriteDamage(writer, "damage_taken", participant.DamageTaken);
            WriteHealing(writer, "healing_done", participant.HealingDone);
            WriteHealing(writer, "healing_received", participant.HealingReceived);
            WriteAmounts(writer, "temp_hp_granted", participant.TemporaryHitPointsGranted);
            WriteAmounts(writer, "temp_hp_received", participant.TemporaryHitPointsReceived);
            WriteAmounts(writer, "temp_hp_absorbed", participant.TemporaryHitPointsAbsorbed);
            WriteAmounts(writer, "temp_hp_granted_absorbed", participant.TemporaryHitPointsGrantedAbsorbed);
            WriteAmounts(writer, "fp_spent", participant.FPSpent);
            WriteAmounts(writer, "fp_regained", participant.FPRegained);
            WriteAmounts(writer, "stm_spent", participant.StaminaSpent);
            WriteAmounts(writer, "stm_regained", participant.StaminaRegained);
            WriteAmounts(writer, "hp_spent", participant.HitPointsSpent);
            WriteAmounts(writer, "items_consumed", participant.ItemsConsumed);

            writer.WriteEndObject();
        }

        private static void WriteDamage(Utf8JsonWriter writer, string propertyName, Dictionary<string, CombatRecordDamageTotals> totals)
        {
            writer.WriteStartObject(propertyName);
            writer.WriteNumber("total_effective", totals.Values.Sum(total => total.Effective));
            writer.WriteStartObject("by_source");
            foreach (var (label, total) in totals.OrderByDescending(pair => pair.Value.Effective).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(label);
                writer.WriteNumber("hits", total.Hits);
                writer.WriteNumber("total", total.Total);
                writer.WriteNumber("effective", total.Effective);
                writer.WriteNumber("hp_lost", total.HitPointsLost);
                writer.WriteNumber("temp_hp_absorbed", total.TemporaryHitPointsAbsorbed);
                writer.WriteNumber("overkill", total.Overkill);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteHealing(Utf8JsonWriter writer, string propertyName, Dictionary<string, CombatRecordHealingTotals> totals)
        {
            writer.WriteStartObject(propertyName);
            writer.WriteNumber("total_effective", totals.Values.Sum(total => total.Effective));
            writer.WriteNumber("total_overheal", totals.Values.Sum(total => total.Overheal));
            writer.WriteStartObject("by_source");
            foreach (var (label, total) in totals.OrderByDescending(pair => pair.Value.Effective).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(label);
                writer.WriteNumber("count", total.Count);
                writer.WriteNumber("requested", total.Requested);
                writer.WriteNumber("effective", total.Effective);
                writer.WriteNumber("overheal", total.Overheal);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteAmounts(Utf8JsonWriter writer, string propertyName, Dictionary<string, CombatRecordAmountTotals> totals)
        {
            writer.WriteStartObject(propertyName);
            writer.WriteNumber("total", totals.Values.Sum(total => total.Amount));
            writer.WriteStartObject("by_source");
            foreach (var (label, total) in totals.OrderByDescending(pair => pair.Value.Amount).ThenBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(label);
                writer.WriteNumber("count", total.Count);
                writer.WriteNumber("amount", total.Amount);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteNestedCounts(Utf8JsonWriter writer, string propertyName, Dictionary<string, Dictionary<string, int>> counts)
        {
            writer.WriteStartObject(propertyName);
            foreach (var (ability, byKey) in counts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                writer.WriteStartObject(ability);
                foreach (var (key, count) in byKey.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    writer.WriteNumber(key, count);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }
    }
}
