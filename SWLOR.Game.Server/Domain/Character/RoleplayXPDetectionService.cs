using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>The model field that used to be static in <c>RoleplayXP</c>, moved here unchanged.</summary>
    public class RoleplayXPDetectionService : IRoleplayXPDetectionService
    {
        private OOCDetectionModel _model;

        public void SetModel(OOCDetectionModel model) => _model = model;

        public OOCDetectionModel GetModel() => _model ?? OOCDetectionModel.Default;
    }
}
