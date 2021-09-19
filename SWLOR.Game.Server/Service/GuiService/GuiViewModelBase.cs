using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Annotations;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.GuiService.Converter;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public abstract class GuiViewModelBase: IGuiViewModel, INotifyPropertyChanged
    {
        protected uint Player { get; private set; }
        protected int WindowToken { get; private set; }

        private readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

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
                return (T)_propertyValues[propertyName];

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

            _propertyValues[propertyName] = value;
            OnPropertyChanged(propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            var value = _propertyValues[propertyName];
            var json = ConvertToJson(value);

            NuiSetBind(Player, WindowToken, propertyName, json);
        }

        private static Json ConvertToJson(object value)
        {

            if (value is string s)
            {
                return new GuiStringConverter().ToJson(s);
            }
            else if (value is int i)
            {
                return new GuiIntConverter().ToJson(i);
            }
            else if (value is float f)
            {
                return new GuiFloatConverter().ToJson(f);
            }
            else if (value is bool b)
            {
                return new GuiBoolConverter().ToJson(b);
            }
            else if (value is GuiRectangle rect)
            {
                return new GuiRectangleConverter().ToJson(rect);
            }
            else if (value is GuiColor color)
            {
                return new GuiColorConverter().ToJson(color);
            }
            else
            {
                throw new Exception($"Converter is not defined for type {value.GetType()}");
            }
        }

        public void Bind(uint player, int windowToken)
        {
            Player = player;
            WindowToken = windowToken;

            // Rebind any existing properties (in the event the window was closed and reopened)
            foreach (var (name, value) in _propertyValues)
            {
                var json = ConvertToJson(value);
                NuiSetBind(Player, WindowToken, name, json);
            }

            // Bind and watch window geometry
            NuiSetBindWatch(Player, WindowToken, nameof(Geometry), true);
        }

        public void UpdatePropertyFromClient(string propertyName)
        {

        }
    }
}
