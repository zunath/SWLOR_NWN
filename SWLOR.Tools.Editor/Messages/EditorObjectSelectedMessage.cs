namespace SWLOR.Tools.Editor.Messages
{
    public class EditorObjectSelectedMessage<T>
    {
        public T SelectedObject{ get; set; }

        public EditorObjectSelectedMessage(T selectedObject)
        {
            SelectedObject = selectedObject;
        }
    }
}
