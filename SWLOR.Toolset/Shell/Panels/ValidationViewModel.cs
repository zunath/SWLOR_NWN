using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Validation panel: a "Run Validation" command that runs every <see cref="ModuleValidator"/>
    /// convention rule over the currently open workspace on a background thread, then shows the
    /// aggregated issues (severity/rule/message/file) in a virtualized list with a summary status
    /// line (counts and duration). Mirrors <see cref="Shell.ShellViewModel"/>'s pattern of awaiting
    /// background work with <c>ConfigureAwait(true)</c> so continuations resume on the UI thread
    /// without needing an explicit Dispatcher hop.
    /// </summary>
    public partial class ValidationViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Domain.GameData.Resources.ResourceIndex? _resourceIndex;
        private readonly OutputLogService _log;
        private readonly Func<EditorService> _editorService;
        private readonly ModuleValidator _validator = new();

        public ObservableCollection<ValidationIssue> Issues { get; } = new();

        [ObservableProperty]
        private string _statusText = "Not run yet.";

        [ObservableProperty]
        private bool _isRunning;

        public ValidationViewModel(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            Func<EditorService> editorService,
            IGameCodeIndex? gameCodeIndex = null,
            Domain.GameData.Resources.ResourceIndex? resourceIndex = null)
        {
            _resourceIndex = resourceIndex;
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
            _gameCodeIndex = gameCodeIndex;
            Id = "Validation";
            Title = "Validation";
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task Run()
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
            {
                StatusText = "No module open.";
                return;
            }

            IsRunning = true;

            try
            {
                StatusText = "Saving open editors before validation...";
                if (!await _editorService().SaveAllAsync().ConfigureAwait(true))
                {
                    StatusText = "Validation cancelled because an open editor could not be saved.";
                    _log.AppendLine("Validation aborted: one or more open editors were not saved.");
                    return;
                }

                Issues.Clear();
                StatusText = "Running validation...";
                var context = new ValidationContext(workspace, _gameCodeIndex, _resourceIndex);
                var result = await _validator.RunAsync(context).ConfigureAwait(true);

                foreach (var issue in result.Issues)
                    Issues.Add(issue);

                StatusText =
                    $"{result.Issues.Count} issue(s) — {result.ErrorCount} error(s), {result.WarningCount} warning(s) — in {result.TotalElapsed.TotalMilliseconds:0}ms.";
                _log.AppendLine($"Validation complete: {StatusText}");
            }
            catch (Exception ex)
            {
                StatusText = $"Validation failed: {ex.Message}";
                _log.AppendLine($"Validation failed: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private bool CanRun() => !IsRunning;

        partial void OnIsRunningChanged(bool value)
        {
            RunCommand.NotifyCanExecuteChanged();
        }
    }
}
