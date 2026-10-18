using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MusicCollectionEngineTests
    {
        [EngineTest("Players start with exactly the two starter songs", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task StarterSongsAreGranted(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var starters = MusicSongCatalog.Songs
                .Where(song => song.Acquisition == MusicAcquisitionType.Starter)
                .Select(song => song.TrackId)
                .ToHashSet();

            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.GrantStarterSongs(player.Creature));
            ctx.Assert(Music.GetCollectedTrackIds(player.Id).SetEquals(starters),
                $"collection is exactly the starter songs ({string.Join(", ", Music.GetCollectedTrackIds(player.Id))})");

            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.GrantStarterSongs(player.Creature));
            ctx.AssertEqual(starters.Count, Music.GetCollectedTrackIds(player.Id).Count, "starter songs after a second grant");
        }

        [EngineTest("Using a song disc adds its song to the collection", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task SongDiscUnlocksSong(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var song = MusicSongCatalog.Songs.First(x => x.Acquisition == MusicAcquisitionType.Loot);
            var disc = CreateObject(ObjectType.Item, $"song_{song.TrackId}", ctx.GetArenaLocation(1f, 1f));
            ctx.Track(disc);
            ctx.Assert(GetIsObjectValid(disc), $"song_{song.TrackId} blueprint creates an item");
            ctx.AssertEqual((int)song.KeyItem, GetLocalInt(disc, "KEY_ITEM_ID"), "disc KEY_ITEM_ID");
            ctx.AssertEqual("KEY_ITEM", GetTag(disc), "disc tag routes to the key item use handler");

            await ctx.ExecuteInCreatureContextAsync(player.Creature,
                () => KeyItem.GiveKeyItem(player.Creature, (KeyItemType)GetLocalInt(disc, "KEY_ITEM_ID")));
            ctx.Assert(Music.GetCollectedTrackIds(player.Id).Contains(song.TrackId), "song is in the collection after using the disc");
        }

        [EngineTest("Song discs and the music player list under the Music market category", Category = "MusicCollection", TimeoutSeconds = 30f)]
        public static async Task MusicItemsUseMusicMarketCategory(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            foreach (var resref in new[] { "music_player", "song_339", "song_22", "song_516" })
            {
                var item = CreateObject(ObjectType.Item, resref, ctx.GetArenaLocation(1f, 1f));
                ctx.Track(item);
                ctx.Assert(GetIsObjectValid(item), $"{resref} creates an item");
                ctx.AssertEqual(MarketCategoryType.Music, PlayerMarket.GetItemMarketCategory(item), $"{resref} market category");
            }

            var map = CreateObject(ObjectType.Item, "map_023", ctx.GetArenaLocation(1f, 1f));
            ctx.Track(map);
            ctx.Assert(PlayerMarket.GetItemMarketCategory(map) != MarketCategoryType.Music, "other key items stay out of Music");
        }

        [EngineTest("World jukebox lists only the interacting player's songs", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task WorldJukeboxFiltersSongs(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var jukebox = CreateObject(ObjectType.Placeable, "jukebox", ctx.GetArenaLocation(2f, 2f));
            ctx.Track(jukebox);
            ctx.Assert(GetIsObjectValid(jukebox), "world jukebox spawns");
            ctx.AssertEqual(string.Empty, Music.GetJukeboxOwnerId(jukebox), "world jukebox has no property owner");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.GrantStarterSongs(player.Creature));

            var model = new MusicPickerViewModel();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GuiViewModelBase<MusicPickerViewModel, GuiPayloadBase>).GetProperty("Player", flags)
                .SetValue(model, player.Creature);
            typeof(MusicPickerViewModel).GetField("_mode", flags).SetValue(model, MusicPickerMode.Jukebox);
            typeof(MusicPickerViewModel).GetField("_jukebox", flags).SetValue(model, jukebox);
            typeof(MusicPickerViewModel).GetMethod("LoadEntries", flags).Invoke(model, null);

            var entries = (MusicPickerEntry[])typeof(MusicPickerViewModel).GetField("_entries", flags).GetValue(model);
            var collected = Music.GetCollectedTrackIds(player.Id);
            ctx.Assert(collected.SetEquals(entries.Select(entry => entry.TrackId)), "picker contains exactly the player's collected songs");
            ctx.Assert(entries.All(entry => entry.IsAvailable), "every listed song can be played");
            ctx.AssertEqual($"All Songs ({collected.Count})", model.CategoryNames[0], "category count excludes locked songs");
            model.ShowUncollected = true;
            ctx.AssertEqual(collected.Count, model.SongNames.Count, "client toggle cannot expose locked songs");

            var newSong = MusicSongCatalog.Songs.First(song => song.Category == MusicCategoryType.Theme);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => KeyItem.GiveKeyItem(player.Creature, newSong.KeyItem));
            model.Refresh(new KeyItemReceivedRefreshEvent(newSong.KeyItem));
            ctx.AssertEqual(collected.Count + 1, model.SongNames.Count, "newly collected song appears while the jukebox is open");
            ctx.AssertEqual(4, model.CategoryNames.Count, "newly collected category joins All Songs and the starter categories");
            ctx.AssertEqual(model.CategoryNames.Count, model.CategorySelected.Count, "category selections refresh with the available categories");
        }

        [EngineTest("A jukebox in a property plays from its owner's collection", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task JukeboxUsesOwnerCollection(EngineTestContext ctx)
        {
            using var owner = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var jukebox = CreateObject(ObjectType.Placeable, "jukebox", ctx.GetArenaLocation(2f, 2f));
            ctx.Track(jukebox);
            ctx.Assert(GetIsObjectValid(jukebox), "jukebox placeable spawns");
            ctx.AssertEqual(string.Empty, Music.GetJukeboxOwnerId(jukebox), "unowned jukebox owner");

            var property = new WorldProperty
            {
                CustomName = "Music test jukebox",
                PropertyType = PropertyType.Structure,
                OwnerPlayerId = owner.Id,
                StructureType = StructureType.Jukebox,
            };
            DB.Set(property);
            try
            {
                SetLocalString(jukebox, "PROPERTY_ID", property.Id);
                ctx.AssertEqual(owner.Id, Music.GetJukeboxOwnerId(jukebox), "property jukebox owner");
            }
            finally
            {
                DB.Delete<WorldProperty>(property.Id);
            }
        }

        [EngineTest("A city jukebox plays from the current mayor's collection", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task CityJukeboxUsesCurrentMayor(EngineTestContext ctx)
        {
            await ctx.WaitFrameAsync();
            var jukebox = CreateObject(ObjectType.Placeable, "jukebox", ctx.GetArenaLocation(2f, 2f));
            ctx.Track(jukebox);

            // Structures keep the owner they were placed under; an election only changes the city's owner.
            var city = new WorldProperty
            {
                CustomName = "Music test city",
                PropertyType = PropertyType.City,
                OwnerPlayerId = "music-test-current-mayor",
                ParentPropertyId = GetResRef(GetArea(jukebox)),
            };
            var structure = new WorldProperty
            {
                CustomName = "Music test city jukebox",
                PropertyType = PropertyType.Structure,
                OwnerPlayerId = "music-test-former-mayor",
                ParentPropertyId = city.Id,
                StructureType = StructureType.Jukebox,
            };
            DB.Set(city);
            DB.Set(structure);
            try
            {
                SetLocalString(jukebox, "PROPERTY_ID", structure.Id);
                ctx.AssertEqual(city.OwnerPlayerId, Music.GetJukeboxOwnerId(jukebox), "city jukebox owner");
            }
            finally
            {
                DB.Delete<WorldProperty>(structure.Id);
                DB.Delete<WorldProperty>(city.Id);
            }
        }

        [EngineTest("Saved music settings the player has not collected are cleared", Category = "MusicCollection", TimeoutSeconds = 45f)]
        public static async Task UncollectedMusicSettingsAreCleared(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.GrantStarterSongs(player.Creature));
            var collected = MusicSongCatalog.Songs.First(song => song.Acquisition == MusicAcquisitionType.Starter);
            var uncollected = MusicSongCatalog.Songs.First(song => song.Acquisition == MusicAcquisitionType.BossDrop);

            var dbPlayer = DB.Get<Player>(player.Id);
            dbPlayer.Settings.PersonalMusicTrackId = collected.TrackId;
            dbPlayer.Settings.BattleThemeId = uncollected.TrackId;
            DB.Set(dbPlayer);

            Music.ClearUncollectedMusicSettings(player.Id);
            dbPlayer = DB.Get<Player>(player.Id);
            ctx.AssertEqual(collected.TrackId, dbPlayer.Settings.PersonalMusicTrackId, "collected personal track is kept");
            ctx.Assert(dbPlayer.Settings.BattleThemeId == null, "uncollected battle theme is cleared");
        }

        [EngineTest("A battle theme plays while the player fights and stops after combat", Category = "MusicCollection", TimeoutSeconds = 90f)]
        public static async Task BattleThemePlaysDuringCombat(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 0f);
            SetPlotFlag(player.Creature, true);
            // Real players get these scripts on module entry, which the fixture skips.
            SetEventScript(player.Creature, EventScript.Creature_OnMeleeAttacked, ScriptName.OnPlayerAttacked);
            SetEventScript(player.Creature, EventScript.Creature_OnDamaged, ScriptName.OnPlayerDamaged);
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.GrantStarterSongs(player.Creature));
            var theme = MusicSongCatalog.Songs.First(song => song.Acquisition == MusicAcquisitionType.Starter);

            var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
            ctx.MakeHostile(enemy);
            SetPlotFlag(enemy, true);
            try
            {
                // Headless player fixtures never swing, so the enemy attacks the player.
                await ctx.ExecuteInCreatureContextAsync(enemy, () => ActionAttack(player.Creature));
                await ctx.WaitUntilAsync(() => Combat.HasRecentAttackActivity(enemy, 1f), 15f, "the enemy to attack the player");
                ctx.Assert(!Music.IsBattleThemePlaying(player.Creature), "no battle theme plays without one selected");

                await ctx.ExecuteInCreatureContextAsync(player.Creature, () => Music.SetBattleTheme(player.Creature, theme.TrackId));
                await ctx.ExecuteInCreatureContextAsync(enemy, () =>
                {
                    ClearAllActions();
                    ActionAttack(player.Creature);
                });
                await ctx.WaitUntilAsync(() => Music.IsBattleThemePlaying(player.Creature), 15f, "the battle theme to start in combat");

                await ctx.WaitUntilAsync(() => GetIsInCombat(player.Creature), 15f, "the player to be in combat");
                await ctx.ExecuteInCreatureContextAsync(player.Creature, Music.StopBattleThemeAfterCombat);
                ctx.Assert(Music.IsBattleThemePlaying(player.Creature), "the battle theme keeps playing while in combat");

                DestroyObject(enemy);
                await ctx.WaitUntilAsync(() => !GetIsInCombat(player.Creature), 45f, "the player to leave combat");
                await ctx.ExecuteInCreatureContextAsync(player.Creature, Music.StopBattleThemeAfterCombat);
                ctx.Assert(!Music.IsBattleThemePlaying(player.Creature), "the battle theme stops after combat");
            }
            finally
            {
                if (GetIsObjectValid(enemy))
                    DestroyObject(enemy);
            }
        }
    }
}
