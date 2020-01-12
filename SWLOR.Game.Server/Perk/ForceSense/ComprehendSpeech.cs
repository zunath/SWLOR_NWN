using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class ComprehendSpeech : IPerk
    {
        public PerkType PerkType => PerkType.ComprehendSpeech;
        public string Name => "Comprehend Speech";
        public bool IsActive => true;
        public string Description => "The caster improves their ability to understand other languages.";
        public PerkCategoryType Category => PerkCategoryType.ForceSense;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ComprehendSpeech;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
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
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Will_Saving_Throw_Use), target);
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
                1, new PerkLevel(3, "The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 0},
                })
            },
            {
                2, new PerkLevel(4, "The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.",
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 15},
                })
            },
            {
                3, new PerkLevel(5, "The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.", SpecializationType.Sentinel,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 30},
                })
            },
            {
                4, new PerkLevel(6, "The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.", SpecializationType.Sentinel,
                new Dictionary<Skill, int>
                {
                    { Skill.ForceSense, 45},
                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ComprehendSpeech1, BaseFPCost = 0, ConcentrationFPCost = 1, ConcentrationTickInterval = 6}
                }
            },
            {
                2, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ComprehendSpeech2, BaseFPCost = 0, ConcentrationFPCost = 2, ConcentrationTickInterval = 6}
                }
            },
            {
                3, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ComprehendSpeech3, BaseFPCost = 0, ConcentrationFPCost = 3, ConcentrationTickInterval = 6}
                }
            },
            {
                4, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.ComprehendSpeech4, BaseFPCost = 0, ConcentrationFPCost = 4, ConcentrationTickInterval = 6}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
