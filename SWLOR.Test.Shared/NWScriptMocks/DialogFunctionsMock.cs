using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for dialog
        private readonly Dictionary<uint, DialogData> _dialogData = new();
        private readonly List<SpeakStringRecord> _dialogSpeakHistory = new();
        private uint _lastSpeaker = OBJECT_INVALID;

        private class DialogData
        {
            public bool IsInConversation { get; set; } = false;
            public string DialogResRef { get; set; } = "";
            public uint Target { get; set; } = OBJECT_INVALID;
            public bool IsPrivate { get; set; } = true;
            public bool PlayHello { get; set; } = true;
            public bool IsPaused { get; set; } = false;
        }

        private class SpeakStringRecord
        {
            public string StringToSpeak { get; set; } = "";
            public TalkVolumeType TalkVolume { get; set; } = TalkVolumeType.Talk;
        }

        public bool IsInConversation(uint oObject) => 
            _dialogData.GetValueOrDefault(oObject, new DialogData()).IsInConversation;

        public void ActionSpeakString(string sStringToSpeak, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) 
        {
            _dialogSpeakHistory.Add(new SpeakStringRecord 
            { 
                StringToSpeak = sStringToSpeak, 
                TalkVolume = nTalkVolume 
            });
        }

        public uint GetLastSpeaker() => _lastSpeaker;

        public int BeginConversation(string sResRef = "", uint oObjectToDialog = OBJECT_INVALID) 
        {
            var data = GetOrCreateDialogData(OBJECT_SELF);
            data.IsInConversation = true;
            data.DialogResRef = sResRef;
            data.Target = oObjectToDialog;
            data.IsPaused = false;
            
            _lastSpeaker = oObjectToDialog;
            return 1; // Success
        }

        private DialogData GetOrCreateDialogData(uint oObject)
        {
            if (!_dialogData.ContainsKey(oObject))
                _dialogData[oObject] = new DialogData();
            return _dialogData[oObject];
        }

        // Helper methods for testing




    }
}
