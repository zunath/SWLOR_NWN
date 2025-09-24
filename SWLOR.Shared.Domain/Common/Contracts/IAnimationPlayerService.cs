using SWLOR.Shared.Domain.AI.ValueObjects;

namespace SWLOR.Shared.Domain.Common.Contracts
{
    public interface IAnimationPlayerService
    {
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}