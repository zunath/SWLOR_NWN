using System.Numerics;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The telegraph shader uniforms last sent to one player, so unchanged slots are not resent.
    /// A null entry means the client's value is not known and must be sent.
    /// </summary>
    public sealed class TelegraphShaderState
    {
        public int?[] PackedData { get; } = new int?[Domain.Combat.TelegraphService.MaxRenderCount];
        public Vector3?[] Positions { get; } = new Vector3?[Domain.Combat.TelegraphService.MaxRenderCount];
    }
}
