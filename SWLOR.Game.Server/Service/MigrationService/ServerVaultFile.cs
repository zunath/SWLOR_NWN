using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// File access for the server vault that reaches every character file. The game names character files in
/// Windows-1252, and on Linux those names are stored as raw bytes that are not valid UTF-8. .NET decodes Linux file
/// names as UTF-8 and replaces invalid bytes, so it lists such a file under a name it then cannot open. On Linux this
/// lists and opens files through libc with the exact bytes: a name that is valid UTF-8 keeps the string .NET gives it,
/// and a name that is not keeps its ASCII characters and stores every other byte as the lone surrogate U+DC00 plus
/// the byte, which turns back into the same bytes. Other platforms use .NET directly.
/// </summary>
internal static class ServerVaultFile
{
    private const char EscapeBase = '\uDC00';
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>The string for a raw file name: the UTF-8 text when valid, otherwise ASCII with every other byte escaped.</summary>
    internal static string DecodeName(byte[] name)
    {
        try
        {
            return StrictUtf8.GetString(name);
        }
        catch (DecoderFallbackException)
        {
            var builder = new StringBuilder(name.Length);
            foreach (var b in name)
                builder.Append(b < 0x80 ? (char)b : (char)(EscapeBase + b));
            return builder.ToString();
        }
    }

    /// <summary>
    /// The raw bytes of a path built from <see cref="DecodeName"/> strings: escaped bytes as themselves, all other text
    /// as UTF-8. Text never holds a low surrogate without its high one, so an escaped byte cannot be mistaken for text.
    /// </summary>
    internal static byte[] EncodePath(string path)
    {
        var bytes = new List<byte>(path.Length);
        for (var index = 0; index < path.Length; index++)
        {
            if (IsEscapedByte(path, index))
            {
                bytes.Add((byte)(path[index] - EscapeBase));
                continue;
            }
            var length = char.IsHighSurrogate(path[index]) && index + 1 < path.Length && char.IsLowSurrogate(path[index + 1]) ? 2 : 1;
            bytes.AddRange(Encoding.UTF8.GetBytes(path.Substring(index, length)));
            index += length - 1;
        }
        return bytes.ToArray();
    }

    /// <summary>A path for logs: escaped bytes are shown as Latin-1 letters, which match the game's Windows-1252 accents.</summary>
    internal static string DisplayPath(string path)
    {
        var builder = new StringBuilder(path.Length);
        for (var index = 0; index < path.Length; index++)
            builder.Append(IsEscapedByte(path, index) ? (char)(path[index] - EscapeBase) : path[index]);
        return builder.ToString();
    }

    private static bool IsEscapedByte(string path, int index) =>
        path[index] >= EscapeBase + 0x80 && path[index] <= EscapeBase + 0xFF &&
        (index == 0 || !char.IsHighSurrogate(path[index - 1]));

    private static bool UseLibc => OperatingSystem.IsLinux();

    /// <summary>Every file below the directory whose name ends in the suffix, as Directory.EnumerateFiles finds them.</summary>
    internal static IEnumerable<string> EnumerateFiles(string directory, string suffix)
    {
        if (!UseLibc)
            return Directory.EnumerateFiles(directory, "*" + suffix, SearchOption.AllDirectories);
        var files = new List<string>();
        var ending = Encoding.UTF8.GetBytes(suffix);
        CollectFiles(directory, ending, files);
        return files;
    }

    private static void CollectFiles(string directory, byte[] suffix, List<string> files)
    {
        var subdirectories = new List<string>();
        var handle = Libc.opendir(NullTerminated(directory));
        if (handle == IntPtr.Zero)
            throw Error("list", directory);
        try
        {
            while (true)
            {
                Marshal.SetLastPInvokeError(0);
                var entry = Libc.readdir(handle);
                if (entry == IntPtr.Zero)
                {
                    var errno = Marshal.GetLastPInvokeError();
                    if (errno != 0)
                        throw Error("list", directory, errno);
                    break;
                }
                var name = EntryName(entry);
                if (IsDotEntry(name))
                    continue;
                var type = Marshal.ReadByte(entry, Libc.DirentTypeOffset);
                var path = Path.Combine(directory, DecodeName(name));
                if (type == Libc.DT_DIR || (type is Libc.DT_LNK or Libc.DT_UNKNOWN && IsDirectory(path)))
                    subdirectories.Add(path);
                else if (EndsWith(name, suffix))
                    files.Add(path);
            }
        }
        finally
        {
            Libc.closedir(handle);
        }
        foreach (var subdirectory in subdirectories)
            CollectFiles(subdirectory, suffix, files);
    }

    private static byte[] EntryName(IntPtr entry)
    {
        var length = 0;
        while (Marshal.ReadByte(entry, Libc.DirentNameOffset + length) != 0)
            length++;
        var name = new byte[length];
        Marshal.Copy(entry + Libc.DirentNameOffset, name, 0, length);
        return name;
    }

    private static bool IsDotEntry(byte[] name) =>
        (name.Length == 1 && name[0] == '.') || (name.Length == 2 && name[0] == '.' && name[1] == '.');

    private static bool EndsWith(byte[] name, byte[] suffix)
    {
        if (name.Length < suffix.Length)
            return false;
        for (var index = 0; index < suffix.Length; index++)
            if (name[name.Length - suffix.Length + index] != suffix[index])
                return false;
        return true;
    }

