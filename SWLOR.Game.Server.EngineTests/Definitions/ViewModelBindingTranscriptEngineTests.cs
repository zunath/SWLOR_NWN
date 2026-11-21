using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The binding transcripts of the six windows the view model split touches: the player guide, perks, character sheet,
    /// incubator, disguises and droid assembly. Each script opens the window with the recorder attached before it is bound, drives the
    /// window's main interactions on a real saved player record with the clock fixed, and compares the transcript
    /// (<see cref="ViewModelTranscript"/>) with the one captured on the code before any view model code moved. A run without an embedded
    /// fixture writes the transcript to /nwn/home and fails, which is how a fixture is captured. The fixtures hold hak data that only the
    /// full engine hak set carries, so these tests are opt-in like the combat load tests: they run only when SWLOR_ENGINE_TEST_FILTER
    /// names the ViewModelTranscript category, and a default sweep skips them.
    /// </summary>
    public static class ViewModelBindingTranscriptEngineTests
    {
        private const string Category = "ViewModelTranscript";

        [EngineTest("Player guide window publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 120f)]
        public static Task PlayerGuide(EngineTestContext ctx) => RunAsync(ctx, "PlayerGuide", null, GuideScriptAsync);

        [EngineTest("Perks window publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 240f)]
        public static Task Perks(EngineTestContext ctx) => RunAsync(ctx, "Perks", SeedPerks, PerksScriptAsync);

        [EngineTest("Character sheet publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 240f)]
        public static Task CharacterSheet(EngineTestContext ctx) => RunAsync(ctx, "CharacterSheet", SeedSheet, SheetScriptAsync);

        [EngineTest("Incubator window publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 240f)]
        public static Task Incubator(EngineTestContext ctx) => RunAsync(ctx, "Incubator", SeedIncubator, IncubatorScriptAsync);

        [EngineTest("Disguise window publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 240f)]
        public static Task Disguise(EngineTestContext ctx) => RunAsync(ctx, "Disguise", SeedDisguise, DisguiseScriptAsync);

        [EngineTest("Droid assembly window publishes the same bindings and values as before the split", Category = Category, TimeoutSeconds = 240f)]
        public static Task DroidAssembly(EngineTestContext ctx) => RunAsync(ctx, "DroidAssembly", SeedDroid, DroidScriptAsync);

        private sealed class Run
        {
            public EngineTestContext Context;
            public PlayerAbilityFixture Fixture;
            public ViewModelTranscript Transcript;
            public TranscriptClock Clock;

            public uint Player => Fixture.Creature;

            /// <summary>The scripts' own checks that a flow did what the transcript is meant to cover; reported after the transcript is compared.</summary>
            public List<string> Problems { get; } = new();

            public void Check(bool condition, string expectation)
            {
                if (!condition)
                    Problems.Add(expectation);
            }

            /// <summary>Runs an action inside the player's creature context with the recording decorators in front of the engine.</summary>
            public async Task PhaseAsync(Action action)
            {
                await Context.ExecuteInCreatureContextAsync(Player, () =>
                {
                    using var frozen = Clock.Freeze();
                    try
                    {
                        // Inside the try: an Install that fails halfway still restores the decorators it had put in.
                        Transcript.Install();
                        action();
                    }
                    finally
                    {
                        Transcript.Uninstall();
                    }
                });
                await Context.WaitFrameAsync();
            }
        }

        private static async Task RunAsync(EngineTestContext ctx, string name, Action<PlayerAbilityFixture> seed,
            Func<Run, Task> script)
        {
            if (ApplicationSettings.Get().EngineTestFilter?.Contains(Category, StringComparison.OrdinalIgnoreCase) != true)
                ctx.Skip($"The binding transcripts compare against the full engine hak set and run only when SWLOR_ENGINE_TEST_FILTER names {Category}.");

            using var clock = new TranscriptClock();
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            fixture.Update(record =>
            {
                record.Name = "Transcript Player";
                foreach (var skill in Enum.GetValues<SkillType>())
                {
                    if (!record.Skills.ContainsKey(skill))
                        record.Skills[skill] = new PlayerSkill();
                    record.Skills[skill].Rank = 50;
                }

                record.UnallocatedSP = 120;
                record.DatePerkRefundAvailable = null;
            });
            seed?.Invoke(fixture);
            var transcript = new ViewModelTranscript(fixture);
            var run = new Run { Context = ctx, Fixture = fixture, Transcript = transcript, Clock = clock };
            try
            {
                await script(run);
            }
            finally
            {
                transcript.Uninstall();
                CloseWindows(fixture);
            }

            var problems = run.Problems.Count == 0 ? string.Empty : " The script did not cover its flows: " + string.Join(" ", run.Problems);
            Compare(ctx, name, CaptureHeader + transcript.Text, problems);
        }

        /// <summary>
        /// The first line of every fixture. The windows read hak data (robe and forearm models, 2DA rows), so the fixtures are captured, and
        /// later compared, on the full engine hak set the recipe builds; the debugserver hak set lacks models and differs.
        /// </summary>
        private const string CaptureHeader =
            "# Captured on the full engine hak set (scripts/ci/fetch-engine-test-haks.sh checkout, prepare-engine-test-home.sh staging, SWLOR.CLI --hak), host entry; compare only on that set, not on the debugserver hak set.\n";

        // ------------------------------------------------------------------ player guide

        private static async Task GuideScriptAsync(Run run)
        {
            var t = run.Transcript;
            PlayerGuideViewModel guide = null;
            await run.PhaseAsync(() =>
            {
                t.Step("open", () => guide = t.Open<PlayerGuideViewModel>(GuiWindowType.PlayerGuide, null));
                var count = ((IList)Get(guide, "_filteredTopicIndexes")).Count;
                foreach (var index in new[] { 0, 1, count / 2, count - 1 })
                    t.Step("select-topic-" + index, () => Call(guide, "SelectTopicByTopicIndex", ((IList<int>)Get(guide, "_filteredTopicIndexes"))[index]));
                t.Step("search-attack", () =>
                {
                    guide.SearchText = "attack";
                    guide.OnClickSearch()();
                });
                t.Step("select-first-result", () => Call(guide, "SelectTopicByTopicIndex", ((IList<int>)Get(guide, "_filteredTopicIndexes"))[0]));
                t.Step("search-nothing", () =>
                {
                    guide.SearchText = "zzzz no such guide text";
                    guide.OnClickSearch()();
                });
                t.Step("clear-search", () => guide.OnClickClearSearch()());
                t.Step("select-topic-with-related", () =>
                {
                    var topics = (IList)GetStatic(typeof(PlayerGuideViewModel).Assembly.GetType("SWLOR.Game.Server.Feature.GuiDefinition.ViewModel.PlayerGuideTopicCatalog")!, "Topics");
                    for (var index = 0; index < topics.Count; index++)
                    {
                        var related = (IList)topics[index]!.GetType().GetProperty("RelatedTopics")!.GetValue(topics[index])!;
                        if (related.Count == 0)
                            continue;
                        Call(guide, "SelectTopicByTopicIndex", index);
                        break;
                    }
                });
                t.Step("select-related", () => Call(guide, "SelectTopicByTopicIndex", ((IList<int>)Get(guide, "_relatedTopicIndexes")).FirstOrDefault()));
                t.Step("close-and-reopen", () => guide = t.Reopen<PlayerGuideViewModel>(GuiWindowType.PlayerGuide, null));
            });
        }

        // ------------------------------------------------------------------ perks

        // A Force-role beast (Warding Howl, the first beast perk, needs level 12 and the Force role) so the beast purchase has a perk to buy.
        private static void SeedPerks(PlayerAbilityFixture fixture) => SeedBeast(fixture, BeastType.DawnfangHound);

        private static void SeedBeast(PlayerAbilityFixture fixture, BeastType type)
        {
            var beast = new Beast
            {
                OwnerPlayerId = fixture.Id,
                Name = "Transcript Beast",
                Level = 20,
                UnallocatedSP = 8,
                Type = type
            };
            DB.Set(beast);
            fixture.Update(record => record.ActiveBeastId = beast.Id);
        }

        private static async Task PerksScriptAsync(Run run)
        {
            var t = run.Transcript;
            PerksViewModel perks = null;
            int Count() => ((IList)Get(perks, "_filteredPerks")).Count;
            int IndexWhere(Func<PerkType, bool> predicate)
            {
                var list = ((IList)Get(perks, "_filteredPerks")).Cast<PerkType>().ToList();
                return list.FindIndex(perk => predicate(perk));
            }

            void Select(int index) => Call(perks, "SelectPerkAt", index);
            void Buy()
            {
                perks.OnClickBuyUpgrade()();
                perks.OnModalConfirmClick()();
            }

            await run.PhaseAsync(() =>
            {
                t.Step("open", () => perks = t.Open<PerksViewModel>(GuiWindowType.Perks, null));
                t.Step("select-first-unowned", () => Select(0));
                t.Step("select-second", () => Select(1));
                t.Step("buy-first-rank", () =>
                {
                    Select(0);
                    Buy();
                });
                t.Step("select-owned-perk", () => Select(0));
                t.Step("buy-to-maxed", () =>
                {
                    for (var rank = 0; rank < 8 && perks.IsBuyEnabled; rank++)
                        Buy();
                });
                t.Step("select-maxed-perk", () => Select(0));
                // A perk whose first rank grants an active ability feat that goes to the quick bar (PerkHotBarService in the split).
                var hotBarIndex = IndexWhere(perk =>
                {
                    var detail = Perk.GetPerkDetails(perk);
                    return perk != PerkType.AdrenalStim && detail.IsActive && detail.PerkLevels.TryGetValue(1, out var level) &&
                           level.Requirements.Count == 0 &&
                           level.GrantedFeats.Any(feat => AbilityCatalog.IsFeatRegistered(feat) &&
                                                          AbilityCatalog.GetAbilityDetail(feat).ImpactAction != null);
                });
                t.Step("hot-bar-perk-found", () => run.Check(hotBarIndex >= 0, "A perk that grants a quick bar feat is in the list."));
                t.Step("hot-bar-perk-select", () => Select(hotBarIndex));
                t.Step("hot-bar-perk-buy", Buy);
                t.Step("hot-bar-slot-assigned", () => { });
                t.Step("hot-bar-perk-select-owned", () => Select(hotBarIndex));
                t.Step("give-refund-tokens-for-hot-bar", () => Currency.GiveCurrency(run.Player, CurrencyType.PerkRefundToken, 3));
                t.Step("hot-bar-perk-refund", () =>
                {
                    perks.OnClickRefund()();
                    perks.OnModalConfirmClick()();
                });
                t.Step("hot-bar-slot-cleared", () => { });
                t.Step("select-refundable-perk", () => Select(0));
                t.Step("give-refund-tokens", () => Currency.GiveCurrency(run.Player, CurrencyType.PerkRefundToken, 3));
                t.Step("refund", () =>
                {
                    perks.OnClickRefund()();
                    perks.OnModalConfirmClick()();
                });
                t.Step("filter-owned", () => perks.OnClickFilterOwned()());
                t.Step("filter-can-buy", () => perks.OnClickFilterCanBuy()());
                t.Step("filter-maxed", () => perks.OnClickFilterMaxed()());
                t.Step("filter-all", () => perks.OnClickFilterAll()());
                t.Step("page-next", () => perks.OnClickNextPage()());
                t.Step("page-previous", () => perks.OnClickPreviousPage()());
                t.Step("search", () => perks.SearchText = "attack");
                t.Step("clear-search", () => perks.SearchText = string.Empty);
                t.Step("category", () => perks.SelectedPerkCategoryId = 1);
                t.Step("sort", () => perks.SelectedSortOrderId = 1);
                t.Step("category-all", () => perks.SelectedPerkCategoryId = 0);
                t.Step("beast-mode", () => perks.OnClickBeastPerks()());
                // The first perk of the list can need more than the seeded beast has; the step picks one it can buy.
                var beastId = DB.Get<Player>(run.Fixture.Id).ActiveBeastId;
                var beastBefore = DB.Get<Beast>(beastId);
                var beastSkillPoints = beastBefore.UnallocatedSP;
                var beastPerkRanks = beastBefore.Perks.Values.Sum();
                var beastPick = -2;
                t.Step("beast-select", () => Select(beastPick = IndexWhere(perk => CanBuyFirstRankForBeast(run.Player, perk, beastSkillPoints))));
                t.Step("beast-buy", Buy);
                var beastAfter = DB.Get<Beast>(beastId);
                run.Check(beastAfter.UnallocatedSP < beastSkillPoints && beastAfter.Perks.Values.Sum() > beastPerkRanks,
                    $"The beast bought a perk rank and spent skill points (picked {beastPick}, SP {beastSkillPoints} to {beastAfter.UnallocatedSP}).");
                t.Step("my-perks", () => perks.OnClickMyPerks()());
                t.Step("refresh-skill-xp", () => perks.Refresh(new SkillXPRefreshEvent(new List<SkillType> { SkillType.Force })));
                t.Step("refresh-reset", () => perks.Refresh(new PerkResetAcquiredRefreshEvent()));
                t.Step("refresh-cooldown", () => perks.Refresh(new PerkRefundCooldownResetRefreshEvent()));
                t.Step("close-and-reopen", () => perks = t.Reopen<PerksViewModel>(GuiWindowType.Perks, null));
            });
        }

        /// <summary>True when the perk's first rank passes the checks a purchase makes for the player and costs no more than the points held.</summary>
        private static bool CanBuyFirstRankForBeast(uint player, PerkType perk, int skillPoints)
        {
            var detail = Perk.GetPerkDetails(perk);
            if (!detail.PerkLevels.TryGetValue(1, out var level) || level.Price > skillPoints)
                return false;
            if (detail.PurchaseRequirement != null && !string.IsNullOrWhiteSpace(detail.PurchaseRequirement(player)))
                return false;
            return level.Requirements.All(requirement => string.IsNullOrWhiteSpace(requirement.CheckRequirements(player)));
        }

        // ------------------------------------------------------------------ character sheet

        private static void SeedSheet(PlayerAbilityFixture fixture) => SeedBeast(fixture, BeastType.Viper);

        private static async Task SheetScriptAsync(Run run)
        {
            var t = run.Transcript;
            CharacterSheetViewModel sheet = null;
            var npc = run.Context.SpawnCreature("nw_bandit001", 3f);
            var self = new CharacterSheetPayload(run.Player, true);
            await run.PhaseAsync(() =>
            {
                t.Step("open-self", () => sheet = t.Open<CharacterSheetViewModel>(GuiWindowType.CharacterSheet, self));
                foreach (var tab in new[] { 1, 2, 3, 0 })
                {
                    var id = tab;
                    t.Step("tab-" + id, () => sheet.SelectedTabId = id);
                }

                t.Step("upgrade-might", () =>
                {
                    sheet.OnClickUpgradeMight()();
                    sheet.OnModalConfirmClick()();
                });
                t.Step("upgrade-agility", () =>
                {
                    sheet.OnClickUpgradeAgility()();
                    sheet.OnModalConfirmClick()();
                });
                t.Step("skill-xp", () =>
                {
                    run.Fixture.Update(record => record.Skills[SkillType.Force].XP += 40);
                    Gui.PublishRefreshEvent(run.Player, new SkillXPRefreshEvent(new List<SkillType> { SkillType.Force }));
                });
                t.Step("stat-adjustment", () => Gui.PublishRefreshEvent(run.Player, new StatAdjustmentRefreshEvent()));
                t.Step("perk-acquired", () => sheet.Refresh(new PerkAcquiredRefreshEvent(PerkType.CombatAnalyzer)));
                t.Step("stats-tab", () => sheet.SelectedTabId = 1);
            });

            // A one-hand weapon, then a second weapon in the off hand, then the main hand weapon off again.
            await run.Context.EquipItemAsync(run.Player, "nw_wswss001", InventorySlot.RightHand);
            await run.Context.DelaySecondsAsync(1.5f);
            await run.PhaseAsync(() =>
            {
                t.Step("equip-main-hand", () => Gui.PublishRefreshEvent(run.Player, new EquipItemRefreshEvent()));
                t.Step("resistances-tab", () => sheet.SelectedTabId = 2);
                t.Step("stats-tab-again", () => sheet.SelectedTabId = 1);
            });
            await run.Context.EquipItemAsync(run.Player, "nw_wswss001", InventorySlot.LeftHand);
            await run.Context.DelaySecondsAsync(1.5f);
            await run.PhaseAsync(() =>
            {
                t.Step("equip-off-hand", () => Gui.PublishRefreshEvent(run.Player, new EquipItemRefreshEvent()));
                t.Step("crafting-tab", () => sheet.SelectedTabId = 3);
                t.Step("stats-tab-dual-wield", () => sheet.SelectedTabId = 1);
            });
            // The main hand weapon really leaves the hand in the engine. The engine then slides the off hand weapon into the main hand,
            // so the off hand is empty and the main hand holds the second weapon.
            var removed = OBJECT_INVALID;
            await run.Context.ExecuteInCreatureContextAsync(run.Player, () =>
            {
                removed = GetItemInSlot(InventorySlot.RightHand, run.Player);
                ClearAllActions();
                ActionUnequipItem(removed);
            });
            await run.Context.WaitUntilAsync(() => !GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, run.Player)), 5f,
                "the main hand weapon to be unequipped");
            await run.Context.DelaySecondsAsync(1.5f);
            run.Context.Assert(GetItemPossessor(removed) == run.Player && GetItemInSlot(InventorySlot.RightHand, run.Player) != removed,
                "The unequipped weapon is back in the pack, out of the main hand.");
            await run.PhaseAsync(() =>
            {
                t.Step("unequip-main-hand", () => Gui.PublishRefreshEvent(run.Player, new UnequipItemRefreshEvent()));
                t.Step("stats-tab-after-unequip", () => sheet.SelectedTabId = 1);
                t.Step("close-self", () => Gui.ClosePlayerWindow(run.Player, GuiWindowType.CharacterSheet));
                t.Step("open-npc", () => sheet = t.Open<CharacterSheetViewModel>(GuiWindowType.CharacterSheet, new CharacterSheetPayload(npc, false)));
                foreach (var tab in new[] { 1, 2, 3, 0 })
                {
                    var id = tab;
                    t.Step("npc-tab-" + id, () => sheet.SelectedTabId = id);
                }

                t.Step("close-npc", () => Gui.ClosePlayerWindow(run.Player, GuiWindowType.CharacterSheet));
            });
            // The sheet reads its HP maximum from the engine creature, so the large maximum is set on the creature and checked before the
            // sheet opens; the ordinary cases above keep the small one as the control.
            // A player's maximum is the sum of its per-level maximums, written the way the game writes them (255 is the most one level holds).
            const int LargeMaxHitPoints = 255;
            await run.Context.ExecuteInCreatureContextAsync(run.Player, () =>
            {
                var levels = GetLevelByPosition(1, run.Player) + GetLevelByPosition(2, run.Player) + GetLevelByPosition(3, run.Player);
                for (var level = 1; level <= levels; level++)
                    CreaturePlugin.SetMaxHitPointsByLevel(run.Player, level, LargeMaxHitPoints);
                SetCurrentHitPoints(run.Player, GetMaxHitPoints(run.Player));
            });
            await run.Context.WaitFrameAsync();
            await run.Context.ExecuteInCreatureContextAsync(run.Player, () =>
                run.Check(GetMaxHitPoints(run.Player) >= LargeMaxHitPoints,
                    "The player creature holds a large live HP maximum before the high-resource sheet opens."));
            await run.PhaseAsync(() =>
            {
                t.Step("open-high-resource", () =>
                {
                    run.Fixture.Update(record =>
                    {
                        record.MaxFP = 90000;
                        record.MaxStamina = 90000;
                        record.MaxHP = 60000;
                    });
                    sheet = t.Open<CharacterSheetViewModel>(GuiWindowType.CharacterSheet, self);
                });
                t.Step("high-resource-stats", () => sheet.SelectedTabId = 1);
            });
        }

        // ------------------------------------------------------------------ incubator

        private static void SeedIncubator(PlayerAbilityFixture fixture) => SeedBeast(fixture, BeastType.Viper);

        private static uint MakeDna(uint player, int attackPurity)
        {
            var dna = CreateItemOnObject(BeastMasteryService.DNAResref, player);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DNAType, (int)BeastType.Viper), dna);
            AddItemProperty(DurationType.Permanent,
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, attackPurity), dna);
            AddItemProperty(DurationType.Permanent,
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, 300), dna);
            return dna;
        }

        private static async Task IncubatorScriptAsync(Run run)
        {
            var payload = new IncubatorPayload("engine-transcript-incubator/" + run.Fixture.Id, string.Empty);
            // The incubator reads the server day from the real clock unless the player has an override: a Wednesday in every run.
            BeastMastery.SetIncubationDayOverride(run.Fixture.Id, DayOfWeek.Wednesday);
            try
            {
                await IncubatorPhasesAsync(run, payload);
            }
            finally
            {
                BeastMastery.ClearIncubationDayOverride(run.Fixture.Id);
            }
        }

        private static async Task IncubatorPhasesAsync(Run run, IncubatorPayload payload)
        {
            var t = run.Transcript;
            IncubatorViewModel incubator = null;
            await run.PhaseAsync(() =>
            {
                t.Track("jobs", () =>
                {
                    var all = new JObject();
                    var jobs = DB.Search(new DBQuery<IncubationJob>()
                        .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), payload.PropertyId, false)).ToList();
                    for (var index = 0; index < jobs.Count; index++)
                        all["job" + index] = new JObject
                        {
                            ["stage"] = jobs[index].CurrentStage, ["type"] = jobs[index].BeastDNAType.ToString(),
                            ["started"] = jobs[index].DateStarted, ["completed"] = jobs[index].DateCompleted
                        };
                    return all;
                });
                t.Step("open-new-job", () => incubator = t.Open<IncubatorViewModel>(GuiWindowType.Incubator, payload));
                t.Step("toggle-resistances", () => incubator.OnClickToggleResistances()());
                t.Step("toggle-resistances-back", () => incubator.OnClickToggleResistances()());
                t.Step("toggle-erratic-genius", () => incubator.OnClickErraticGeniusToggled()());
                t.Step("toggle-allow-mutation", () => incubator.OnClickAllowMutationToggled()());
            });

            var dna = 0u;
            await run.PhaseAsync(() =>
            {
                dna = MakeDna(run.Player, 500);
                t.Step("select-dna-prompt", () => incubator.OnClickDNA()());
                t.Step("select-dna", () => t.Target(dna));
            });
            await run.PhaseAsync(() =>
            {
                var enzymes = new (string Name, string Resref, Func<Action> Click)[]
                {
                    ("hydrolase", "hydrolase_blue", () => incubator.OnClickHydrolase()),
                    ("isomerase", "isomerase_red", () => incubator.OnClickIsomerase()),
                    ("lyase", "lyase_blue", () => incubator.OnClickLyase())
                };
                foreach (var enzyme in enzymes)
                {
                    var item = CreateItemOnObject(enzyme.Resref, run.Player);
                    var click = enzyme.Click;
                    t.Step("select-" + enzyme.Name + "-prompt", () => click()());
                    t.Step("select-" + enzyme.Name, () => t.Target(item));
                }
            });
            await run.PhaseAsync(() =>
            {
                void SelectEnzymes(string label)
                {
                    foreach (var enzyme in new[] { ("hydrolase", "hydrolase_blue"), ("isomerase", "isomerase_red"), ("lyase", "lyase_blue") })
                    {
                        var item = CreateItemOnObject(enzyme.Item2, run.Player);
                        var click = enzyme.Item1 == "hydrolase" ? incubator.OnClickHydrolase()
                            : enzyme.Item1 == "isomerase" ? incubator.OnClickIsomerase() : incubator.OnClickLyase();
                        t.Step(label + "-select-" + enzyme.Item1 + "-prompt", () => click());
                        t.Step(label + "-select-" + enzyme.Item1, () => t.Target(item));
                    }
                }

                t.Step("remove-lyase", () =>
                {
                    incubator.OnClickLyase()();
                    incubator.OnModalConfirmClick()();
                });
                var lyase = CreateItemOnObject("lyase_blue", run.Player);
                t.Step("select-lyase-again-prompt", () => incubator.OnClickLyase()());
                t.Step("select-lyase-again", () => t.Target(lyase));
                t.Step("start-job", () =>
                {
                    incubator.OnClickStartJob()();
                    incubator.OnModalConfirmClick()();
                });
                t.Step("in-progress-reopen", () => incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload));
                t.Step("clock-half-stage", () => FrozenClockAdvance(run, 3600 * 6));
                t.Step("in-progress-refresh", () => incubator.Refresh(new PerkAcquiredRefreshEvent(PerkType.CombatAnalyzer)));
                t.Step("stage-complete", () =>
                {
                    FrozenClockAdvance(run, 3600 * 40);
                    incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload);
                });
                t.Step("stage-2-ready", () =>
                {
                    FrozenClockAdvance(run, 3600 * 40);
                    incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload);
                });
                SelectEnzymes("stage-2");
                t.Step("continue-job", () =>
                {
                    incubator.OnClickContinueJob()();
                    incubator.OnModalConfirmClick()();
                });
                t.Step("reopen-after-continue", () => incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload));
                t.Step("cancel-job", () =>
                {
                    incubator.OnClickCancelJob()();
                    incubator.OnModalConfirmClick()();
                });
                var dna2 = MakeDna(run.Player, 700);
                t.Step("select-dna-for-complete-prompt", () => incubator.OnClickDNA()());
                t.Step("select-dna-for-complete", () => t.Target(dna2));
                t.Step("start-second-job", () =>
                {
                    incubator.OnClickStartJob()();
                    incubator.OnModalConfirmClick()();
                });
                // Every stage runs: finish it, put the enzymes in, start the next one.
                for (var stage = 2; stage <= IncubationProjection.NumberOfStages; stage++)
                {
                    var number = stage;
                    t.Step("stage-" + number + "-ready", () =>
                    {
                        FrozenClockAdvance(run, 3600 * 40);
                        incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload);
                    });
                    SelectEnzymes("second-stage-" + number);
                    t.Step("second-job-start-stage-" + number, () =>
                    {
                        incubator.OnClickContinueJob()();
                        incubator.OnModalConfirmClick()();
                    });
                }

                t.Step("all-stages-complete", () =>
                {
                    FrozenClockAdvance(run, 3600 * 40);
                    incubator = t.Reopen<IncubatorViewModel>(GuiWindowType.Incubator, payload);
                });
                t.Step("complete-job", () =>
                {
                    incubator.OnClickCompleteJob()();
                    incubator.OnModalConfirmClick()();
                });
                // The job reached its last stage, so completing it hands out the egg and closes the job.
                run.Check(FullInventoryFixture.CountCarried(run.Player, "beast_egg") == 1, "Completing the last stage creates the egg.");
                run.Check(!DB.Search(new DBQuery<IncubationJob>()
                    .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), payload.PropertyId, false)).Any(), "Completing the job closes it.");
            });

            // The inventory is full while the window closes, so the DNA cannot go back to the player and stays held.
            await run.PhaseAsync(() =>
            {
                var dna3 = MakeDna(run.Player, 900);
                t.Step("hold-items-prompt", () => incubator.OnClickDNA()());
                t.Step("hold-items", () => t.Target(dna3));
            });
            List<uint> fillers = null;
            await run.Context.ExecuteInCreatureContextAsync(run.Player, () => fillers = FullInventoryFixture.Fill(run.Context, run.Player));
            await run.PhaseAsync(() =>
            {
                t.Step("close-with-held-items", () =>
                {
                    incubator.OnWindowClosed()();
                    Gui.ClosePlayerWindow(run.Player, GuiWindowType.Incubator);
                });
                run.Check(!string.IsNullOrWhiteSpace(((IIncubatorHost)incubator).Session.DnaItem), "The DNA stays held when the inventory is full.");
            });
            await run.Context.ExecuteInCreatureContextAsync(run.Player, () => FullInventoryFixture.Free(fillers));
            await run.Context.WaitFrameAsync();
            await run.PhaseAsync(() =>
            {
                t.Step("reopen-with-held-items", () => incubator = t.Open<IncubatorViewModel>(GuiWindowType.Incubator, payload));
                run.Check(!string.IsNullOrWhiteSpace(((IIncubatorHost)incubator).Session.DnaItem), "The held DNA is in the slot after the window opens again.");
            });
        }

        private static void FrozenClockAdvance(Run run, double seconds) => run.Clock.Advance(seconds);

        // ------------------------------------------------------------------ disguises

        private static void SeedDisguise(PlayerAbilityFixture fixture)
        {
        }

        private static async Task DisguiseScriptAsync(Run run)
        {
            var t = run.Transcript;
            DisguiseViewModel disguise = null;
            await run.PhaseAsync(() =>
            {
                t.Step("open-empty", () => disguise = t.Open<DisguiseViewModel>(GuiWindowType.Disguises, null));
                t.Step("retired-tab", () => disguise.OnClickRetired()());
                t.Step("available-tab", () => disguise.OnClickAvailable()());
                t.Step("new", () =>
                {
                    disguise.OnClickNew()();
                    disguise.OnModalConfirmClick()();
                });
                t.Step("edit-fields", () =>
                {
                    disguise.PrivateName = "Transcript Disguise";
                    disguise.Descriptor = "a hooded stranger";
                    disguise.Biography = "Only a transcript.";
                });
                t.Step("next-portrait", () => disguise.OnClickNextPortrait()());
                t.Step("previous-portrait", () => disguise.OnClickPreviousPortrait()());
                t.Step("sound-set-next-page", () => disguise.OnClickNextSoundSetPage()());
                t.Step("sound-set-previous-page", () => disguise.OnClickPreviousSoundSetPage()());
                t.Step("sound-set-select", () => disguise.SelectedSoundSetIndex = 1);
                t.Step("sound-set-page-select", () => disguise.SelectedSoundSetPageIndex = 1);
                t.Step("sound-set-preview", () => disguise.OnClickPreviewSoundSet()());
                t.Step("save", () => disguise.OnClickSave()());
                t.Step("edit", () => disguise.OnClickEdit()());
                t.Step("cancel-edit", () => disguise.OnClickCancelEdit()());
                // The disguise id is a new Guid every run; the transcript names it.
                t.Mask(DB.Search(new DBQuery<PlayerDisguise>().AddFieldSearch(nameof(PlayerDisguise.PlayerId), run.Fixture.Id, false)).First().Id,
                    "<disguise>");
                t.Step("activate", () =>
                {
                    disguise.OnClickActivateOrDeactivate()();
                    disguise.OnModalConfirmClick()();
                });
                t.Step("deactivate", () =>
                {
                    disguise.OnClickActivateOrDeactivate()();
                    disguise.OnModalConfirmClick()();
                });
                t.Step("retire", () =>
                {
                    disguise.OnClickRetire()();
                    disguise.OnModalConfirmClick()();
                });
                t.Step("retired-tab-with-disguise", () => disguise.OnClickRetired()());
                t.Step("unretire", () =>
                {
                    disguise.OnClickUnretire()();
                    disguise.OnModalConfirmClick()();
                });
                t.Step("perk-refunded", () => disguise.Refresh(new PerkRefundedRefreshEvent(PerkType.CombatAnalyzer)));
                t.Step("perk-acquired", () => disguise.Refresh(new PerkAcquiredRefreshEvent(PerkType.CombatAnalyzer)));
                t.Step("close-and-reopen", () => disguise = t.Reopen<DisguiseViewModel>(GuiWindowType.Disguises, null));
            });
        }

        // ------------------------------------------------------------------ droid assembly

        private static void SeedDroid(PlayerAbilityFixture fixture) =>
            fixture.Update(record => record.Skills[SkillType.Engineering].Rank = 40);

        private static async Task DroidScriptAsync(Run run)
        {
            var t = run.Transcript;
            DroidAssemblyViewModel droid = null;
            var parts = new[] { "d_as_cpu1_a", "d_hd_mpv1", "d_bd_mp1", "d_ar_mpv1", "d_lg_mpv1" };
            await run.PhaseAsync(() => t.Step("open", () => droid = t.Open<DroidAssemblyViewModel>(GuiWindowType.DroidAssembly, null)));
            var items = new List<uint>();
            await run.PhaseAsync(() =>
            {
                var handlers = new Func<Action>[]
                {
                    () => droid.OnClickCPU(), () => droid.OnClickHead(), () => droid.OnClickBody(), () => droid.OnClickArms(),
                    () => droid.OnClickLegs()
                };
                for (var index = 0; index < parts.Length; index++)
                {
                    var slot = index;
                    var item = CreateItemOnObject(parts[index], run.Player);
                    items.Add(item);
                    t.Step("install-" + parts[index] + "-prompt", () => handlers[slot]()());
                    t.Step("install-" + parts[index], () => t.Target(item));
                }

                t.Step("name", () => droid.Name = "Transcript Droid");
                t.Step("personality", () => droid.PersonalityIndex = 1);
                t.Step("remove-legs", () =>
                {
                    handlers[4]()();
                    droid.OnModalConfirmClick()();
                });
                var wrong = CreateItemOnObject("d_hd_mpv1", run.Player);
                t.Step("wrong-part-prompt", () => handlers[4]()());
                t.Step("wrong-part", () => t.Target(wrong));
                var legs = CreateItemOnObject("d_lg_mpv1", run.Player);
                t.Step("reinstall-legs-prompt", () => handlers[4]()());
                t.Step("reinstall-legs", () => t.Target(legs));
                t.Step("reset", () =>
                {
                    droid.OnClickReset()();
                    droid.OnModalConfirmClick()();
                });
            });
            await run.PhaseAsync(() =>
            {
                var handlers = new Func<Action>[]
                {
                    () => droid.OnClickCPU(), () => droid.OnClickHead(), () => droid.OnClickBody(), () => droid.OnClickArms(),
                    () => droid.OnClickLegs()
                };
                // The reset left the window without a build in progress; a second droid starts the way a player starts one.
                t.Step("new-droid-before-assembly", () => droid.OnClickNewDroid()());
                for (var index = 0; index < parts.Length; index++)
                {
                    var slot = index;
                    var item = CreateItemOnObject(parts[index], run.Player);
                    t.Step("assemble-" + parts[index] + "-prompt", () => handlers[slot]()());
                    t.Step("assemble-" + parts[index], () => t.Target(item));
                }

                const int droidPersonalityIndex = 1;
                t.Step("personality-before-construct", () => droid.PersonalityIndex = droidPersonalityIndex);
                t.Step("construct", () =>
                {
                    droid.Name = "Transcript Droid";
                    droid.OnClickConstruct()();
                    droid.OnModalConfirmClick()();
                });
                run.Check(FullInventoryFixture.CountCarried(run.Player, DroidService.DroidControlItemResref) == 1,
                    "Constructing creates the droid controller.");
                var controller = GetFirstItemInInventory(run.Player);
                while (GetIsObjectValid(controller) && GetResRef(controller) != DroidService.DroidControlItemResref)
                    controller = GetNextItemInInventory(run.Player);
                var personality = false;
                for (var property = GetFirstItemProperty(controller); GetIsItemPropertyValid(property); property = GetNextItemProperty(controller))
                {
                    if (GetItemPropertyType(property) == ItemPropertyType.DroidPersonality)
                        personality |= GetItemPropertySubType(property) == droidPersonalityIndex;
                }

                run.Check(personality, "The droid controller carries a DroidPersonality item property with the chosen personality.");
                t.Step("new-droid", () => droid.OnClickNewDroid()());
                t.Step("close-and-reopen", () => droid = t.Reopen<DroidAssemblyViewModel>(GuiWindowType.DroidAssembly, null));
            });
        }

        // ------------------------------------------------------------------ comparison

        private static object GetStatic(Type type, string name) =>
            type.GetField(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!.GetValue(null);

        private static void Compare(EngineTestContext ctx, string name, string actual, string problems)
        {
            var resource = "ViewModelTranscript/" + name + ".txt";
            using var stream = typeof(ViewModelBindingTranscriptEngineTests).Assembly.GetManifestResourceStream(resource);
            var capture = "/nwn/home/viewmodel-transcript-" + name + ".txt";
            // The capture goes to the server home when it exists; the message says where it went, or that it went nowhere.
            string Capture()
            {
                if (!Directory.Exists("/nwn/home"))
                    return $"/nwn/home does not exist, so the {actual.Length} chars of the transcript were not written anywhere";
                try
                {
                    File.WriteAllText(capture, actual, new UTF8Encoding(false));
                    return $"the transcript ({actual.Length} chars) was written to {capture}";
                }
                catch (Exception exception)
                {
                    return $"writing the transcript to {capture} failed ({exception.GetType().Name}: {exception.Message})";
                }
            }

            if (stream == null)
            {
                ctx.Assert(false, $"No fixture is embedded; {Capture()}.{problems}");
                return;
            }

            var expected = new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            if (expected != actual)
            {
                var captured = Capture();
                var expectedLines = expected.Split('\n');
                var actualLines = actual.Split('\n');
                var index = 0;
                while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
                    index++;
                ctx.Assert(false, $"{name} transcript differs at line {index + 1}: expected '{Shorten(index < expectedLines.Length ? expectedLines[index] : "<end>")}' " +
                                  $"but got '{Shorten(index < actualLines.Length ? actualLines[index] : "<end>")}'; {captured}.{problems}");
                return;
            }

            ctx.Assert(problems.Length == 0, problems.Trim());
            ctx.SetResultDetail($"{actual.Split('\n').Length - 1} lines, identical to the captured transcript.");
        }

        private static string Shorten(string line) => line.Length <= 300 ? line : line.Substring(0, 300) + "...";
    }
}
