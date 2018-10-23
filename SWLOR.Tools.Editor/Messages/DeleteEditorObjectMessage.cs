namespace SWLOR.Tools.Editor.Messages
{
    public class DeleteEditorObjectMessage<T>
    {
        public T DeletedEditorObject { get; set; }

        public DeleteEditorObjectMessage(T newEditorObject)
        {
            DeletedEditorObject = newEditorObject;
        }
    }
}
