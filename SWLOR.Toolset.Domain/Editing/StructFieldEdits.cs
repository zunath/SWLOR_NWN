using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>
    /// Memento for adding a named field to a struct (JsonGffStruct.Add). Reverting removes it by
    /// name; JsonGffStruct.Add always recomputes nwn_gff's sorted insertion position from the
    /// struct's current contents, so re-applying (redo) reproduces the original position exactly.
    /// </summary>
    public sealed class AddFieldEdit : IDocumentEdit, IDocumentEditTargetProvider
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

        public IEnumerable<object> GetMutationTargets() => new object[] { _field };
    }

    /// <summary>
    /// Memento for removing a named field from a struct (JsonGffStruct.Remove). Reverting
    /// re-adds the exact same field instance; see <see cref="AddFieldEdit"/> for why this
    /// reproduces the original position.
    /// </summary>
    public sealed class RemoveFieldEdit : IDocumentEdit, IDocumentEditTargetProvider
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

        public IEnumerable<object> GetMutationTargets() => new object[] { _field };
    }

    /// <summary>
    /// Memento for rewriting a struct's "__struct_id" (JsonGffStruct.SetStructId). Holds the raw
    /// token either side of the change rather than a parsed number, so reverting restores the
    /// source file's exact bytes.
    /// </summary>
    public sealed class StructIdEdit : IDocumentEdit
    {
        private readonly JsonGffStruct _struct;
        private readonly byte[]? _oldValue;
        private readonly byte[]? _newValue;

        internal StructIdEdit(JsonGffStruct owner, byte[]? oldValue, byte[]? newValue)
        {
            _struct = owner;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Apply()
        {
            _struct.RawStructId = _newValue;
        }

        public void Revert()
        {
            _struct.RawStructId = _oldValue;
        }

        public string Describe()
        {
            return "Renumber list element";
        }
    }
}
