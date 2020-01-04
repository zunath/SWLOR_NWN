using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class SithAlchemy : IPerk
    {
        public PerkType PerkType => PerkType.SithAlchemy;
        public string Name => "Sith Alchemy";
        public bool IsActive => false;
        public string Description => "The power to transform living (or recently-living) flesh.";
        public PerkCategoryType Category => PerkCategoryType.ForceAlter;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.SithAlchemy;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Dark;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
        {
            {
                1, new PerkLevel(0, "Unlocks Sith Alchemy.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceAlter, 0},
                },
                new List<int>
                {
                    99
                })
            },
            {
                2, new PerkLevel(7, "When used on a corpse, raises the creature as a henchman while the caster concentrates.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceAlter, 80},
                },
                new List<int>
                {
                    99
                })
            },
            {
                3, new PerkLevel(7, "Alchemist can create monsters.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceAlter, 90},
                },
                new List<int>
                {
                    99
                })
            },
            {
                4, new PerkLevel(0, "Alchemist can employ monsters as henchmen while they concentrate.", SpecializationType.Consular,
                new Dictionary<SkillType, int>
                {
                    { SkillType.ForceAlter, 90},
                },
                new List<int>
                {
                    99
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.SithAlchemy1, BaseFPCost = 25, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.SithAlchemy2, BaseFPCost = 300, ConcentrationFPCost = 0, ConcentrationTickInterval = 1}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.SithAlchemy3, BaseFPCost = 0, ConcentrationFPCost = 5, ConcentrationTickInterval = 1}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
