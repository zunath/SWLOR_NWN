using System.Collections.Generic;

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.Shared.Core.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class IncendiaryBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;

        public IncendiaryBombAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService) 
            : base(random, itemService, perkService, statService, combatService, combatPoint, enmityService)
        {
            _combatService = combatService;
            _statService = statService;
        }

        private void ApplyEffect(uint creature, int dmg)
        {
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = _statService.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), creature);
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary1Enable)]
        public void IncendiaryBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 4);
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary1Heartbeat)]
        public void IncendiaryBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary2Enable)]
        public static void IncendiaryBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 10);
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary2Heartbeat)]
        public void IncendiaryBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 10);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary3Enable)]
        public void IncendiaryBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        [ScriptHandler(ScriptName.OnGrenadeIncendiary3Heartbeat)]
        public void IncendiaryBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 16);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            IncendiaryBomb1();
            IncendiaryBomb2();
            IncendiaryBomb3();

            return _builder.Build();
        }
        
        private void IncendiaryBomb1()
        {
            _builder.Create(FeatType.IncendiaryBomb1, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc1_en",
                        "grenade_inc1_hb",
                        20f);

                    _enmityService.ModifyEnmityOnAll(activator, 250);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb2()
        {
            _builder.Create(FeatType.IncendiaryBomb2, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc2_en",
                        "grenade_inc2_hb",
                        40f);

                    _enmityService.ModifyEnmityOnAll(activator, 350);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb3()
        {
            _builder.Create(FeatType.IncendiaryBomb3, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc3_en",
                        "grenade_inc3_hb",
                        60f);

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
