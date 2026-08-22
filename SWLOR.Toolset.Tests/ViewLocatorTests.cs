using Avalonia.Controls;
using Dock.Model.Core;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Every docked view model must resolve to a real view type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because the script editor shipped broken and nothing caught it. Its view was
    /// placed in a <c>SWLOR.Toolset.Editors.Views</c> namespace, matching the folder it lives in —
    /// but the existing editor views live in that same folder while declaring the parent namespace
    /// <c>SWLOR.Toolset.Editors</c>, which is what <see cref="ViewLocator"/>'s convention expects.
    /// The result compiled cleanly, the app launched fine, and opening a script rendered a
    /// "Not Found" placeholder instead of the editor.
    /// </para>
    /// <para>
    /// A launch-and-kill smoke test cannot catch that — it only proves the window appeared, not that
    /// any particular tab renders. Asserting the convention over every dockable does, and costs
    /// nothing.
    /// </para>
    /// </remarks>
    public class ViewLocatorTests
    {
        private static IEnumerable<Type> DockableViewModels =>
            typeof(ViewLocator).Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => typeof(IDockable).IsAssignableFrom(t))
                .Where(t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));

        [Test]
        public void EveryDockableViewModelResolvesToAView()
        {
            var models = DockableViewModels.ToList();
            models.Should().NotBeEmpty("the reflection filter must actually be finding view models");

            var unresolved = models
                .Where(t => ViewLocator.ResolveViewType(t) == null)
                .Select(t => $"{t.FullName} -> {ViewLocator.ResolveViewTypeName(t)} (missing)")
                .ToList();

            unresolved.Should().BeEmpty(
                "a view model with no view renders a 'Not Found' placeholder at runtime");
        }

        [Test]
        public void EveryResolvedViewIsAControl()
        {
            foreach (var model in DockableViewModels)
            {
                var view = ViewLocator.ResolveViewType(model);
                if (view == null)
                    continue;

                typeof(Control).IsAssignableFrom(view)
                    .Should().BeTrue("{0} must be a Control to be shown in a dock", view.FullName);

                view.GetConstructor(Type.EmptyTypes)
                    .Should().NotBeNull("{0} is constructed parameterlessly by the locator", view.FullName);
            }
        }

        /// <summary>The specific regression: the script editor's view must be findable.</summary>
        [Test]
        public void TheScriptEditorResolves()
        {
            var viewModel = typeof(ViewLocator).Assembly
                .GetType("SWLOR.Toolset.Editors.ScriptEditorViewModel");

            viewModel.Should().NotBeNull();
            ViewLocator.ResolveViewType(viewModel!).Should().NotBeNull(
                "opening a script must show the editor, not a placeholder");
        }
    }
}
