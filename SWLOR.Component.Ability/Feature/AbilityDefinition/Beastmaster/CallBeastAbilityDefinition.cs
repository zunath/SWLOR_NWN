using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class CallBeastAbilityDefinition: IAbilityListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly ICombatPointService _combatPointService;
        private readonly BeastMastery _beastMastery;
        private readonly IEnmityService _enmityService;
        private readonly IPerkService _perkService;

        public CallBeastAbilityDefinition(
            IDatabaseService db, 
            ICombatPointService combatPointService, 
            BeastMastery beastMastery, 
            IEnmityService enmityService, 
            IPerkService perkService)
        {
            _db = db;
            _combatPointService = combatPointService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
            _perkService = perkService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CallBeast(builder);

            return builder.Build();
        }

        private void CallBeast(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CallBeast, PerkType.Tame) // Intentionally tied to Tame
                .Name("Call Beast")
                .Level(1)
                .HasRecastDelay(RecastGroup.CallBeast, 60f * 10f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (GetIsInCombat(activator) || _enmityService.HasEnmity(activator))
                    {
                        return "You are in combat and cannot call your beast.";
                    }

                    var maxBeastLevel = _perkService.GetPerkLevel(activator, PerkType.Tame) * 10;

                    if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                    {
                        return "Only players may use this ability.";
                    }

                    if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, activator)))
                    {
                        return "You already have a companion active.";
                    }

                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);

                    if (string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId))
                    {
                        return "You do not have an active beast.";
                    }

                    var dbBeast = _db.Get<Beast>(dbPlayer.ActiveBeastId);

                    if (dbBeast.IsDead)
                    {
                        return "Your beast is unconscious.";
                    }

                    if (dbBeast.Level > maxBeastLevel)
                    {
                        return $"Your Tame level is too low to call this beast. (Required: {maxBeastLevel/10})";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);
                    
                    _beastMastery.SpawnBeast(activator, dbPlayer.ActiveBeastId, 50);

                    _enmityService.ModifyEnmityOnAll(activator, 230);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
