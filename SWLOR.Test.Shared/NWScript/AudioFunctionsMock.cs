using System.Collections.Generic;
using System.Numerics;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for audio
        private readonly Dictionary<int, float> _soundDurations = new();
        private readonly List<SoundPlayRecord> _soundHistory = new();
        private readonly Dictionary<uint, Dictionary<int, AudioStreamData>> _audioStreams = new();

        public class SoundPlayRecord
        {
            public int StrRef { get; set; }
            public string SoundName { get; set; } = "";
            public bool RunAsAction { get; set; }
        }

        public class AudioStreamData
        {
            public bool IsPaused { get; set; } = false;
            public float Volume { get; set; } = 1.0f;
            public float Position { get; set; } = 0.0f;
        }

        public float GetStrRefSoundDuration(int nStrRef) => 
            _soundDurations.GetValueOrDefault(nStrRef, 0.0f);

        public float GetDialogSoundLength(int nStrRef) => 
            _soundDurations.GetValueOrDefault(nStrRef, 0.0f);

        public void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true) 
        {
            _soundHistory.Add(new SoundPlayRecord 
            { 
                StrRef = nStrRef, 
                RunAsAction = nRunAsAction 
            });
        }

        public void PlaySound(string sSoundName) 
        {
            _soundHistory.Add(new SoundPlayRecord 
            { 
                SoundName = sSoundName, 
                RunAsAction = true 
            });
        }

        public void SetAudioStreamPaused(uint oPlayer, int nStreamIdentifier, bool bPaused, float fFadeTime = 0.0f) 
        {
            if (!_audioStreams.ContainsKey(oPlayer))
                _audioStreams[oPlayer] = new Dictionary<int, AudioStreamData>();
            if (!_audioStreams[oPlayer].ContainsKey(nStreamIdentifier))
                _audioStreams[oPlayer][nStreamIdentifier] = new AudioStreamData();
            
            _audioStreams[oPlayer][nStreamIdentifier].IsPaused = bPaused;
        }

        public void SetAudioStreamVolume(uint oPlayer, int nStreamIdentifier, float fVolume = 1.0f, float fFadeTime = 0.0f) 
        {
            if (!_audioStreams.ContainsKey(oPlayer))
                _audioStreams[oPlayer] = new Dictionary<int, AudioStreamData>();
            if (!_audioStreams[oPlayer].ContainsKey(nStreamIdentifier))
                _audioStreams[oPlayer][nStreamIdentifier] = new AudioStreamData();
            
            _audioStreams[oPlayer][nStreamIdentifier].Volume = fVolume;
        }

        public void SeekAudioStream(uint oPlayer, int nStreamIdentifier, float fSeconds) 
        {
            if (!_audioStreams.ContainsKey(oPlayer))
                _audioStreams[oPlayer] = new Dictionary<int, AudioStreamData>();
            if (!_audioStreams[oPlayer].ContainsKey(nStreamIdentifier))
                _audioStreams[oPlayer][nStreamIdentifier] = new AudioStreamData();
            
            _audioStreams[oPlayer][nStreamIdentifier].Position = fSeconds;
        }

        // Helper methods for testing
        public void SetSoundDuration(int nStrRef, float duration) => _soundDurations[nStrRef] = duration;
        public List<SoundPlayRecord> GetSoundHistory() => _soundHistory;
        public void ClearSoundHistory() => _soundHistory.Clear();
        public Dictionary<int, AudioStreamData> GetAudioStreams(uint oPlayer) => _audioStreams.GetValueOrDefault(oPlayer, new Dictionary<int, AudioStreamData>());
    }
}
