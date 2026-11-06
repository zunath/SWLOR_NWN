using SWLOR.Game.Server.Service.RoleplayXPService;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Holds the model the roleplay XP chat check loaded at server start. The check itself stays in the
    /// <c>RoleplayXP</c> Feature class.
    /// </summary>
    public interface IRoleplayXPDetectionService
    {
        /// <summary>Replaces the loaded model; null when none was loaded.</summary>
        void SetModel(OOCDetectionModel model);

        /// <summary>The loaded model, or the built-in default when none was loaded.</summary>
        OOCDetectionModel GetModel();
    }
}
