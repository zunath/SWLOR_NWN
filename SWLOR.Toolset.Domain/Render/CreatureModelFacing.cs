using System.Numerics;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Corrects NWN creature artwork from its authored forward axis to the area's shared instance
    /// heading convention.
    /// </summary>
    /// <remarks>
    /// Creature bodies face model <c>+Y</c>, while a GIT creature's
    /// <c>XOrientation</c>/<c>YOrientation</c> vector is its world-space facing. The area transform
    /// applies that heading as though model <c>+X</c> were forward, so an uncorrected creature is
    /// displayed a quarter turn anticlockwise from Aurora. This model-space turn maps the body's
    /// <c>+Y</c> front onto <c>+X</c> before the stored heading is applied.
    /// </remarks>
    public static class CreatureModelFacing
    {
        public static readonly Matrix4x4 ForwardCorrection = Matrix4x4.CreateRotationZ(-MathF.PI / 2f);
    }
}
