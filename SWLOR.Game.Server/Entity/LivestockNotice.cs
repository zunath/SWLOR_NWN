namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A message for a character about their animals that they were not online to read: one wandered off, or predators
    /// raided the corrals. Shown at the character's next login, then deleted. A new type, so no migration.
    /// </summary>
    public class LivestockNotice : EntityBase
    {
        public LivestockNotice()
        {
            PlayerId = string.Empty;
            Text = string.Empty;
        }

        [Indexed]
        public string PlayerId { get; set; }

        public string Text { get; set; }
    }
}
