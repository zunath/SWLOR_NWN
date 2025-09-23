using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class ReviveBeastAbilityDefinition : IAbilityListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly ICombatPointService _combatPointService;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IEnmityService _enmityService;

        public ReviveBeastAbilityDefinition(
            IDatabaseService db, 
            ICombatPointService combatPointService, 
            IBeastMasteryService beastMastery, 
            IEnmityService enmityService)
        {
            _db = db;
            _combatPointService = combatPointService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ReviveBeast1(builder);
            ReviveBeast2(builder);
            ReviveBeast3(builder);

            return builder.Build();
        }

        private string Validation(uint activator)
        {
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

            if (!dbBeast.IsDead)
            {
                return "Your beast is not unconscious.";
            }

            return string.Empty;
        }

        private void ReviveBeast1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast1, PerkType.ReviveBeast)
                .Name("Revive Beast I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(4f)
                .RequirementStamina(15)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var dbBeast = _db.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    _db.Set(dbBeast);

                    _beastMastery.SpawnBeast(activator, dbBeast.Id, 0);
                    _enmityService.ModifyEnmityOnAll(activator, 500);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void ReviveBeast2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast2, PerkType.ReviveBeast)
                .Name("Revive Beast II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(4f)
                .RequirementStamina(17)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var dbBeast = _db.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    _db.Set(dbBeast);

                    var hpPercentage = 10 + GetAbilityScore(activator, AbilityType.Social);
                    _beastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                    _enmityService.ModifyEnmityOnAll(activator, 500);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void ReviveBeast3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ReviveBeast3, PerkType.ReviveBeast)
                .Name("Revive Beast III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(4f)
                .RequirementStamina(18)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = _db.Get<Player>(playerId);
                    var dbBeast = _db.Get<Beast>(dbPlayer.ActiveBeastId);

                    dbBeast.IsDead = false;

                    _db.Set(dbBeast);

                    var hpPercentage = 30 + GetAbilityScore(activator, AbilityType.Social) * 2;
                    _beastMastery.SpawnBeast(activator, dbBeast.Id, hpPercentage);
                    _enmityService.ModifyEnmityOnAll(activator, 500);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
