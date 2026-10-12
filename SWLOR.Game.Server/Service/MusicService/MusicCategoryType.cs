namespace SWLOR.Game.Server.Service.MusicService
{
    public enum MusicCategoryType
    {
        [MusicCategory("Invalid")]
        Invalid = 0,
        [MusicCategory("Cantina")]
        Cantina = 1,
        [MusicCategory("Battle")]
        Battle = 2,
        [MusicCategory("Themes")]
        Theme = 3,
        [MusicCategory("Locations")]
        Location = 4,
        [MusicCategory("Ambient")]
        Ambient = 5,
    }
}
