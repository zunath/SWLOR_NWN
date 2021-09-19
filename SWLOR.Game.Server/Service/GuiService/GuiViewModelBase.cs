using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Annotations;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiViewModelBase: IGuiDataModel, INotifyPropertyChanged
    {
        public void Refresh(uint player, int windowToken)
        {
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
