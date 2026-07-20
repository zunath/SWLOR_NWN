using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Memento for adding a named field to a struct (JsonGffStruct.Add). Reverting removes it by
    /// name; JsonGffStruct.Add always recomputes nwn_gff's sorted insertion position from the
    /// struct's current contents, so re-applying (redo) reproduces the original position exactly.
    /// </summary>
    public sealed class AddFieldEdit : IDocumentEdit
    {
        private readonly JsonGffStruct _struct;
        private readonly string _name;
        private readonly JsonGffField _field;

        internal AddFieldEdit(JsonGffStruct owner, string name, JsonGffField field)
        {
            _struct = owner;
            _name = name;
            _field = field;
        }

        public void Apply()
        {
            _struct.Add(_name, _field);
        }

        public void Revert()
        {
            _struct.Remove(_name);
        }

        public string Describe()
        {
            return $"Add field '{_name}'";
        }
    }

    /// <summary>
    /// Memento for removing a named field from a struct (JsonGffStruct.Remove). Reverting
    /// re-adds the exact same field instance; see <see cref="AddFieldEdit"/> for why this
    /// reproduces the original position.
    /// </summary>
    public sealed class RemoveFieldEdit : IDocumentEdit
    {
        private readonly JsonGffStruct _struct;
        private readonly string _name;
        private readonly JsonGffField _field;

        internal RemoveFieldEdit(JsonGffStruct owner, string name, JsonGffField field)
        {
            _struct = owner;
            _name = name;
            _field = field;
        }

        public void Apply()
        {
            _struct.Remove(_name);
        }

        public void Revert()
        {
            _struct.Add(_name, _field);
        }

        public string Describe()
        {
            return $"Remove field '{_name}'";
        }
    }
}
