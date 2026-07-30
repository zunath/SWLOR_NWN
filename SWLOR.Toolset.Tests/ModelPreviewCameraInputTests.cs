using System.Numerics;
using System.Reflection;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The model-preview camera exposes pixel-distance operations instead of reusing the
    /// time-integrated camera-pad actions.
    /// </summary>
    public sealed class ModelPreviewCameraInputTests
    {
        [AvaloniaTest]
        public void PanPreviewByPixels_MovesInScreenSpace()
        {
            var control = PreviewControl();
            var before = Read<Vector3>(control, "_target");

            control.PanPreviewByPixels(dxPixels: 20f, dyPixels: 30f);

            var after = Read<Vector3>(control, "_target");
            after.X.Should().BeLessThan(before.X, "a rightward grab-drag moves the camera left");
            after.Z.Should().BeGreaterThan(before.Z, "a downward grab-drag moves the camera up");
            after.Y.Should().BeApproximately(before.Y, 0.0001f, "the front preview's vertical drag is not depth motion");
        }

        [AvaloniaTest]
        public void OrbitPreviewByPixels_DoubleDistanceProducesDoubleTurn()
        {
            var shortDrag = PreviewControl();
            var longDrag = PreviewControl();
            var start = Read<float>(shortDrag, "_azimuth");

            shortDrag.OrbitPreviewByPixels(dxPixels: 40f, dyPixels: 0f);
            longDrag.OrbitPreviewByPixels(dxPixels: 80f, dyPixels: 0f);

            var shortTurn = start - Read<float>(shortDrag, "_azimuth");
            var longTurn = start - Read<float>(longDrag, "_azimuth");
            longTurn.Should().BeApproximately(shortTurn * 2f, 0.0001f);
        }

        private static GlAreaControl PreviewControl()
        {
            var control = new GlAreaControl();
            Write(control, "_target", Vector3.Zero);
            Write(control, "_azimuth", MathF.PI * 1.5f);
            Write(control, "_elevation", 0f);
            Write(control, "_distance", 5f);
            Write(control, "_viewportHeight", 300);
            return control;
        }

        private static T Read<T>(GlAreaControl control, string fieldName) =>
            (T)Field(fieldName).GetValue(control)!;

        private static void Write<T>(GlAreaControl control, string fieldName, T value) =>
            Field(fieldName).SetValue(control, value);

        private static FieldInfo Field(string fieldName) =>
            typeof(GlAreaControl).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
}
