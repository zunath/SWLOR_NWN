namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// One tab of a blueprint editor: a title and whatever view model fills it.
    /// </summary>
    /// <remarks>
    /// Content is deliberately untyped so a tab can be a list of schema fields, a model grid, a
    /// behavior list or the variable table without the editor knowing the difference - the app's
    /// DataTemplates pick the view by content type, the same way field controls are chosen.
    /// </remarks>
    public sealed class EditorTabViewModel
    {
        public EditorTabViewModel(string title, object content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; }

        public object Content { get; }
    }
}
