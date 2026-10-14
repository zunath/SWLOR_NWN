using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolCrossSkillEngineTests
{
    private static uint _observedTarget = OBJECT_INVALID;
    private static int _damageEvents;

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveActivatedImpacts()
    {
        if (OBJECT_SELF == _observedTarget && GetTotalDamageDealt() > 0) _damageEvents++;
    }

    [EngineTest("Twin Pistol cross-trained attacks capture one impact per weapon and none from shields", Category = "TwinPistolCrossSkill", TimeoutSeconds = 120f)]
    public static async Task CrossTrainedWeaponCounts(EngineTestContext ctx)
    {
        var loadouts = new (string Main, string Off, int Impacts)[]
        {
            ("b_pistol", null, 1), ("b_pistol", "ec_shield", 1),
            ("b_longsword", null, 1), ("b_longsword", "b_longsword", 2),
            ("b_staff", null, 1), ("b_rifle", null, 1),
            ("saber_train_1", null, 1), ("saber_train_1", "saber_train_1", 2),
            ("trn_saberstaff_1", null, 1), ("b_pistol", "tp_comp_proto", 2),
        };
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            foreach (var (mainResref, offResref, impacts) in loadouts)
            {
                var actor = ctx.SpawnCreature("civilian");
                var target = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.EquipItemAsync(actor, mainResref, InventorySlot.RightHand);
                if (offResref != null) await ctx.EquipItemAsync(actor, offResref, InventorySlot.LeftHand);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ctx.SetNPCResources(actor, 100, 100);
                    ctx.SetNPCPerkLevel(actor, PerkType.PairedVolley, 1);
                    CreaturePlugin.AddFeat(actor, FeatType.PairedVolley1);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
                    ctx.MakeHostile(target);
                    _observedTarget = target;
                    _damageEvents = 0;
                    Ability.ClearLastCompletedAbilityImpactSummary(actor);
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.PairedVolley1, GetLocation(target)),
                        $"Paired Volley accepts {mainResref}/{offResref}: {Ability.GetLastActivationDenialReason()}");
                });
                await ctx.WaitUntilAsync(() => _damageEvents >= impacts &&
                    Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                    8f, $"the captured {mainResref}/{offResref} weapon impacts");
                await ctx.DelaySecondsAsync(.2f);
                ctx.AssertEqual(impacts, _damageEvents, $"{mainResref}/{offResref} contributes exactly one impact per equipped attack weapon");
                ctx.AssertEqual(SkillType.TwinPistol, Ability.GetLastCompletedAbilityImpactSummary(actor).SkillType,
                    "Cross-trained equipment does not redirect the purchased ability's skill");
                await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                _observedTarget = OBJECT_INVALID;
                DestroyObject(actor);
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
        }
        finally
        {
            _observedTarget = OBJECT_INVALID;
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
        }
    }

    [EngineTest("Twin Pistol stances retain their global damage cost and earned rhythm across saber rifle and pistol swaps", Category = "TwinPistolCrossSkill", TimeoutSeconds = 90f)]
    public static async Task StancesAndRhythmSurviveWeaponSwaps(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 50;
            record.Skills[SkillType.Lightsaber].Rank = 50;
            record.Skills[SkillType.Rifle].Rank = 50;
            record.Perks[PerkType.AkimboStance] = 1;
            record.Perks[PerkType.CrossfireStance] = 1;
        });
        var main = await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
        var companion = await ctx.EquipItemAsync(actor, "tp_comp_proto", InventorySlot.LeftHand);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        var saber = OBJECT_INVALID;
        var rifle = OBJECT_INVALID;
        var hasteBaseline = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
        var movementBaseline = Stat.GetStatAdjustment(actor, StatType.MovementSpeedPercentAdjustment);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            saber = CreateItemOnObject("saber_train_1", actor);
            rifle = CreateItemOnObject("b_rifle", actor);
            CreaturePlugin.AddFeat(actor, FeatType.AkimboStance);
            CreaturePlugin.AddFeat(actor, FeatType.CrossfireStance);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
            ctx.MakeHostile(target);
            ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.AkimboStance, GetLocation(actor)),
                $"Akimbo activates through the normal stance pipeline: {Ability.GetLastActivationDenialReason()}");
        });
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            await ctx.WaitUntilAsync(() => StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null,
                5f, "Akimbo stance to apply");
            AssignCommand(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => (StatusEffect.GetStatusEffect<AkimboRampStatusEffect>(actor)?.Stacks ?? 0) >= 3,
                24f, "three rhythm stacks earned from actual ordinary hostile hits");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                var ramp = StatusEffect.GetStatusEffect<AkimboRampStatusEffect>(actor);
                var stacks = ramp.Stacks;
                var totalHaste = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
                CreaturePlugin.RunUnequip(actor, companion);
                CreaturePlugin.RunUnequip(actor, main);
                CreaturePlugin.RunEquip(actor, saber, InventorySlot.RightHand);
                ctx.AssertEqual(BaseItem.Lightsaber, GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, actor)), "The replacement is an actual lightsaber");
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null, "A legal weapon swap retains Akimbo");
                ctx.AssertEqual(stacks, StatusEffect.GetStatusEffect<AkimboRampStatusEffect>(actor)?.Stacks ?? 0, "Swapping preserves earned rhythm stacks");
                ctx.AssertEqual(totalHaste, Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent), "Haste remains global with the saber");
                ctx.AssertEqual(85, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "The saber inherits Akimbo's full ordinary damage cost");
                OrdinaryAttackTraits.RecordHit(actor, target, long.MaxValue - 1);
                ctx.AssertEqual(stacks, StatusEffect.GetStatusEffect<AkimboRampStatusEffect>(actor)?.Stacks ?? 0,
                    "A weapon swap does not reset the two-second rhythm grant cooldown");
                CreaturePlugin.RunUnequip(actor, saber);
                CreaturePlugin.RunEquip(actor, rifle, InventorySlot.RightHand);
                ctx.AssertEqual(85, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "The rifle inherits the same ordinary damage cost");
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.CrossfireStance, GetLocation(actor)),
                    $"Crossfire can activate with a rifle: {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => StatusEffect.GetStatusEffect<CrossfireStanceStatusEffect>(actor) != null,
                5f, "Crossfire to replace Akimbo");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) == null &&
                    StatusEffect.GetStatusEffect<AkimboRampStatusEffect>(actor) == null, "Stance exclusivity removes Akimbo and its ramp");
                ctx.AssertEqual(hasteBaseline + 10, Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent), "Crossfire has only its own global Haste");
                ctx.AssertEqual(movementBaseline + 10, Stat.GetStatAdjustment(actor, StatType.MovementSpeedPercentAdjustment), "Crossfire grants global movement speed");
                ctx.AssertEqual(90, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "Crossfire pays its global ordinary damage cost");
                CreaturePlugin.RunUnequip(actor, rifle);
                CreaturePlugin.RunEquip(actor, companion, InventorySlot.LeftHand);
                CreaturePlugin.RunEquip(actor, main, InventorySlot.RightHand);
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The original owned pair can be restored");
                ctx.Assert(StatusEffect.GetStatusEffect<CrossfireStanceStatusEffect>(actor) != null, "Restoring the pair preserves the active cross-trained stance");
                ctx.AssertEqual(90, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "Restoring the pair cannot shed the stance penalty");
                ctx.Assert(!UsePerkFeat.TryUseAbility(actor, actor, FeatType.AkimboStance, GetLocation(actor)), "Equipment changes cannot reset Akimbo's original recast");
                ClearAllActions();
            });
        }
        finally
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            OrdinaryAttackTraits.ClearActor(actor);
            PairedRangedTraits.ClearActor(actor);
        }
    }
}