    private static bool IsDirectory(string path)
    {
        var handle = Libc.opendir(NullTerminated(path));
        if (handle == IntPtr.Zero)
            return false;
        Libc.closedir(handle);
        return true;
    }

    internal static bool Exists(string path)
    {
        if (!UseLibc)
            return File.Exists(path);
        var fd = Libc.open(NullTerminated(path), Libc.O_RDONLY | Libc.O_CLOEXEC, 0);
        if (fd < 0)
            return false;
        Libc.close(fd);
        return !IsDirectory(path);
    }

    internal static byte[] ReadAllBytes(string path)
    {
        if (!UseLibc)
            return File.ReadAllBytes(path);
        using var stream = Open(path, Libc.O_RDONLY, FileAccess.Read);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    internal static void WriteAllBytes(string path, byte[] data)
    {
        if (!UseLibc)
        {
            File.WriteAllBytes(path, data);
            return;
        }
        using var stream = Open(path, Libc.O_WRONLY | Libc.O_CREAT | Libc.O_TRUNC, FileAccess.Write);
        stream.Write(data);
    }

    /// <summary>Copies a file to a path that must not exist yet, as File.Copy without overwrite.</summary>
    internal static void Copy(string source, string destination)
    {
        if (!UseLibc)
        {
            File.Copy(source, destination);
            return;
        }
        var data = ReadAllBytes(source);
        using var stream = Open(destination, Libc.O_WRONLY | Libc.O_CREAT | Libc.O_EXCL, FileAccess.Write);
        stream.Write(data);
    }

    /// <summary>Moves a file over any file at the destination, as File.Move with overwrite.</summary>
    internal static void Move(string source, string destination)
    {
        if (!UseLibc)
        {
            File.Move(source, destination, true);
            return;
        }
        if (Libc.rename(NullTerminated(source), NullTerminated(destination)) != 0)
            throw Error("move", source);
    }

    /// <summary>Deletes a file; a missing file is not an error, as File.Delete.</summary>
    internal static void Delete(string path)
    {
        if (!UseLibc)
        {
            File.Delete(path);
            return;
        }
        if (Libc.unlink(NullTerminated(path)) != 0 && Marshal.GetLastPInvokeError() != Libc.ENOENT)
            throw Error("delete", path);
    }

    /// <summary>Creates a directory and its parents, as Directory.CreateDirectory.</summary>
    internal static void CreateDirectory(string path)
    {
        if (!UseLibc)
        {
            Directory.CreateDirectory(path);
            return;
        }
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(path));
        if (!string.IsNullOrEmpty(parent) && parent != path && !IsDirectory(parent))
            CreateDirectory(parent);
        if (Libc.mkdir(NullTerminated(path), Libc.DirectoryMode) != 0 && (Marshal.GetLastPInvokeError() != Libc.EEXIST || !IsDirectory(path)))
            throw Error("create directory", path);
    }

    private static FileStream Open(string path, int flags, FileAccess access)
    {
        var fd = Libc.open(NullTerminated(path), flags | Libc.O_CLOEXEC, Libc.FileMode);
        if (fd < 0)
            throw Error("open", path);
        return new FileStream(new SafeFileHandle(fd, true), access);
    }

    private static byte[] NullTerminated(string path)
    {
        var bytes = EncodePath(path);
        Array.Resize(ref bytes, bytes.Length + 1);
        return bytes;
    }

    private static IOException Error(string action, string path, int? errno = null)
    {
        var code = errno ?? Marshal.GetLastPInvokeError();
        var message = $"Could not {action} '{DisplayPath(path)}': {Marshal.GetPInvokeErrorMessage(code)}";
        return code == Libc.ENOENT ? new FileNotFoundException(message, DisplayPath(path)) : new IOException(message);
    }

    /// <summary>The libc calls and Linux x86-64 constants the vault needs.</summary>
    private static class Libc
    {
        public const int O_RDONLY = 0x0;
        public const int O_WRONLY = 0x1;
        public const int O_CREAT = 0x40;
        public const int O_EXCL = 0x80;
        public const int O_TRUNC = 0x200;
        public const int O_CLOEXEC = 0x80000;
        public const int ENOENT = 2;
        public const int EEXIST = 17;
        public const byte DT_UNKNOWN = 0;
        public const byte DT_DIR = 4;
        public const byte DT_LNK = 10;
        public const int FileMode = 0x1B6; // 0666, before the umask, as .NET creates files.
        public const int DirectoryMode = 0x1FF; // 0777, before the umask, as .NET creates directories.

        // struct dirent on Linux x86-64 (glibc and musl): d_ino 8 bytes, d_off 8, d_reclen 2, d_type 1, then d_name.
        public const int DirentTypeOffset = 18;
        public const int DirentNameOffset = 19;

        [DllImport("libc", SetLastError = true)]
        public static extern IntPtr opendir(byte[] name);

        [DllImport("libc", SetLastError = true)]
        public static extern IntPtr readdir(IntPtr dir);

        [DllImport("libc", SetLastError = true)]
        public static extern int closedir(IntPtr dir);

        [DllImport("libc", SetLastError = true)]
        public static extern int open(byte[] path, int flags, int mode);

        [DllImport("libc", SetLastError = true)]
        public static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        public static extern int rename(byte[] oldPath, byte[] newPath);

        [DllImport("libc", SetLastError = true)]
        public static extern int unlink(byte[] path);

        [DllImport("libc", SetLastError = true)]
        public static extern int mkdir(byte[] path, int mode);
    }
}
