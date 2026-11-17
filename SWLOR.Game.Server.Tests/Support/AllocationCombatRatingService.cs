using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 5 of the stat split, <c>CombatRatingService</c>, with its calls into unconverted code answered without allocating, as
/// <see cref="AllocationStatService"/> answered them before the ratings moved (P5-4c).
/// </summary>
internal sealed class AllocationCombatRatingService : CombatRatingService
{
    public AllocationCombatRatingService(
        IDBService db,
        ISkillService skill,
        IItemService item,
        ITwinPistolEquipmentService twinPistolEquipment,
        ICombatQueriesService queries,
        ICombatDamageModifiersService damageModifiers,
        IStatService stat)
        : base(db, skill, item, twinPistolEquipment, queries, damageModifiers, stat)
    {
    }

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => 0;

    protected override int GetLowHPAttackAdjustment(uint creature) => 0;

    protected override int GetLowFPAttackAdjustment(uint creature) => 0;

    protected override SkillType GetWeaponSkill(uint creature, uint weapon) => SkillType.Invalid;

    protected override bool IsRangedDamageSkill(SkillType skillType) => false;
}
