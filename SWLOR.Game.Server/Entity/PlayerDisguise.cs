namespace SWLOR.Game.Server.Entity
{
    public class PlayerDisguise: EntityBase
    {
        public PlayerDisguise()
        {
            PlayerId = string.Empty;
            PrivateName = string.Empty;
            Descriptor = string.Empty;
            Biography = string.Empty;
            PortraitInternalId = 1;
            SoundSetId = -1;
            IsRetired = false;
            DateRetired = null;
            DateLastActivated = null;
            ScrambleAccountId = true;
        }

        [Indexed]
        public string PlayerId { get; set; }
        [Indexed]
        public bool IsRetired { get; set; }
        public string PrivateName { get; set; }
        public string Descriptor { get; set; }
        public string Biography { get; set; }
        public int PortraitInternalId { get; set; }
        public int SoundSetId { get; set; }
        public bool ScrambleAccountId { get; set; }
        public DateTime? DateRetired { get; set; }
        public DateTime? DateLastActivated { get; set; }
    }
}
