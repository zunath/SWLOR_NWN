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
            Stealth(builder, FeatType.Stealth1, "Stealth I", 1);
            Stealth(builder, FeatType.Stealth2, "Stealth II", 2);
            Stealth(builder, FeatType.Stealth3, "Stealth III", 3);
            Stealth(builder, FeatType.Stealth4, "Stealth IV", 4);

            return builder.Build();
        }

        private static void Stealth(AbilityBuilder builder, FeatType feat, string name, int level)
        {
            builder
                .Create(feat, PerkType.Stealth)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasRecastDelay(RecastGroup.Stealth, 8f)
                .SkillType(SkillType.Espionage)
                .HasImpactAction(ToggleStealthMode)
                .IsCastedAbility();
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
