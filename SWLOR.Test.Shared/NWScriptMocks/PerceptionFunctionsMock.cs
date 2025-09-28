namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for perception
        private uint _lastPerceived = OBJECT_INVALID;
        private bool _lastPerceptionHeard = false;
        private bool _lastPerceptionInaudible = false;
        private bool _lastPerceptionSeen = false;
        private bool _lastPerceptionVanished = false;

        public uint GetLastPerceived() => _lastPerceived;

        public bool GetLastPerceptionHeard() => _lastPerceptionHeard;

        public bool GetLastPerceptionInaudible() => _lastPerceptionInaudible;

        public bool GetLastPerceptionSeen() => _lastPerceptionSeen;

        public bool GetLastPerceptionVanished() => _lastPerceptionVanished;

        // Helper methods for testing
    }
}
