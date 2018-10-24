namespace SWLOR.Tools.Editor.Messages
{
    public class DeleteEditorObject<T>
    {
        public T DeletedEditorObject { get; set; }

        public DeleteEditorObject(T newEditorObject)
        {
            DeletedEditorObject = newEditorObject;
        }
    }
}
