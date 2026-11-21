using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using static SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetFormat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// The stats table rows for status effects, movement, force evasion, detection, stealth and experience.
    /// </summary>
    public class CharacterSheetUtilityRows
    {
        private ICreatureStatService CreatureStat { get; }
        private IPerkService Perk { get; }
        private IStatService Stat { get; }

        public CharacterSheetUtilityRows(ICreatureStatService creatureStat, IPerkService perk, IStatService stat)
        {
            CreatureStat = creatureStat;
            Perk = perk;
            Stat = stat;
        }

        public void AddRows(ICharacterSheetHost host, Action<string, string, string> addStat)
        {
            addStat("Ranged Evasion", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.RangedEvasionPercentAdjustment)), "Evasion adjustment against ranged attacks.");
            addStat("Slow", GetEffectStateLabel(host, EffectTypeScript.Slow), "Reduces attack speed.");
            addStat("Paralysis", GetEffectStateLabel(host, EffectTypeScript.Paralyze), "Prevents auto attacks and other actions.");
            addStat("Movement Speed", FormatMultiplier(CreatureStat.GetMovementSpeedMultiplier(host.Target)), "Increases or decreases your movement speed.");
            addStat("Force Evasion", FormatPercent(GetForceEvasion(host)), "Percent chance to completely evade a detrimental force ability.");
            addStat("Force Affinity", Perk.GetForceAffinity(host.Target).ToString(), "-10 Dark to +10 Light. Strengthens matching powers; weakens opposing powers.");
            addStat("Detection", Stat.GetDetection(host.Target).ToString(), "PER + WIL plus equipment, perk, and status-effect bonuses; Detect mode adds +5.");
            addStat("Stealth", Stat.GetStealth(host.Target).ToString(), "Twice AGI plus equipment, perk, and status-effect bonuses.");
            addStat("Experience", FormatPercent(Stat.GetStatAdjustment(host.Target, StatType.ExperiencePercentAdjustment)), "Bonus or penalty applied to experience gained from skill use.");
        }

        private static string GetEffectStateLabel(ICharacterSheetHost host, EffectTypeScript effectType)
        {
            for (var effect = GetFirstEffect(host.Target); GetIsEffectValid(effect); effect = GetNextEffect(host.Target))
            {
                if (GetEffectType(effect) == effectType)
                    return "Active";
            }

            return "Inactive";
        }

        private int GetForceEvasion(ICharacterSheetHost host)
        {
            var skillType = Stat.GetStatAdjustment(host.Target, StatType.IncomingAbilityHitChancePercentAdjustmentSkillType);
            if (skillType != (int)SkillType.Force)
                return 0;

            return Math.Max(0, -Stat.GetStatAdjustment(host.Target, StatType.IncomingAbilityHitChancePercentAdjustment));
        }
    }
}
