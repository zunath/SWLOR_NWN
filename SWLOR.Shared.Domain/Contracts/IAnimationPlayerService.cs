using SWLOR.Component.Character.Model;

namespace SWLOR.Component.Character.Contracts
{
    public interface IAnimationPlayerService
    {
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}