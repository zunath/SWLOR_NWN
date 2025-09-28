namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for party management
        private readonly Dictionary<uint, List<uint>> _partyMembers = new(); // Leader -> List of members
        private readonly Dictionary<uint, uint> _partyLeaders = new(); // Member -> Leader
        private readonly Dictionary<uint, Dictionary<int, bool>> _panelButtonFlash = new(); // Player -> Button -> Flash state

        public void AddToParty(uint oPC, uint oPartyLeader) 
        {
            if (!_partyMembers.ContainsKey(oPartyLeader))
                _partyMembers[oPartyLeader] = new List<uint>();
            
            if (!_partyMembers[oPartyLeader].Contains(oPC))
                _partyMembers[oPartyLeader].Add(oPC);
            
            _partyLeaders[oPC] = oPartyLeader;
        }

        public void RemoveFromParty(uint oPC) 
        {
            if (_partyLeaders.ContainsKey(oPC))
            {
                var leader = _partyLeaders[oPC];
                if (_partyMembers.ContainsKey(leader))
                    _partyMembers[leader].Remove(oPC);
                _partyLeaders.Remove(oPC);
            }
        }

        public void SetPanelButtonFlash(uint oPlayer, int nButton, int nEnableFlash) 
        {
            if (!_panelButtonFlash.ContainsKey(oPlayer))
                _panelButtonFlash[oPlayer] = new Dictionary<int, bool>();
            _panelButtonFlash[oPlayer][nButton] = nEnableFlash != 0;
        }

        // Helper methods for testing
    }
}
