using SWLOR.Toolset.Shell.Views;

namespace SWLOR.Toolset.Services
{
    public enum ExternalChangeChoice
    {
        Cancel,
        Reload,
        Overwrite
    }

    public enum UnsavedChangesChoice
    {
        Cancel,
        Save,
        Discard
    }

    /// <summary>Prompts for editor decisions that must be explicit before data can be replaced.</summary>
    public interface IEditorPromptService
    {
        Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath);

        Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle);
    }

    public sealed class EditorPromptService : IEditorPromptService
    {
        public async Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var choice = await EditorChoiceDialog.ShowAsync(
                $"'{fileName}' changed outside SWLOR Toolset",
                "Reload the file to keep the external version and discard this tab's edits, or overwrite it with the version currently open in the editor.",
                "Reload",
                "Overwrite").ConfigureAwait(true);

            return choice switch
            {
                EditorDialogChoice.Primary => ExternalChangeChoice.Reload,
                EditorDialogChoice.Secondary => ExternalChangeChoice.Overwrite,
                _ => ExternalChangeChoice.Cancel
            };
        }

        public async Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle)
        {
            var choice = await EditorChoiceDialog.ShowAsync(
                $"Save changes to '{documentTitle.TrimEnd(' ', '*')}'?",
                "This editor has unsaved changes. Save them before closing, discard them explicitly, or cancel and keep the tab open.",
                "Save",
                "Discard").ConfigureAwait(true);

            return choice switch
            {
                EditorDialogChoice.Primary => UnsavedChangesChoice.Save,
                EditorDialogChoice.Secondary => UnsavedChangesChoice.Discard,
                _ => UnsavedChangesChoice.Cancel
            };
        }
    }
}
