using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The instance with its calls into unconverted code answered without allocating.</summary>
internal sealed class AllocationStatService : StatService
{
    public AllocationStatService(
        IDBService db,
        ICombatRecorderService combatRecorder,
        IPlayerFeedbackService playerFeedback,
        IStatusEffectService statusEffect,
        IMimicryTraitStatsService mimicryTraitStats,
        ITwinPistolEquipmentService twinPistolEquipment,
        IItemService item,
        IRecastService recast,
        ICompanionLookup beastMastery,
        IPvpCombatService pvpCombat,
        IPerkService perk,
        ISkillService skill,
        ITemporaryStatModifierService temporaryStatModifier)
        : base(db, combatRecorder, playerFeedback, statusEffect, mimicryTraitStats, twinPistolEquipment, item, recast, beastMastery, pvpCombat,
            perk, skill, temporaryStatModifier, FacadeForwarder.LazyFor<ICombatAttackTimingService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.LazyFor<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>())
    {
    }

    protected override int GetPerkStatBonus(uint creature, StatType stat) => 0;

    protected override int GetTemporaryStatAdjustment(uint creature, StatType stat) => 0;

    protected override bool IsDroid(uint creature) => false;

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => 0;

    protected override int GetLowHPAttackAdjustment(uint creature) => 0;

    protected override int GetLowFPAttackAdjustment(uint creature) => 0;

    protected override SkillType GetWeaponSkill(uint creature, uint weapon) => SkillType.Invalid;

    protected override bool IsRangedDamageSkill(SkillType skillType) => false;
}
