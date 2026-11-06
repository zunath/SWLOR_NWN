using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.RoleplayXPService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The model the roleplay XP chat check loaded at server start. It lives in
    /// <see cref="IRoleplayXPDetectionService"/>; every member forwards to the container's instance.
    /// </summary>
    public static class RoleplayXPDetection
    {
        private static IRoleplayXPDetectionService Instance => FacadeAccessor.Resolve<IRoleplayXPDetectionService>();

        public static void SetModel(OOCDetectionModel model)
        {
            Instance.SetModel(model);
        }

        public static OOCDetectionModel GetModel()
        {
            return Instance.GetModel();
        }
    }
}
