using SWLOR.Component.Player.Model;

namespace SWLOR.Component.Player.Contracts
{
    public interface IAnimationPlayerService
    {
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}