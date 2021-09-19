using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Annotations;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.GuiService.Converter;

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
            Console.WriteLine($"propName from Set = {propertyName}");

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
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Console.WriteLine($"prop name = {propertyName}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            var value = _propertyValues[propertyName];
            Json json;

            if (value is string s)
            {
                json = new GuiStringConverter().ToJson(s);
            }
            else if (value is int i)
            {
                json = new GuiIntConverter().ToJson(i);
            }
            else if (value is float f)
            {
                json = new GuiFloatConverter().ToJson(f);
            }
            else if (value is bool b)
            {
                json = new GuiBoolConverter().ToJson(b);
            }
            else if (value is GuiRectangle rect)
            {
                json = new GuiRectangleConverter().ToJson(rect);
            }
            else if (value is GuiColor color)
            {
                json = new GuiColorConverter().ToJson(color);
            }
            else
            {
                throw new Exception($"Converter is not defined for type {value.GetType()}");
            }

            NWScript.NuiSetBind(Player, WindowToken, propertyName, json);
        }

        public void Bind(uint player, int windowToken)
        {
            Player = player;
            WindowToken = windowToken;
        }

        public void UpdatePropertyFromClient(string propertyName)
        {

        }
    }
}
