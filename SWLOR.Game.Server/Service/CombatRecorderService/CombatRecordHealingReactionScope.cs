namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Holds the events of reactions to a heal that is about to be applied, so they are written
    /// after that heal. Disposing ends the reaction; the default value is inactive.
    /// </summary>
    public readonly struct CombatRecordHealingReactionScope : IDisposable
    {
        private readonly bool _isActive;

        internal CombatRecordHealingReactionScope(bool isActive)
        {
            _isActive = isActive;
        }

        public void Dispose()
        {
            if (_isActive)
                CombatRecorder.EndHealingReaction();
        }
    }
}
