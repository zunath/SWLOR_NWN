using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    /// <summary>
    /// The Portable Music Player and the song discs only Engineers can record.
    /// </summary>
    public class MusicRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Portable Music Player
            _builder.Create(RecipeType.PortableMusicPlayer, SkillType.Engineering)
                .Category(RecipeCategoryType.MusicPlayers)
                .Resref("music_player")
                .Level(5)
                .Quantity(1)
                .Component("elec_ruined", 3)
                .Component("ref_tilarium", 2);

            // Song Disc: Tavern 1
            _builder.Create(RecipeType.SongDiscTavern1, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_22")
                .Level(1)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Tavern 2
            _builder.Create(RecipeType.SongDiscTavern2, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_23")
                .Level(2)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Tavern 3
            _builder.Create(RecipeType.SongDiscTavern3, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_24")
                .Level(3)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Tavern 4
            _builder.Create(RecipeType.SongDiscTavern4, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_56")
                .Level(4)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Celebration
            _builder.Create(RecipeType.SongDiscCelebration, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_450")
                .Level(5)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Humor 1
            _builder.Create(RecipeType.SongDiscHumor1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_456")
                .Level(6)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Holidays 1
            _builder.Create(RecipeType.SongDiscHolidays1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_459")
                .Level(7)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Holidays 2
            _builder.Create(RecipeType.SongDiscHolidays2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_460")
                .Level(8)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Generic 1
            _builder.Create(RecipeType.SongDiscGeneric1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_451")
                .Level(9)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Generic 2
            _builder.Create(RecipeType.SongDiscGeneric2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_452")
                .Level(10)
                .Quantity(1)
                .Component("elec_ruined", 2)
                .Component("ref_tilarium", 1);

            // Song Disc: Generic 3
            _builder.Create(RecipeType.SongDiscGeneric3, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_453")
                .Level(11)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Sunrise 2
            _builder.Create(RecipeType.SongDiscSunrise2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_445")
                .Level(12)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Sunset 2
            _builder.Create(RecipeType.SongDiscSunset2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_447")
                .Level(13)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 1
            _builder.Create(RecipeType.SongDiscRace1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_578")
                .Level(14)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 2
            _builder.Create(RecipeType.SongDiscRace2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_579")
                .Level(15)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 3
            _builder.Create(RecipeType.SongDiscRace3, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_580")
                .Level(16)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 4
            _builder.Create(RecipeType.SongDiscRace4, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_581")
                .Level(17)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 5
            _builder.Create(RecipeType.SongDiscRace5, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_582")
                .Level(18)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Race 6
            _builder.Create(RecipeType.SongDiscRace6, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_583")
                .Level(19)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Feel Hot Nightclub
            _builder.Create(RecipeType.SongDiscFeelHotNightclub, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_277")
                .Level(20)
                .Quantity(1)
                .Component("elec_flawed", 2)
                .Component("ref_currian", 1);

            // Song Disc: Musik Box Nightclub
            _builder.Create(RecipeType.SongDiscMusikBoxNightclub, SkillType.Engineering)
                .Category(RecipeCategoryType.CantinaMusicDiscs)
                .Resref("song_279")
                .Level(21)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Gloom 1
            _builder.Create(RecipeType.SongDiscGloom1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_454")
                .Level(22)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Gloom 2
            _builder.Create(RecipeType.SongDiscGloom2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_455")
                .Level(23)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Mystery 1
            _builder.Create(RecipeType.SongDiscMystery1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_474")
                .Level(24)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Tragic 2
            _builder.Create(RecipeType.SongDiscTragic2, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_479")
                .Level(25)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Opera 1
            _builder.Create(RecipeType.SongDiscOpera1, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_475")
                .Level(26)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Baroque Recital
            _builder.Create(RecipeType.SongDiscBaroqueRecital, SkillType.Engineering)
                .Category(RecipeCategoryType.AmbientMusicDiscs)
                .Resref("song_449")
                .Level(27)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: City Marketplace
            _builder.Create(RecipeType.SongDiscCityMarketplace, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_20")
                .Level(28)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Generic Store
            _builder.Create(RecipeType.SongDiscGenericStore, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_26")
                .Level(29)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: Generic Wealthy House
            _builder.Create(RecipeType.SongDiscGenericWealthyHouse, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_25")
                .Level(30)
                .Quantity(1)
                .Component("elec_good", 2)
                .Component("ref_idailia", 1);

            // Song Disc: City Wealthy
            _builder.Create(RecipeType.SongDiscCityWealthy, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_19")
                .Level(31)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Design Salon 1
            _builder.Create(RecipeType.SongDiscDesignSalon1, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_437")
                .Level(32)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Design Salon 3
            _builder.Create(RecipeType.SongDiscDesignSalon3, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_439")
                .Level(33)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Jungle 1
            _builder.Create(RecipeType.SongDiscJungle1, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_440")
                .Level(34)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Jungle 2
            _builder.Create(RecipeType.SongDiscJungle2, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_441")
                .Level(35)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Swamp 1
            _builder.Create(RecipeType.SongDiscSwamp1, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_442")
                .Level(36)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Swamp 2
            _builder.Create(RecipeType.SongDiscSwamp2, SkillType.Engineering)
                .Category(RecipeCategoryType.LocationMusicDiscs)
                .Resref("song_443")
                .Level(37)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Battle 1
            _builder.Create(RecipeType.SongDiscBattle1, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_511")
                .Level(38)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Battle 2
            _builder.Create(RecipeType.SongDiscBattle2, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_512")
                .Level(39)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Battle 3
            _builder.Create(RecipeType.SongDiscBattle3, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_513")
                .Level(40)
                .Quantity(1)
                .Component("elec_imperfect", 2)
                .Component("ref_barinium", 1);

            // Song Disc: Battle 4
            _builder.Create(RecipeType.SongDiscBattle4, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_514")
                .Level(41)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Battle 5
            _builder.Create(RecipeType.SongDiscBattle5, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_520")
                .Level(42)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Battle 6
            _builder.Create(RecipeType.SongDiscBattle6, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_524")
                .Level(43)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Battle 7
            _builder.Create(RecipeType.SongDiscBattle7, SkillType.Engineering)
                .Category(RecipeCategoryType.BattleMusicDiscs)
                .Resref("song_527")
                .Level(44)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Theme Republic
            _builder.Create(RecipeType.SongDiscThemeRepublic, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_396")
                .Level(45)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Theme Czerka
            _builder.Create(RecipeType.SongDiscThemeCzerka, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_390")
                .Level(46)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Rebel Alliance
            _builder.Create(RecipeType.SongDiscRebelAlliance, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_538")
                .Level(47)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Stormtroopers
            _builder.Create(RecipeType.SongDiscStormtroopers, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_549")
                .Level(48)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Theme, Lando
            _builder.Create(RecipeType.SongDiscThemeLando, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_472")
                .Level(49)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);

            // Song Disc: Lando Calrissian
            _builder.Create(RecipeType.SongDiscLandoCalrissian, SkillType.Engineering)
                .Category(RecipeCategoryType.ThemeMusicDiscs)
                .Resref("song_539")
                .Level(50)
                .Quantity(1)
                .Component("elec_high", 2)
                .Component("ref_gostian", 1);
        }
    }
}
