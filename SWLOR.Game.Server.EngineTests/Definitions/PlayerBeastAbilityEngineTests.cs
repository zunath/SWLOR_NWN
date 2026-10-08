using System.Threading.Tasks;
using System.Collections.Generic;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerBeastAbilityEngineTests
    {
        public static bool Supports(FeatType feat) => feat is FeatType.CallBeast or
            FeatType.GuardingBondStance or FeatType.PredatoryBondStance or FeatType.Reward1 or
            FeatType.Reward2 or FeatType.Reward3 or FeatType.ReviveBeast1 or FeatType.ReviveBeast2 or
            FeatType.ReviveBeast3 or FeatType.SoothePet or FeatType.Tame;

        public static async Task RunCaseAsync(EngineTestContext ctx, FeatType feat)
        {
            ctx.Assert(Supports(feat), "player beast fixture must have a specific observable outcome for this feat");
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var ability = Ability.GetAbilityDetail(feat);
            fixture.Update(record => record.Perks[ability.EffectiveLevelPerkType] =
                Math.Max(record.Perks.GetValueOrDefault(ability.EffectiveLevelPerkType), ability.AbilityLevel));
            CreaturePlugin.AddFeat(player, feat);
            var isTame = feat == FeatType.Tame;
            var isCall = feat == FeatType.CallBeast;
            var isRevive = feat is FeatType.ReviveBeast1 or FeatType.ReviveBeast2 or FeatType.ReviveBeast3;
            var isReward = feat is FeatType.Reward1 or FeatType.Reward2 or FeatType.Reward3;
            var target = player;
            var pet = OBJECT_INVALID;
            var treat = OBJECT_INVALID;
            string beastId = null;
            try
            {
                if (isTame)
                {
                    target = ctx.SpawnCreature("nw_rat001", 1f);
                    await ctx.DelaySecondsAsync(1f);
                    ctx.Assert(!string.IsNullOrWhiteSpace(ability.CustomValidation(player, target, 1, GetLocation(target))),
                        "a non-tamable NPC is rejected");
                    BeastMastery.SetBeastType(target, BeastType.Womprat);
                    ctx.SeedRandom(1);
                }
                else
                {
                    ctx.Assert(!string.IsNullOrWhiteSpace(ability.CustomValidation(player, player, ability.AbilityLevel, GetLocation(player))),
                        "missing active beast is rejected");
                    var record = new Beast
                    {
                        OwnerPlayerId = fixture.Id, Name = "Engine test beast", Type = BeastType.Womprat,
                        Level = 20, IsDead = isRevive,
                    };
                    DB.Set(record);
                    beastId = record.Id;
                    fixture.Update(db => db.ActiveBeastId = beastId);
                    if (!isCall && !isRevive)
                    {
                        await ctx.ExecuteInCreatureContextAsync(player, () => BeastMastery.SpawnBeast(player, beastId, 100));
                        await ctx.WaitUntilAsync(() => BeastMastery.IsPlayerBeast(GetAssociate(AssociateType.Henchman, player)), 5f, "owned companion to spawn");
                        pet = GetAssociate(AssociateType.Henchman, player);
                        ctx.Track(pet);
                        await ctx.DelaySecondsAsync(4.5f);
                        ctx.SuppressNPCNaturalRegen(pet);
                        if (isReward)
                        {
                            ctx.Assert(!string.IsNullOrWhiteSpace(ability.CustomValidation(player, player, ability.AbilityLevel, GetLocation(player))),
                                "Reward without treats is rejected even with an owned companion");
                            await ctx.ExecuteInCreatureContextAsync(player, () =>
                            {
                                treat = CreateItemOnObject("pet_treat", player, 2);
                                SetItemStackSize(treat, 2);
                                ObjectPlugin.SetCurrentHitPoints(pet, 1);
                            });
                            ctx.Assert(GetIsObjectValid(treat), "pet treat fixture exists");
                            await ctx.WaitUntilAsync(() => GetItemPossessor(treat) == player, 5f,
                                "pet treats to enter the player's inventory");
                            await ctx.ExecuteInCreatureContextAsync(player, () =>
                                ctx.AssertEqual(treat, GetItemPossessedBy(player, "pet_treat"), "native pet-treat tag lookup"));
                            ctx.AssertEqual(2, GetItemStackSize(treat), "two pet treats before Reward");
                        }
                        if (feat == FeatType.SoothePet)
                            ctx.Assert(StatusEffect.ApplyStatusEffect(player, pet, new PoisonStatusEffect(), 30f), "cleanseable pet poison applies");
                    }
                }

                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    ctx.AssertEqual(string.Empty, ability.CustomValidation(player, target, ability.AbilityLevel, GetLocation(target)),
                        "player ownership and supplies satisfy the real validation"));
                var staminaBefore = Stat.GetCurrentStamina(player);
                var petHPBefore = GetIsObjectValid(pet) ? GetCurrentHitPoints(pet) : 0;
                var activated = false;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    activated = UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target)));
                ctx.Assert(activated, $"{feat} activates through the player feat pipeline: {Ability.GetLastActivationDenialReason()}");
                var wait = (ability.ActivationDelay?.Invoke(player, target, ability.AbilityLevel) ?? 0f) + 8f;
                if (isTame)
                {
                    await ctx.WaitUntilAsync(() => !string.IsNullOrEmpty(DB.Get<Player>(fixture.Id).ActiveBeastId), wait, "taming to persist an owned beast");
                    beastId = DB.Get<Player>(fixture.Id).ActiveBeastId;
                    var tamed = DB.Get<Beast>(beastId);
                    ctx.AssertEqual(fixture.Id, tamed.OwnerPlayerId, "tamed beast owner");
                    ctx.AssertEqual(BeastType.Womprat, tamed.Type, "tamed beast type");
                    await ctx.WaitUntilAsync(() => !GetIsObjectValid(target), 3f, "wild target to be removed after taming");
                }
                else if (isCall || isRevive)
                {
                    await ctx.WaitUntilAsync(() => BeastMastery.IsPlayerBeast(GetAssociate(AssociateType.Henchman, player)), wait, "Call/Revive to create the owned associate");
                    pet = GetAssociate(AssociateType.Henchman, player);
                    ctx.Track(pet);
                    ctx.SuppressNPCNaturalRegen(pet);
                    await ctx.DelaySecondsAsync(4.5f);
                    ctx.AssertEqual(beastId, BeastMastery.GetBeastId(pet), "summoned record identity");
                    ctx.Assert(!DB.Get<Beast>(beastId).IsDead, "summoned beast is alive in persisted state");
                    var percent = isCall ? 100 : feat == FeatType.ReviveBeast1 ? 0 :
                        Math.Min(100, (feat == FeatType.ReviveBeast2 ? 10 : 45) + GetAbilityScore(player, AbilityType.Social));
                    var expectedHP = isCall ? GetMaxHitPoints(pet) : Math.Min(GetMaxHitPoints(pet), 1 + (int)(GetMaxHitPoints(pet) * (percent * 0.01f)));
                    ctx.AssertEqual(expectedHP, GetCurrentHitPoints(pet), "summoned health after the delayed native correction");
                }
                else if (isReward)
                {
                    await ctx.WaitUntilAsync(() => GetCurrentHitPoints(pet) > petHPBefore, wait, "Reward to heal the owned beast");
                    ctx.AssertEqual(1, GetItemStackSize(treat), "Reward consumes exactly one treat");
                }
                else if (feat == FeatType.SoothePet)
                    await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<PoisonStatusEffect>(pet), wait, "Soothe Pet to cleanse poison");
                else
                {
                    var status = feat == FeatType.GuardingBondStance ? typeof(GuardingBondStanceStatusEffect) : typeof(PredatoryBondStanceStatusEffect);
                    await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect(player, status), wait, "the bond stance to apply");
                }
                ctx.Assert(Recast.IsOnRecastDelay(player, ability.RecastGroup).Item1, "player recast is persisted");
                var expectedCost = 0;
                foreach (var requirement in ability.Requirements)
                    if (requirement is AbilityRequirementStamina stamina)
                        expectedCost += stamina.RequiredSTM;
                ctx.AssertEqual(staminaBefore - expectedCost, Stat.GetCurrentStamina(player), "player stamina cost");
            }
            finally
            {
                if (GetIsObjectValid(pet))
                {
                    await ctx.ExecuteInCreatureContextAsync(player, () => RemoveHenchman(player, pet));
                    DestroyObject(pet);
                }
                // A failed assertion may occur immediately after Tame persists a record.
                beastId ??= DB.Get<Player>(fixture.Id).ActiveBeastId;
                if (!string.IsNullOrEmpty(beastId)) DB.Delete<Beast>(beastId);
                if (target != player && GetIsObjectValid(target)) DestroyObject(target);
                await ctx.WaitFrameAsync();
                // Queued last so it runs after Dispose clears the player flag; otherwise the
                // fixture stays at the spawn point through the rest of the sweep.
                AssignCommand(player, () => DestroyObject(player));
            }
        }
    }
}
