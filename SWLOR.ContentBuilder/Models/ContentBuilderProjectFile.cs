using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// Root shape of a saved Content Builder project ("File -> Save/Save As/Open"). Deliberately flat
    /// and readable rather than clever: AreaSettings mirrors every user-editable control on the Areas
    /// tab (see MainWindow.CaptureState/ApplyState), and Batch reuses AreaBatchFileEntry -- the same
    /// shape SWLOR.ProcgenReview's "--areas-file" contract already uses (see AreaBatchFile.cs) --
    /// rather than inventing a second, parallel batch format.
    ///
    /// Version has NO default initializer on purpose: a saved file always stamps it explicitly
    /// (ProjectFileService.CurrentVersion), so a JSON file with the "version" property entirely
    /// missing deserializes to 0, which ProjectFileService.ValidateJson reports as a distinct
    /// "missing version" error rather than silently being accepted as version 1.
    /// </summary>
    public sealed class ContentBuilderProjectFile
    {
        public int Version { get; set; }
        public AreaSettingsFile AreaSettings { get; set; }
        public List<AreaBatchFileEntry> Batch { get; set; } = new();
    }
}
