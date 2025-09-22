using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IAnimationPlayerService
    {
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}