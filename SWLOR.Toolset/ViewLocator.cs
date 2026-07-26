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
        /// <summary>
        /// The view type name this convention expects for a view model type.
        /// </summary>
        /// <remarks>
        /// Note what this does <b>not</b> do: it never appends ".Views". A view model in
        /// <c>SWLOR.Toolset.Editors</c> must have its view in <c>SWLOR.Toolset.Editors</c> too —
        /// the editor views live in a <c>Views/</c> <i>folder</i> but deliberately declare the parent
        /// namespace. Putting a new editor view in a <c>.Views</c> namespace compiles cleanly and
        /// then silently renders the not-found placeholder at runtime, which is exactly how the
        /// script editor shipped broken; <c>ViewLocatorTests</c> now fails instead.
        /// </remarks>
        public static string ResolveViewTypeName(Type viewModelType) =>
            viewModelType.FullName!
                .Replace(".ViewModels", ".Views")
                .Replace(".Panels", ".Views")
                .Replace("ViewModel", "View");

        /// <summary>The view type for a view model type, or null when the convention finds none.</summary>
        public static Type? ResolveViewType(Type viewModelType) =>
            viewModelType.Assembly.GetType(ResolveViewTypeName(viewModelType));

        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            var viewModelType = data.GetType();
            var viewType = ResolveViewType(viewModelType);
            if (viewType != null)
                return (Control)Activator.CreateInstance(viewType)!;

            return new TextBlock { Text = "Not Found: " + ResolveViewTypeName(viewModelType) };
        }

        public bool Match(object? data) => data is IDockable;
    }
}
