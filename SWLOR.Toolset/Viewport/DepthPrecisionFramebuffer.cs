using System.Numerics;
using Silk.NET.OpenGL;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// An offscreen colour+depth target with a 24-bit depth buffer, rendered into instead of the
    /// framebuffer Avalonia hands the control, and blitted back over it at the end of the frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists for one reason: <b>Avalonia's framebuffer has a 16-bit depth buffer</b>, and that is
    /// not enough to keep NWN's wall decals from z-fighting. Avalonia allocates the depth attachment
    /// itself in <c>OpenGlControlBase</c> and offers no way to ask for a deeper one -
    /// <c>Avalonia.OpenGL.dll</c> contains exactly one depth format in its IL, <c>DEPTH_COMPONENT16</c>,
    /// and no 24- or 32-bit constant anywhere in the assembly. The NWN toolset draws to a real window
    /// with the ordinary 24-bit depth buffer, which is why the same geometry is steady there and
    /// flickers here.
    /// </para>
    /// <para>
    /// The arithmetic, for the camera in <see cref="GlAreaControl"/> (near = distance/20,
    /// far = distance*25 + 100, so a fixed near:far ratio of roughly 1:500): depth resolution in world
    /// units is <c>z^2 * (far - near) / (near * far * (2^bits - 1))</c>. Viewing a wall from 40m that is
    /// about <b>12mm</b> at 16 bits - coarser than the gap between a painting and the wall it hangs on,
    /// so the two surfaces land in the same depth bucket and which one wins changes with sub-pixel
    /// camera movement. That is the flicker. At 24 bits the same figure is about <b>0.05mm</b>, a 256x
    /// margin, because precision scales with 2^bits. Note it also scales with z^2, so the problem gets
    /// worse the further out the camera is pulled, which is why it reads as a panning/zooming artefact.
    /// </para>
    /// <para>
    /// Depth is a renderbuffer rather than a texture because nothing samples it, and colour likewise -
    /// the frame's only consumer is the blit. Both are reallocated only when the viewport size changes.
    /// If the target cannot be created (an FBO-incomplete driver), <see cref="BeginFrame"/> returns
    /// false once and stays false, and the caller draws straight to Avalonia's framebuffer as before:
    /// a flickering view is worse than a steady one but far better than a black one.
    /// </para>
    /// </remarks>
    public sealed class DepthPrecisionFramebuffer
    {
        private uint _framebuffer;
        private uint _colour;
        private uint _depth;
        private uint _width;
        private uint _height;

        /// <summary>
        /// Latched after a failed allocation so a driver that cannot honour the request is asked once
        /// per context rather than once per frame.
        /// </summary>
        private bool _unavailable;

        /// <summary>True when the offscreen target is allocated and being rendered into.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Binds the offscreen target for this frame, allocating or resizing it as needed. Returns false
        /// when the caller should just render to Avalonia's framebuffer instead.
        /// </summary>
        public bool BeginFrame(GL gl, uint width, uint height)
        {
            ArgumentNullException.ThrowIfNull(gl);
            IsActive = false;

            if (_unavailable || width == 0 || height == 0)
                return false;

            try
            {
                if (!EnsureAllocated(gl, width, height))
                    return false;

                gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
                IsActive = true;
                return true;
            }
            catch (Exception)
            {
                // A driver that throws on any of this is one we cannot use the offscreen path on.
                Release(gl);
                _unavailable = true;
                return false;
            }
        }

        /// <summary>
        /// Copies the frame over Avalonia's framebuffer and rebinds it, so the control leaves the
        /// context in the state Avalonia expects. Safe to call when <see cref="BeginFrame"/> failed.
        /// </summary>
        public void EndFrame(GL gl, int targetFramebuffer, Vector3 background)
        {
            ArgumentNullException.ThrowIfNull(gl);

            if (!IsActive)
                return;

            IsActive = false;

            try
            {
                var target = (uint)Math.Max(0, targetFramebuffer);
                gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
                gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target);

                // Clear the target first, because it is not necessarily the same size as this one.
                // Drawing straight to Avalonia's framebuffer used to clear all of it - glClear ignores
                // the viewport - so any part of it wider or taller than the size computed from Bounds x
                // RenderScaling still ended up as background. The blit only covers the region it is
                // given, so without this those edge pixels keep whatever was in them: measured as a
                // one-pixel seam of stale content along the top of the viewport.
                gl.BindFramebuffer(FramebufferTarget.Framebuffer, target);
                gl.ClearColor(background.X, background.Y, background.Z, 1f);
                gl.Clear((uint)ClearBufferMask.ColorBufferBit);
                gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebuffer);
                gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, target);

                // Same size both sides, so Nearest is an exact copy rather than a resample.
                gl.BlitFramebuffer(
                    0, 0, (int)_width, (int)_height,
                    0, 0, (int)_width, (int)_height,
                    (uint)ClearBufferMask.ColorBufferBit,
                    BlitFramebufferFilter.Nearest);

                gl.BindFramebuffer(FramebufferTarget.Framebuffer, target);
            }
            catch (Exception)
            {
                // Losing the blit costs this one frame; the next BeginFrame decides what to do next.
                Release(gl);
                _unavailable = true;

                try
                {
                    gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)Math.Max(0, targetFramebuffer));
                }
                catch (Exception)
                {
                    // Nothing further to try - the context is going down.
                }
            }
        }

        /// <summary>Releases the target. Call from the control's GL teardown, on the render thread.</summary>
        public void Dispose(GL gl)
        {
            if (gl == null)
                return;

            try
            {
                Release(gl);
            }
            catch (Exception)
            {
                // Teardown must never throw; the context may already be partly invalid.
            }
        }

        private bool EnsureAllocated(GL gl, uint width, uint height)
        {
            if (_framebuffer != 0 && _width == width && _height == height)
                return true;

            Release(gl);

            _framebuffer = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);

            _colour = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _colour);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Rgba8, width, height);
            gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, _colour);

            // The entire point of this class. GL 3.3 core and ES 3.0 - the floor the viewport's
            // shaders already require - both guarantee DEPTH_COMPONENT24 as a renderbuffer format.
            _depth = gl.GenRenderbuffer();
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depth);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
            gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depth);

            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                Release(gl);
                _unavailable = true;
                return false;
            }

            _width = width;
            _height = height;
            return true;
        }

        private void Release(GL gl)
        {
            if (_framebuffer != 0)
            {
                gl.DeleteFramebuffer(_framebuffer);
                _framebuffer = 0;
            }

            if (_colour != 0)
            {
                gl.DeleteRenderbuffer(_colour);
                _colour = 0;
            }

            if (_depth != 0)
            {
                gl.DeleteRenderbuffer(_depth);
                _depth = 0;
            }

            _width = 0;
            _height = 0;
            IsActive = false;
        }
    }
}
