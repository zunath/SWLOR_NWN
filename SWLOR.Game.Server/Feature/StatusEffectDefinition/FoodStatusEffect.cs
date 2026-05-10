using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FoodStatusEffect : StatusEffectBase
    {
        public FoodEffectData Food { get; }
        public override string Name => "Food";
        public override EffectIconType Icon => EffectIconType.Food;

        public FoodStatusEffect()
            : this(new FoodEffectData())
        {
        }

        public FoodStatusEffect(FoodEffectData food)
        {
            Food = food;

            StatGroup.Stats[StatType.MaxFP] = food.FP;
            StatGroup.Stats[StatType.MaxStamina] = food.STM;
            StatGroup.Stats[StatType.Attack] = food.Attack;
            StatGroup.Stats[StatType.Accuracy] = food.Accuracy;
            StatGroup.Stats[StatType.Evasion] = food.Evasion;
            StatGroup.Stats[StatType.PhysicalDefense] = food.DefensePhysical;
            StatGroup.Stats[StatType.ForceDefense] = food.DefenseForce;
            StatGroup.Resists[ResistanceType.Fire] = food.ResistanceFire;
            StatGroup.Resists[ResistanceType.Poison] = food.ResistancePoison;
            StatGroup.Resists[ResistanceType.Electrical] = food.ResistanceElectrical;
            StatGroup.Resists[ResistanceType.Ice] = food.ResistanceIce;
            StatGroup.Resists[ResistanceType.Mind] = food.ResistanceMind;
            StatGroup.Resists[ResistanceType.Mobility] = food.ResistanceMobility;
            StatGroup.Resists[ResistanceType.Trauma] = food.ResistanceTrauma;
            StatGroup.Resists[ResistanceType.Disruption] = food.ResistanceDisruption;
            StatGroup.Stats[StatType.ExperiencePercentAdjustment] = food.XPBonusPercent;
            StatGroup.Stats[StatType.HPRegen] = food.HPRegen;
            StatGroup.Stats[StatType.FPRegen] = food.FPRegen;
            StatGroup.Stats[StatType.StaminaRegen] = food.STMRegen;
            StatGroup.Stats[StatType.RestRegen] = food.RestRegen;
            StatGroup.Stats[StatType.AbilityRecastReductionPercent] = food.RecastReductionPercent;
            StatGroup.Abilities[AbilityType.Might] = food.Might;
            StatGroup.Abilities[AbilityType.Vitality] = food.Vitality;
            StatGroup.Abilities[AbilityType.Perception] = food.Perception;
            StatGroup.Abilities[AbilityType.Willpower] = food.Willpower;
            StatGroup.Abilities[AbilityType.Agility] = food.Agility;
            StatGroup.Abilities[AbilityType.Social] = food.Social;

            AddCraftBonus(CraftSkillBonusType.Control, food.Control);
            AddCraftBonus(CraftSkillBonusType.Craftsmanship, food.Craftsmanship);
        }

        public override IStatusEffect Clone()
        {
            return new FoodStatusEffect(Food);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (Food.HP <= 0)
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.TemporaryFoodHP = Food.HP;
            Stat.AdjustPlayerMaxHP(dbPlayer, creature, Food.HP);
            DB.Set(dbPlayer);
        }

        protected override void Reapply(uint creature)
        {
            if (Food.HP <= 0)
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            Stat.AdjustPlayerMaxHP(dbPlayer, creature, 0);
        }

        protected override void Remove(uint creature)
        {
            if (Food.HP <= 0)
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            dbPlayer.TemporaryFoodHP = 0;
            Stat.AdjustPlayerMaxHP(dbPlayer, creature, -Food.HP);
            DB.Set(dbPlayer);
        }

        private void AddCraftBonus(CraftSkillBonusType bonusType, System.Collections.Generic.IReadOnlyDictionary<SkillType, int> bonuses)
        {
            foreach (var (skill, value) in bonuses)
            {
                StatGroup.CraftSkillBonuses[bonusType][skill] = value;
            }
        }
    }
}
