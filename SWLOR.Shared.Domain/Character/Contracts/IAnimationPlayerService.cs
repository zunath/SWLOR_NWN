using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IAnimationPlayerService
    {
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}