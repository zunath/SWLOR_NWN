using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service;

public interface IAnimationPlayerService
{
    void Play(uint oObject, AnimationEvent animationEvent);
}