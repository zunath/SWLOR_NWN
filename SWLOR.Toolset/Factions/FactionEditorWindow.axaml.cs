using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Factions
{
    public partial class FactionEditorWindow : Window
    {
        private readonly FactionEditorViewModel? _viewModel;
        private bool _closeApproved;
        private bool _checkingClose;

        public FactionEditorWindow()
        {
            InitializeComponent();
        }

        public FactionEditorWindow(FactionEditorViewModel viewModel) : this()
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            Closing += OnClosing;
            Closed += (_, _) => viewModel.Dispose();
        }

        public static async Task<IReadOnlyCollection<string>> ShowAsync(
            string moduleRoot,
            OutputLogService log,
            IEditorPromptService prompts)
        {
            var owner = (Avalonia.Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null)
                return Array.Empty<string>();

            var factionPath = Path.Combine(moduleRoot, "fac", "repute.fac.json");
            if (!File.Exists(factionPath))
                throw new FileNotFoundException("The module has no repute.fac faction table.", factionPath);

            var factionCount = await Task.Run(() =>
            {
                using var session = Domain.Editing.DocumentSession.Open(factionPath);
                return new Domain.Documents.FacDocument(session.Document).FactionList.Count;
            }).ConfigureAwait(true);
            // Opening the editor must not parse every blueprint and area-instance file merely to
            // decorate the faction list with usage counts. SWLOR's GIT corpus alone is hundreds of
            // megabytes. The destructive save still discovers and remaps every real reference.
            var usage = Enumerable.Range(0, factionCount).ToDictionary(
                id => id,
                _ => FactionReferenceUsage.Unknown);

            var viewModel = new FactionEditorViewModel(moduleRoot, usage, log, prompts);
            var window = new FactionEditorWindow(viewModel);
            await window.ShowDialog(owner).ConfigureAwait(true);
            return viewModel.ChangedPaths.ToList();
        }

        private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_viewModel?.IsSaving == true)
            {
                e.Cancel = true;
                return;
            }

            if (_closeApproved || _viewModel == null || !_viewModel.IsDirty)
                return;

            e.Cancel = true;
            if (!_checkingClose)
                _ = ConfirmCloseAsync();
        }

        private async Task ConfirmCloseAsync()
        {
            if (_viewModel == null)
                return;

            _checkingClose = true;
            try
            {
                if (await _viewModel.TryCloseAsync().ConfigureAwait(true))
                {
                    _closeApproved = true;
                    Close();
                }
            }
            finally
            {
                _checkingClose = false;
            }
        }
    }
}
