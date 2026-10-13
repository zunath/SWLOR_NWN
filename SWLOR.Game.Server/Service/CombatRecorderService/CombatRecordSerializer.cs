using System.Buffers;
using System.Text;
using System.Text.Json;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Writes recorder events as single-line JSON objects. Runs on the recording writer thread,
    /// never on the server's main thread.
    /// </summary>
    public static class CombatRecordSerializer
    {
        public static string ToJsonLine(in CombatRecordEvent recordEvent)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                Write(writer, recordEvent);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        public static void Write(Utf8JsonWriter writer, in CombatRecordEvent e)
        {
            writer.WriteStartObject();
            writer.WriteString("type", CombatRecordNames.GetEventTypeName(e.Type));
            writer.WriteNumber("ts_ms", e.TimestampMs);
            writer.WriteNumber("frame", e.Frame);

            switch (e.Type)
            {
                case CombatRecordEventType.RecordingStarted:
                    writer.WriteString("label", e.Text);
                    writer.WriteString("area_name", e.Text2);
                    writer.WriteString("area_resref", e.SourceName);
                    writer.WriteString("started_utc",
                        DateTimeOffset.FromUnixTimeMilliseconds(e.TimestampMs).UtcDateTime.ToString("O"));
                    break;
                case CombatRecordEventType.RecordingStopped:
                    writer.WriteString("reason", e.Text);
                    writer.WriteNumber("events", e.Value0);
                    break;
                case CombatRecordEventType.Participant:
                    writer.WriteNumber("id", e.ActorId);
                    writer.WriteString("name", e.Text);
                    writer.WriteBoolean("player", e.Flag);
                    if (!string.IsNullOrEmpty(e.Text2))
                        writer.WriteString("resref", e.Text2);
                    break;
                case CombatRecordEventType.AbilityCast:
                case CombatRecordEventType.AbilityQueued:
                    WriteParticipant(writer, "caster", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteLocation(writer, e);
                    WriteAbility(writer, e);
                    if (e.Type == CombatRecordEventType.AbilityCast)
                    {
                        writer.WriteString("activation", CombatRecordNames.GetActivationTypeName(e.Value1));
                        writer.WriteNumber("delay_s", e.Value0 / 1000.0);
                    }
                    break;
                case CombatRecordEventType.AbilityImpact:
                    WriteParticipant(writer, "caster", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteLocation(writer, e);
                    WriteAbility(writer, e);
                    writer.WriteNumber("targets_hit", e.Value0);
                    writer.WriteNumber("critical_hits", e.Value1);
                    writer.WriteNumber("damage_queued", e.Value2);
                    writer.WriteBoolean("recurring", e.Flag);
                    break;
                case CombatRecordEventType.AbilityCost:
                    WriteParticipant(writer, "caster", e.ActorId);
                    WriteAbility(writer, e);
                    writer.WriteNumber("fp", e.Value0);
                    writer.WriteNumber("stm", e.Value1);
                    break;
                case CombatRecordEventType.Attack:
                    WriteParticipant(writer, "attacker", e.ActorId);
                    WriteParticipant(writer, "defender", e.TargetId);
                    writer.WriteString("mode", CombatRecordNames.GetSourceKindName(e.SourceKind));
                    if (e.SourceName != null)
                        writer.WriteString("ability", e.SourceName);
                    writer.WriteString("result", CombatRecordNames.GetAttackResultName((CombatRecordAttackResult)e.Value0));
                    writer.WriteNumber("hit_chance", e.Value1);
                    break;
                case CombatRecordEventType.Damage:
                    WriteParticipant(writer, "source", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteSource(writer, e);
                    WriteDamageBreakdown(writer, e.Damage);
                    writer.WriteNumber("total", e.Value0);
                    writer.WriteNumber("hp_lost", e.GetHitPointsLost());
                    writer.WriteNumber("overkill", e.GetOverkill());
                    writer.WriteNumber("temp_hp_absorbed", e.GetTemporaryHitPointsAbsorbed());
                    writer.WriteNumber("hp_before", e.Value1);
                    writer.WriteNumber("hp_after", e.Value2);
                    writer.WriteNumber("temp_hp_before", e.Value3);
                    writer.WriteNumber("temp_hp_after", e.Value4);
                    writer.WriteNumber("max_hp", e.Value5);
                    writer.WriteBoolean("engine_applied", e.Flag);
                    break;
                case CombatRecordEventType.TemporaryHitPointsAbsorbed:
                    WriteParticipant(writer, "attacker", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    writer.WriteString("effect_key", e.Text);
                    writer.WriteNumber("amount", e.Value0);
                    WriteParticipant(writer, "granted_by", e.Value1);
                    writer.WriteString("granted_kind", CombatRecordNames.GetSourceKindName(e.SourceKind));
                    WriteNullableString(writer, "granted_name", CombatRecordNames.GetSourceName(e.SourceKind, e.SourceName, e.SourceCode));
                    break;
                case CombatRecordEventType.Healing:
                    WriteParticipant(writer, "source", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteSource(writer, e);
                    writer.WriteNumber("requested", e.Value0);
                    writer.WriteNumber("effective", e.Value1);
                    writer.WriteNumber("overheal", Math.Max(0, e.Value0 - e.Value1));
                    writer.WriteNumber("hp_before", e.Value2);
                    writer.WriteNumber("hp_after", e.Value3);
                    writer.WriteNumber("max_hp", e.Value4);
                    break;
                case CombatRecordEventType.TemporaryHitPointsGranted:
                    WriteParticipant(writer, "source", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteSource(writer, e);
                    writer.WriteString("effect_key", e.Text);
                    writer.WriteNumber("amount", e.Value0);
                    writer.WriteNumber("duration_s", e.Value1 / 1000.0);
                    break;
                case CombatRecordEventType.Resource:
                    WriteParticipant(writer, "creature", e.ActorId);
                    writer.WriteString("resource", CombatRecordNames.GetResourceName((CombatRecordResource)e.Value0));
                    writer.WriteNumber("delta", e.Value1);
                    if (e.Value2 >= 0)
                        writer.WriteNumber("after", e.Value2);
                    if (e.Text != null)
                        writer.WriteString("item", e.Text);
                    writer.WriteString("reason", CombatRecordNames.GetSourceKindName(e.SourceKind));
                    WriteNullableString(writer, "reason_name", CombatRecordNames.GetSourceName(e.SourceKind, e.SourceName, e.SourceCode));
                    break;
                case CombatRecordEventType.StatusApplied:
                case CombatRecordEventType.StatusRefreshed:
                    WriteParticipant(writer, "source", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    writer.WriteString("effect", e.Text);
                    WriteNullableString(writer, "effect_type", e.SourceName);
                    WriteNullableString(writer, "ability", e.Text2);
                    if (e.Value0 == CombatRecordEvent.PermanentDuration)
                        writer.WriteNull("duration_s");
                    else
                        writer.WriteNumber("duration_s", e.Value0 / 1000.0);
                    break;
                case CombatRecordEventType.StatusRemoved:
                    WriteParticipant(writer, "source", e.ActorId);
                    WriteParticipant(writer, "target", e.TargetId);
                    writer.WriteString("effect", e.Text);
                    WriteNullableString(writer, "effect_type", e.SourceName);
                    writer.WriteString("reason", CombatRecordNames.GetStatusRemovalName((CombatRecordStatusRemoval)e.Value0));
                    break;
                case CombatRecordEventType.Death:
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteParticipant(writer, "killer", e.ActorId);
                    WriteParticipant(writer, "last_damage_source", e.Value0);
                    break;
                case CombatRecordEventType.Resurrection:
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteParticipant(writer, "caster", e.ActorId);
                    WriteSource(writer, e);
                    break;
                case CombatRecordEventType.Subdual:
                    WriteParticipant(writer, "target", e.TargetId);
                    WriteParticipant(writer, "subdued_by", e.ActorId);
                    break;
                case CombatRecordEventType.Snapshot:
                    WriteParticipant(writer, "creature", e.ActorId);
                    writer.WriteNumber("hp", e.Value0);
                    writer.WriteNumber("max_hp", e.Value1);
                    writer.WriteNumber("temp_hp", e.Value2);
                    writer.WriteNumber("fp", e.Value3);
                    writer.WriteNumber("max_fp", e.Value4);
                    writer.WriteNumber("stm", e.Value5);
                    writer.WriteNumber("max_stm", e.Value6);
                    writer.WriteBoolean("in_combat", e.Flag);
                    writer.WriteBoolean("dead", e.Value7 != 0);
                    break;
            }

            writer.WriteEndObject();
        }

        private static void WriteParticipant(Utf8JsonWriter writer, string propertyName, int participantId)
        {
            if (participantId <= 0)
                writer.WriteNull(propertyName);
            else
                writer.WriteNumber(propertyName, participantId);
        }

        private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string value)
        {
            if (value == null)
                writer.WriteNull(propertyName);
            else
                writer.WriteString(propertyName, value);
        }

        private static void WriteLocation(Utf8JsonWriter writer, in CombatRecordEvent e)
        {
            if (!e.HasLocation)
                return;

            writer.WriteStartObject("location");
            writer.WriteNumber("x", Math.Round(e.X, 2));
            writer.WriteNumber("y", Math.Round(e.Y, 2));
            writer.WriteNumber("z", Math.Round(e.Z, 2));
            writer.WriteEndObject();
        }

        private static void WriteAbility(Utf8JsonWriter writer, in CombatRecordEvent e)
        {
            WriteNullableString(writer, "ability", e.SourceName);
            if (e.SourceCode > 0)
            {
                writer.WriteNumber("feat_id", e.SourceCode);
                writer.WriteString("feat", ((FeatType)e.SourceCode).ToString());
            }
            else
            {
                writer.WriteNull("feat");
            }

            writer.WriteNumber("rank", e.Rank);
        }

        private static void WriteSource(Utf8JsonWriter writer, in CombatRecordEvent e)
        {
            writer.WriteString("source_kind", CombatRecordNames.GetSourceKindName(e.SourceKind));
            WriteNullableString(writer, "source_name", CombatRecordNames.GetSourceName(e.SourceKind, e.SourceName, e.SourceCode));
            if (e.SourceKind == CombatRecordSourceKind.Ability && e.SourceCode > 0)
                writer.WriteString("feat", ((FeatType)e.SourceCode).ToString());
        }

        private static void WriteDamageBreakdown(Utf8JsonWriter writer, in CombatRecordDamageBreakdown damage)
        {
            writer.WriteStartObject("damage");
            WritePositive(writer, "physical", damage.Physical);
            WritePositive(writer, "force", damage.Force);
            WritePositive(writer, "fire", damage.Fire);
            WritePositive(writer, "poison", damage.Poison);
            WritePositive(writer, "electrical", damage.Electrical);
            WritePositive(writer, "ice", damage.Ice);
            WritePositive(writer, "sonic", damage.Sonic);
            WritePositive(writer, "other", damage.Other);
            writer.WriteEndObject();
        }

        private static void WritePositive(Utf8JsonWriter writer, string propertyName, int value)
        {
            if (value > 0)
                writer.WriteNumber(propertyName, value);
        }
    }
}
