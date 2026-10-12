using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
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
    }
}
