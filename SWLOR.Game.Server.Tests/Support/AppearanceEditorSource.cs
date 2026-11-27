namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The appearance editor's source as one text: the view model plus the code of its services (Feature/AppearanceEditing), so a
/// test that reads a method by name finds it wherever the editor keeps it.
/// </summary>
public static class AppearanceEditorSource
{
    public static string Read(string repositoryRoot)
    {
        var viewModel = File.ReadAllText(Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel",
            "AppearanceEditorViewModel.cs"));
        var services = Directory.GetFiles(Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Feature", "AppearanceEditing"), "*.cs")
            .Where(path => !Path.GetFileName(path).StartsWith("I", StringComparison.Ordinal) &&
                           !Path.GetFileName(path).StartsWith("AppearanceEditorBindings", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => File.ReadAllText(path))
            .Select(text =>
            {
                var open = text.IndexOf('{', text.IndexOf("namespace ", StringComparison.Ordinal));
                var close = text.LastIndexOf('}');
                return text.Substring(open + 1, close - open - 1);
            });
        return viewModel + "\nnamespace AppearanceEditingSources\n{\n" + string.Concat(services) + "\n}\n";
    }
}
