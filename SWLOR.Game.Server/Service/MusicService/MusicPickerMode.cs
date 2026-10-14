namespace SWLOR.Game.Server.Service.MusicService
{
    public enum MusicPickerMode
    {
        // A portable music player: the player's own collection, heard only by them.
        Personal = 0,

        // A jukebox: the owner's collection (or the user's, if unowned), heard by the whole area.
        Jukebox = 1,

        // A DM: every track, heard by the whole area.
        DungeonMaster = 2,
    }
}
