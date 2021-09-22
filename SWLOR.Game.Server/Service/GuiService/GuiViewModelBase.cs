using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Annotations;
using SWLOR.Game.Server.Service.GuiService.Component;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiViewModelBase: IGuiViewModel, INotifyPropertyChanged
    {
        private class PropertyDetail
        {
            public object Value { get; set; }
            public Type Type { get; set; }
        }

        private static readonly GuiPropertyConverter _converter = new GuiPropertyConverter();

        protected uint Player { get; private set; }
        protected int WindowToken { get; private set; }

        private readonly Dictionary<string, PropertyDetail> _propertyValues = new Dictionary<string, PropertyDetail>();

        public GuiRectangle Geometry
        {
            get => Get<GuiRectangle>();
            set => Set(value);
        }

        protected T Get<T>([CallerMemberName]string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return default(T);

            if (_propertyValues.ContainsKey(propertyName))
                return (T)_propertyValues[propertyName].Value;

            return default(T);
        }

        protected void Set<T>(T value, [CallerMemberName]string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new Exception("Attempted to Set a value on a ViewModel with a null or empty propertyName.");
            }

            if (_propertyValues.ContainsKey(propertyName))
            {
                // No change detected, exit early.
                if (_propertyValues[propertyName] == (object)value)
                    return;
            }

            if (!_propertyValues.ContainsKey(propertyName))
                _propertyValues[propertyName] = new PropertyDetail();
            

            _propertyValues[propertyName].Value = value;
            _propertyValues[propertyName].Type = typeof(T);
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            var value = _propertyValues[propertyName].Value;
            var json = _converter.ToJson(value);

            NuiSetBind(Player, WindowToken, propertyName, json);
        }
        
        public void Bind(uint player, int windowToken)
        {
            Player = player;
            WindowToken = windowToken;

            // Rebind any existing properties (in the event the window was closed and reopened)
            foreach (var (name, propertyDetail) in _propertyValues)
            {
                var json = _converter.ToJson(propertyDetail.Value);
                NuiSetBind(Player, WindowToken, name, json);
            }

            NuiSetBindWatch(Player, WindowToken, nameof(Geometry), true);
            NuiSetBind(Player, WindowToken, nameof(Geometry), Geometry.ToJson());
        }

        public void UpdatePropertyFromClient(string propertyName)
        {
            var property = _propertyValues[propertyName];
            var json = NuiGetBind(Player, WindowToken, propertyName);
            var value = _converter.ToObject(json, property.Type);

            _propertyValues[propertyName].Value = value;
        }
    }
}
