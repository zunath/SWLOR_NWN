using Nwn.Authoring.Editing;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Services
{
    /// <summary>Saves edited documents after checking that their accepted file generation is current.</summary>
    public sealed class SaveService
    {
        private readonly OutputLogService _log;

        public SaveService(OutputLogService log) => _log = log;

        /// <summary>Saves the session if dirty; returns true when the file is clean afterwards.</summary>
        public bool Save(DocumentSession session)
        {
            if (!session.UndoStack.IsDirty)
                return true;

            try
            {
                var saveBytes = session.ToBytes();
                if (!TryWriteAtomicIfUnchanged(session, saveBytes))
                {
                    _log.AppendLine($"Save refused for {session.FilePath}: the file changed outside the editor.");
                    return false;
                }

                session.UndoStack.MarkSaved();
                session.RecordCurrentFileState(saveBytes);
                _log.AppendLine($"Saved {session.FilePath}.");
                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {session.FilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>Replaces one session file only when it still matches the accepted generation.</summary>
        public static bool TryWriteAtomicIfUnchanged(DocumentSession session, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(bytes);

            SwlorFileWriteAccess.WriteAccess.EnsureAllowed();
            using var writeLease = ModuleWriteLock.AcquireForResourcePath(session.FilePath, TimeSpan.Zero);
            if (session.HasExternalChange())
                return false;

            SwlorFileWriteAccess.Writer.WriteAtomic(session.FilePath, bytes);
            return true;
        }

        /// <summary>Replaces a script source only when it still matches the accepted generation.</summary>
        public static bool TryWriteAtomicIfUnchanged(ScriptSession session, byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(bytes);

            SwlorFileWriteAccess.WriteAccess.EnsureAllowed();
            using var writeLease = ModuleWriteLock.AcquireForResourcePath(session.FilePath, TimeSpan.Zero);
            if (session.HasExternalChange())
                return false;

            SwlorFileWriteAccess.Writer.WriteAtomic(session.FilePath, bytes);
            return true;
        }
    }
}
