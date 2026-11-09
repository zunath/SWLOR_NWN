using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The instance with its calls into unconverted code answered without allocating.</summary>
internal sealed class AllocationStatService : StatService
{
    public AllocationStatService(
        IDBService db,
        ICombatRecorderService combatRecorder,
        IPlayerFeedbackService playerFeedback,
        IStatusEffectService statusEffect,
        IMimicryService mimicry,
        ITwinPistolEquipmentService twinPistolEquipment,
        IPowerGloveEquipmentService powerGloveEquipment,
        IItemService item,
        IRecastService recast,
        IBeastMasteryService beastMastery,
        IEnmityService enmity,
        IResistanceService resistance,
        IPvpCombatService pvpCombat,
        IPerkService perk,
        ISkillService skill,
        ITemporaryStatModifierService temporaryStatModifier)
        : base(db, combatRecorder, playerFeedback, statusEffect, mimicry, twinPistolEquipment, powerGloveEquipment, item, recast, beastMastery, enmity, resistance, pvpCombat,
            perk, skill, temporaryStatModifier, FacadeForwarder.LazyFor<ICombatAttackTimingService>(), FacadeForwarder.For<ICombatDamageCatalogService>(), FacadeForwarder.LazyFor<ICombatDamageModifiersService>(), FacadeForwarder.LazyFor<ICombatDamageTakenService>(), FacadeForwarder.LazyFor<ICombatQueriesService>(), FacadeForwarder.LazyFor<ICombatResourcesService>(), FacadeForwarder.LazyFor<IDroidService>())
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
