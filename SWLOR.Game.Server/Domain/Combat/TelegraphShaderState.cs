using System.Numerics;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The telegraph shader uniforms last sent to one player, so unchanged slots are not resent.
    /// A null entry means the client's value is not known and must be sent.
    /// </summary>
    public sealed class TelegraphShaderState
    {
        public int?[] PackedData { get; } = new int?[TelegraphService.MaxRenderCount];
        public Vector4?[] Positions { get; } = new Vector4?[TelegraphService.MaxRenderCount];

        /// <summary>
        /// The fill clock time until which this viewer has a shape still filling. Until then the fill
        /// clock uniform keeps being sent to them.
        /// </summary>
        public long FillClockActiveUntil { get; set; }
    }
}
