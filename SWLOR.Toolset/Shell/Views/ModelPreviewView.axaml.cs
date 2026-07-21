using System.ComponentModel;
using Avalonia.Controls;
using SWLOR.Toolset.Shell.Panels;

namespace SWLOR.Toolset.Shell.Views
{
    public partial class ModelPreviewView : UserControl
    {
        private ModelPreviewViewModel? _viewModel;

        public ModelPreviewView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => AttachViewModel();
        }

        private void AttachViewModel()
        {
            if (_viewModel != null)
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModel = DataContext as ModelPreviewViewModel;
            if (_viewModel == null)
                return;

            if (_viewModel.TextureService != null)
                Preview.SetTextureService(_viewModel.TextureService);

            Preview.Model = _viewModel.CurrentModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModelPreviewViewModel.CurrentModel) && _viewModel != null)
                Preview.Model = _viewModel.CurrentModel;
        }
    }
}
