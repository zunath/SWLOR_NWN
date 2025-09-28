namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for time
        private int _currentHour = 12;
        private int _currentMinute = 0;
        private int _currentSecond = 0;
        private int _currentMillisecond = 0;

        public bool GetIsDay() => _currentHour >= 6 && _currentHour < 18;

        public bool GetIsNight() => _currentHour < 6 || _currentHour >= 18;

        public bool GetIsDawn() => _currentHour >= 5 && _currentHour < 7;

        public bool GetIsDusk() => _currentHour >= 17 && _currentHour < 19;

        public float RoundsToSeconds(int nRounds) => nRounds * 6.0f;

        public float HoursToSeconds(int nHours) => nHours * 3600.0f;

        public float TurnsToSeconds(int nTurns) => nTurns * 60.0f;

        public int GetTimeHour() => _currentHour;

        public int GetTimeMinute() => _currentMinute;

        public int GetTimeSecond() => _currentSecond;

        public int GetTimeMillisecond() => _currentMillisecond;

        // Helper methods for testing
        public void SetTime(int nHour, int nMinute = 0, int nSecond = 0, int nMillisecond = 0) 
        {
            _currentHour = Math.Max(0, Math.Min(23, nHour));
            _currentMinute = Math.Max(0, Math.Min(59, nMinute));
            _currentSecond = Math.Max(0, Math.Min(59, nSecond));
            _currentMillisecond = Math.Max(0, Math.Min(999, nMillisecond));
        }


    }
}
