using NAudio.Wave;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Services
{
    /// <summary>
    /// Plays one audio resource at a time, so a builder choosing an ambient sound can hear it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The original toolset put Play and Stop beside the list of sounds an object will play, and
    /// picking an ambience by name alone is guesswork - <c>al_pl_bazarwalla</c> says nothing about
    /// what it sounds like. This is that button, reading out of the same resource index the picker
    /// lists from rather than off disk, because the sound a builder is choosing may live in a hak.
    /// </para>
    /// <para>
    /// One playback at a time, by design: a preview is for answering "is this the one", and two
    /// ambiences at once answers nothing. Starting a new one stops the old.
    /// </para>
    /// </remarks>
    public sealed class SoundPreviewService : IDisposable
    {
        /// <summary>The four bytes a RIFF WAV opens with.</summary>
        private static readonly byte[] RiffMagic = "RIFF"u8.ToArray();

        /// <summary>NWN's music wrapper: "BMU " and a version, then an MP3 stream.</summary>
        private static readonly byte[] BmuMagic = "BMU "u8.ToArray();

        /// <summary>Bytes of BMU header before the MP3 begins.</summary>
        private const int BmuHeaderLength = 8;

        private readonly ResourceIndex? _resources;
        private readonly object _syncRoot = new();

        private WaveOutEvent? _device;
        private WaveStream? _stream;
        private IDisposable? _source;
        private bool _disposed;

        /// <summary>Raised when playback ends, whether it finished or was stopped.</summary>
        public event Action? PlaybackStopped;

        public SoundPreviewService(ResourceIndex? resources)
        {
            _resources = resources;
        }

        /// <summary>Whether anything can be played at all, which decides if the buttons appear.</summary>
        public bool IsAvailable => _resources != null && OperatingSystem.IsWindows();

        public bool IsPlaying
        {
            get
            {
                lock (_syncRoot)
                    return _device?.PlaybackState == PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Plays an audio resref, replacing whatever was playing. Returns the reason it could not be
        /// played, or null when it started - the caller shows that beside the button rather than
        /// leaving a click look like nothing happened.
        /// </summary>
        public string? Play(string? resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            if (!IsAvailable)
                return "Sound preview is not available here.";

            if (!TryReadBytes(resRef, out var bytes))
                return $"{resRef} is not in the module's audio resources.";

            try
            {
                Stop();

                lock (_syncRoot)
                {
                    _stream = OpenStream(bytes);
                    if (_stream == null)
                        return $"{resRef} is not an audio format this can play.";

                    _device = new WaveOutEvent();
                    _device.PlaybackStopped += OnPlaybackStopped;
                    _device.Init(_stream);
                    _device.Play();
                }

                return null;
            }
            catch (Exception ex)
            {
                // A sound that will not decode is a bad resource, not a reason to take the editor
                // down - and the builder still needs to be told why they heard nothing.
                Stop();
                return $"Could not play {resRef}: {ex.Message}";
            }
        }

        public void Stop()
        {
            WaveOutEvent? device;
            WaveStream? stream;
            IDisposable? source;

            lock (_syncRoot)
            {
                device = _device;
                stream = _stream;
                source = _source;
                _device = null;
                _stream = null;
                _source = null;
            }

            if (device != null)
            {
                device.PlaybackStopped -= OnPlaybackStopped;
                try
                {
                    device.Stop();
                }
                catch (Exception)
                {
                    // Already gone; disposing is all that is left to do.
                }

                device.Dispose();
            }

            stream?.Dispose();
            source?.Dispose();

            if (device != null)
                PlaybackStopped?.Invoke();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e) => PlaybackStopped?.Invoke();

        private bool TryReadBytes(string resRef, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (_resources == null)
                return false;

            try
            {
                var identity = new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension("wav"));
                if (!_resources.TryLookup(identity, out var handle))
                    return false;

                bytes = handle.GetBytes();
                return bytes.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Wraps the bytes in whatever reader their header calls for, converting anything WaveOut
        /// cannot take - NWN's ambient sounds are frequently IMA ADPCM - into PCM on the way.
        /// </summary>
        private WaveStream? OpenStream(byte[] bytes)
        {
            if (StartsWith(bytes, RiffMagic))
            {
                var source = new MemoryStream(bytes);
                _source = source;
                var reader = new WaveFileReader(source);
                if (reader.WaveFormat.Encoding is WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat)
                    return reader;

                return new WaveFormatConversionStream(
                    new WaveFormat(reader.WaveFormat.SampleRate, 16, reader.WaveFormat.Channels),
                    reader);
            }

            if (StartsWith(bytes, BmuMagic))
            {
                var source = new MemoryStream(bytes, BmuHeaderLength, bytes.Length - BmuHeaderLength);
                _source = source;
                return new Mp3FileReader(source);
            }

            return null;
        }

        private static bool StartsWith(byte[] bytes, byte[] magic)
        {
            if (bytes.Length < magic.Length)
                return false;

            for (var i = 0; i < magic.Length; i++)
            {
                if (bytes[i] != magic[i])
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Stop();
        }
    }
}
