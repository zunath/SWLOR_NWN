using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class StealthAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            AddStealthRank(builder, FeatType.Stealth1, "Stealth I", 1);
            AddStealthRank(builder, FeatType.Stealth2, "Stealth II", 2);
            AddStealthRank(builder, FeatType.Stealth3, "Stealth III", 3);
            AddStealthRank(builder, FeatType.Stealth4, "Stealth IV", 4);

            return builder.Build();
        }

        private static void AddStealthRank(AbilityBuilder builder, FeatType feat, string name, int level)
        {
            builder
                .Create(feat, PerkType.Stealth)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasRecastDelay(RecastGroup.Stealth, 8f)
                .SkillType(SkillType.Espionage)
                .HasCustomValidation(ValidateStealthUse)
                .HasImpactAction(ToggleStealthMode)
                .PreservesStealthDuringActivation()
                .IsCastedAbility();
        }

        private static string ValidateStealthUse(uint activator, uint target, int level, Location targetLocation)
        {
            // Exiting stealth is always valid. Entry remains out-of-combat only, except for the
            // short explicit window opened by Ghost Protocol.
            if (GetActionMode(activator, ActionMode.Stealth) ||
                !GetIsInCombat(activator) ||
                GetLocalInt(activator, Stealth.CombatEntryWindowVariable) != 0)
            {
                return string.Empty;
            }

            return "You cannot enter stealth while in combat.";
        }

        private static void ToggleStealthMode(uint activator, uint target, int level, Location targetLocation)
        {
            var enteringStealth = !GetActionMode(activator, ActionMode.Stealth);

            Log.Write(LogGroup.Attack,
                $"Player '{GetName(activator)}' ({GetObjectUUID(activator)}) {(enteringStealth ? "entered" : "exited")} Espionage stealth.");

            AssignCommand(activator, () =>
            {
                SetActionMode(activator, ActionMode.Stealth, enteringStealth);
            });
        }
    }
}
