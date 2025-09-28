using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for chat
        private readonly List<string> _dmMessages = new();
        private readonly Dictionary<uint, List<string>> _playerMessages = new();
        private readonly List<SpeakRecord> _speakHistory = new();
        private uint _pcChatSpeaker = OBJECT_INVALID;
        private string _pcChatMessage = "";
        private TalkVolumeType _pcChatVolume = TalkVolumeType.Talk;

        public class PlayerMessage
        {
            public uint Player { get; set; }
            public string Message { get; set; } = "";
            public int StrRef { get; set; }
            public bool IsStrRef { get; set; }
        }

        public class SpeakRecord
        {
            public string StringToSpeak { get; set; } = "";
            public int StrRef { get; set; }
            public TalkVolumeType Volume { get; set; } = TalkVolumeType.Talk;
            public bool IsStrRef { get; set; }
        }

        public uint GetPCChatSpeaker() => _pcChatSpeaker;
        
        public void SendMessageToAllDMs(string szMessage) => _dmMessages.Add(szMessage);
        
        public string GetPCChatMessage() => _pcChatMessage;
        
        public TalkVolumeType GetPCChatVolume() => _pcChatVolume;
        
        public void SetPCChatMessage(string sNewChatMessage = "") => _pcChatMessage = sNewChatMessage;
        
        public void SetPCChatVolume(TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _pcChatVolume = nTalkVolume;

        // Message methods
        public void SendMessageToPC(uint oPlayer, string szMessage) 
        {
            if (!_playerMessages.ContainsKey(oPlayer))
                _playerMessages[oPlayer] = new List<string>();
            _playerMessages[oPlayer].Add(szMessage);
        }

        // Helper methods for testing
        public List<string> GetDMMessages() => new List<string>(_dmMessages);
        public void ClearDMMessages() => _dmMessages.Clear();
        public void SetPCChatSpeaker(uint speaker) => _pcChatSpeaker = speaker;
        public Dictionary<uint, List<string>> GetPlayerMessages() => new Dictionary<uint, List<string>>(_playerMessages);
        public List<string> GetPlayerMessages(uint oPlayer) => _playerMessages.GetValueOrDefault(oPlayer, new List<string>());
        public void ClearPlayerMessages() => _playerMessages.Clear();
        public List<SpeakRecord> GetSpeakHistory() => new List<SpeakRecord>(_speakHistory);
        public void ClearSpeakHistory() => _speakHistory.Clear();
    }
}
