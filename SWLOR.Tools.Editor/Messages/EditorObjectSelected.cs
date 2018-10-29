namespace SWLOR.Tools.Editor.Messages
{
    public class EditorObjectSelected<T>
    {
        public T OldObject { get; set; }
        public T SelectedObject{ get; set; }

        public EditorObjectSelected(T oldObject, T selectedObject)
        {
            OldObject = oldObject;
            SelectedObject = selectedObject;
        }
    }
}
