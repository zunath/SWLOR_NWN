using System.Text;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The settings of every ability the weapon ability definitions build, as text: per feat, every <c>AbilityDetail</c> property that differs from a new detail and every
/// <c>WeaponAbilityProfile</c> property that differs from the default, one "Feat Section.Property value" line each, read by reflection.
/// </summary>
public static class WeaponAbilitySettingsWriter
{
    public static string Write(IReadOnlyList<WeaponAbilityEntry> entries)
    {
        var text = new StringBuilder();
        var defaults = new AbilityDetail();
        foreach (var entry in entries)
        {
            text.Append("== ").Append(entry.Feat).Append(' ').Append(entry.Definition).Append('\n');
            foreach (var property in WeaponAbilityValueWriter.Properties(typeof(AbilityDetail)))
            {
                var value = WeaponAbilityValueWriter.Write(property.GetValue(entry.Detail));
                if (value != WeaponAbilityValueWriter.Write(property.GetValue(defaults)))
                    text.Append(entry.Feat).Append(" Detail.").Append(property.Name).Append(' ').Append(value).Append('\n');
            }

            var profile = WeaponAbilityProfileReader.Find(entry.Detail);
            if (profile == null)
            {
                text.Append(entry.Feat).Append(" Profile none\n");
                continue;
            }

            var changed = WeaponAbilityProfileReader.NonDefault(profile);
            text.Append(entry.Feat).Append(" Profile configured ").Append(changed.Count).Append('\n');
            foreach (var line in changed)
                text.Append(entry.Feat).Append(" Profile.").Append(line.Replace("=", " ", StringComparison.Ordinal)).Append('\n');
        }

        return text.ToString();
    }
}
