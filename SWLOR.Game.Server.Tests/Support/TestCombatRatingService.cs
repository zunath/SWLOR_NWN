using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part 5 of the stat split, <c>CombatRatingService</c> (Combat, P5-4c): Attack, Accuracy, Defense, Evasion and the deflection and
/// Guard chances, with its calls into unconverted code answered by the bench (<c>StatBench.Seams.cs</c>). It reads the stat
/// adjustment, the NPC stats and the pools through the bench's stat double, which it read on its own instance before the move,
/// so a test sees the same answers and the same log.
/// </summary>
public sealed class TestCombatRatingService : CombatRatingService
{
    private readonly StatBench _bench;

    public TestCombatRatingService(
        StatBench bench,
        IDBService db,
        IItemService item,
        ITwinPistolEquipmentService twinPistolEquipment,
        IStatService stat)
        : base(db, FacadeForwarder.For<ISkillService>(), item, twinPistolEquipment, FacadeForwarder.For<ICombatQueriesService>(),
            FacadeForwarder.For<ICombatDamageModifiersService>(), stat)
    {
        _bench = bench;
    }

    protected override SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => _bench.BaseItemSkill;

    protected override SkillType GetWeaponSkill(uint creature, uint weapon) => _bench.WeaponSkills.GetValueOrDefault(weapon, SkillType.Invalid);

    protected override AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType) => _bench.WeaponDamageAbility;

    protected override AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType) => _bench.WeaponAccuracyAbility;

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => _bench.NearbyStatusTargetAdjustment;

    protected override int GetLowHPAttackAdjustment(uint creature) => _bench.LowHPAdjustment;

    protected override int GetLowFPAttackAdjustment(uint creature) => _bench.LowFPAdjustment;

    protected override bool IsRangedDamageSkill(SkillType skillType) => _bench.RangedSkills.Contains(skillType);
}
