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
        private readonly Func<IGameCodeIndex?>? _gameCodeIndex;
        private readonly Func<Domain.GameData.Resources.ResourceIndex?>? _resourceIndex;
        private readonly OutputLogService _log;
        private readonly Func<EditorService> _editorService;
        private readonly ModuleValidator _validator = new();

        /// <summary>
        /// Raised while a pack or Build All is running. Validation saves every dirty editor before it
        /// scans, so starting it during one of those replaces resources the packer is copying or the
        /// compiler is walking. <see cref="IsRunning"/> only stops what starts after validation does.
        /// </summary>
        private readonly Services.ModuleMutationLock? _mutationLock;

        public ObservableCollection<ValidationIssue> Issues { get; } = new();

        [ObservableProperty]
        private string _statusText = "Not run yet.";

        [ObservableProperty]
        private bool _isRunning;

        public ValidationViewModel(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            Func<EditorService> editorService,
            Func<IGameCodeIndex?>? gameCodeIndex = null,
            Func<Domain.GameData.Resources.ResourceIndex?>? resourceIndex = null,
            Services.ModuleMutationLock? mutationLock = null)
        {
            _resourceIndex = resourceIndex;
            _mutationLock = mutationLock;
            if (_mutationLock != null)
                _mutationLock.Changed += () => RunCommand.NotifyCanExecuteChanged();
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

            // Rechecked here as well as in the predicate: a keyboard binding or a pack that started
            // between the click and this line would otherwise get through the disabled button.
            if (_mutationLock?.IsLocked == true)
            {
                StatusText = "Validation is unavailable while the module is being packed or built.";
                return;
            }

            IsRunning = true;

            try
            {
                using (Services.ModuleMutationLock.AllowModuleWrites())
                {
                    StatusText = "Saving open editors before validation...";
                    if (!await _editorService().SaveAllAsync().ConfigureAwait(true))
                    {
                        StatusText = "Validation cancelled because an open editor could not be saved.";
                        _log.AppendLine("Validation aborted: one or more open editors were not saved.");
                        return;
                    }
                }

                Issues.Clear();
                StatusText = "Loading validation indexes...";
                var gameCodeIndex = await Task.Run(
                    () => _gameCodeIndex?.Invoke()).ConfigureAwait(true);
                var resourceIndex = _resourceIndex?.Invoke();
                StatusText = "Running validation...";
                var context = new ValidationContext(workspace, gameCodeIndex, resourceIndex);
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

        public bool CanRun() => !IsRunning && _mutationLock?.IsLocked != true;

        partial void OnIsRunningChanged(bool value)
        {
            RunCommand.NotifyCanExecuteChanged();
        }
    }
}
