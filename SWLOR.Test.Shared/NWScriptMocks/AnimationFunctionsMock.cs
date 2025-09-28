using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for animations
        private readonly List<AnimationRecord> _animationHistory = new();

        private class AnimationRecord
        {
            public AnimationType Animation { get; set; }
            public float Speed { get; set; }
            public float DurationSeconds { get; set; }
        }

        public void PlayAnimation(AnimationType nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f)
        {
            _animationHistory.Add(new AnimationRecord
            {
                Animation = nAnimation,
                Speed = fSpeed,
                DurationSeconds = fSeconds
            });
        }

        // Helper methods for testing
    }
}
