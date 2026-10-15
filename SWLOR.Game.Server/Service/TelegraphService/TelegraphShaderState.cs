using System.Numerics;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    /// <summary>
    /// The telegraph shader uniforms last sent to one player, so unchanged slots are not resent.
    /// A null entry means the client's value is not known and must be sent.
    /// </summary>
    public sealed class TelegraphShaderState
    {
        public int?[] PackedData { get; } = new int?[Telegraph.MaxRenderCount];
        public Vector3?[] Positions { get; } = new Vector3?[Telegraph.MaxRenderCount];
    }
}
