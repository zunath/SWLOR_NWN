using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Core;

namespace SWLOR.Toolset
{
    /// <summary>
    /// Resolves a view for a view model by naming convention: a type in a ".ViewModels[.X]"
    /// namespace named "FooViewModel" maps to a type in the matching ".Views[.X]" namespace named
    /// "FooView". Dock.Avalonia treats any <see cref="IDockable"/> as data-template-worthy even if
    /// no matching view type is found (it falls back to its own default presentation).
    /// </summary>
    public sealed class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            var viewModelType = data.GetType();
            var viewTypeName = viewModelType.FullName!
                .Replace(".ViewModels", ".Views")
                .Replace(".Panels", ".Views")
                .Replace("ViewModel", "View");

            var viewType = viewModelType.Assembly.GetType(viewTypeName);
            if (viewType != null)
                return (Control)Activator.CreateInstance(viewType)!;

            return new TextBlock { Text = "Not Found: " + viewTypeName };
        }

        public bool Match(object? data) => data is IDockable;
    }
}
