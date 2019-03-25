using System.Collections.ObjectModel;
using Caliburn.Micro;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.ViewModels.Data;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class CustomEffectEditorViewModel: 
        BaseEditorViewModel<CustomEffectViewModel>,
        ICustomEffectEditorViewModel
    {
        public CustomEffectEditorViewModel(
            IEventAggregator eventAggregator, 
            IObjectListViewModel<CustomEffectViewModel> objListVM)
            : base(eventAggregator, objListVM)
        {
            CategoryTypes = new ObservableCollection<CustomEffectCategoryType>
            {
                CustomEffectCategoryType.NormalEffect, 
                CustomEffectCategoryType.Stance, 
                CustomEffectCategoryType.FoodEffect
            };

        }

        protected override CustomEffectViewModel CreateNew()
        {
            var obj = new CustomEffectViewModel();
            return obj;
        }


        private ObservableCollection<CustomEffectCategoryType> _categoryTypes;
        public ObservableCollection<CustomEffectCategoryType> CategoryTypes
        {
            get => _categoryTypes;
            set
            {
                _categoryTypes = value;
                NotifyOfPropertyChange(() => CategoryTypes);
            }
        }

        private CustomEffectCategoryType _selectedCategoryType;
        public CustomEffectCategoryType SelectedCategoryType
        {
            get => _selectedCategoryType;
            set
            {
                _selectedCategoryType = value;
                NotifyOfPropertyChange(() => SelectedCategoryType);
                ActiveObject.CustomEffectCategoryID = (int) SelectedCategoryType;
            }
        }
    }
}
