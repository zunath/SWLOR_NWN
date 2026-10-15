namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Pops a recorder attribution when disposed. The default value is inactive, which is what
    /// every Begin* call returns while no recording is running.
    /// </summary>
    public readonly struct CombatRecordAttributionScope : IDisposable
    {
        private readonly int _restoreDepthPlusOne;

        internal CombatRecordAttributionScope(int restoreDepth)
        {
            _restoreDepthPlusOne = restoreDepth + 1;
        }

        public void Dispose()
        {
            if (_restoreDepthPlusOne > 0)
                CombatRecorder.RestoreAttributionDepth(_restoreDepthPlusOne - 1);
        }
    }
}
