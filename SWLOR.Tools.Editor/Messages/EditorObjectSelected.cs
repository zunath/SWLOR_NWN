namespace SWLOR.Tools.Editor.Messages
{
    public class EditorObjectSelected<T>
    {
        public T SelectedObject{ get; set; }

        public EditorObjectSelected(T selectedObject)
        {
            SelectedObject = selectedObject;
        }
    }
}
