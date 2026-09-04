using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editing
{
    /// <summary>Memento for inserting a struct into a list-typed field (JsonGffField.InsertElement).</summary>
    public sealed class InsertElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly JsonGffStruct _element;

        internal InsertElementEdit(JsonGffField field, int index, JsonGffStruct element)
        {
            _field = field;
            _index = index;
            _element = element;
        }

        public void Apply()
        {
            _field.InsertElement(_index, _element);
        }

        public void Revert()
        {
            _field.RemoveElementAt(_index);
        }

        public string Describe()
        {
            return $"Insert list element at {_index}";
        }

        public IEnumerable<object> GetMutationTargets() =>
            ListElementMutationTargets.Enumerate(_field, _element);
    }

    /// <summary>Memento for removing a struct from a list-typed field (JsonGffField.RemoveElementAt).</summary>
    public sealed class RemoveElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _index;
        private readonly JsonGffStruct _element;

        internal RemoveElementEdit(JsonGffField field, int index, JsonGffStruct element)
        {
            _field = field;
            _index = index;
            _element = element;
        }

        public void Apply()
        {
            _field.RemoveElementAt(_index);
        }

        public void Revert()
        {
            _field.InsertElement(_index, _element);
        }

        public string Describe()
        {
            return $"Remove list element at {_index}";
        }

        public IEnumerable<object> GetMutationTargets() =>
            ListElementMutationTargets.Enumerate(_field, _element);
    }

    /// <summary>Memento for reordering a list-typed field's elements (JsonGffField.MoveElement).</summary>
    public sealed class MoveElementEdit : IDocumentEdit, IDocumentEditTargetProvider
    {
        private readonly JsonGffField _field;
        private readonly int _fromIndex;
        private readonly int _toIndex;

        internal MoveElementEdit(JsonGffField field, int fromIndex, int toIndex)
        {
            _field = field;
            _fromIndex = fromIndex;
            _toIndex = toIndex;
        }

        public void Apply()
        {
            _field.MoveElement(_fromIndex, _toIndex);
        }

        public void Revert()
        {
            _field.MoveElement(_toIndex, _fromIndex);
        }

        public string Describe()
        {
            return $"Move list element {_fromIndex} -> {_toIndex}";
        }

        public IEnumerable<object> GetMutationTargets() => new object[] { _field };
    }

    /// <summary>
    /// Structural list edits own both their list and the subtree whose presence they change.
    /// A later edit to a field inside an inserted or removed element therefore overlaps the
    /// structural edit even though the two mementos hold different object references.
    /// </summary>
    internal static class ListElementMutationTargets
    {
        internal static IEnumerable<object> Enumerate(JsonGffField listField, JsonGffStruct element)
        {
            yield return listField;
            foreach (var target in Enumerate(element))
                yield return target;
        }

        private static IEnumerable<object> Enumerate(JsonGffStruct element)
        {
            yield return element;
            foreach (var (_, field) in element.Entries)
            {
                yield return field;
                switch (field.Type)
                {
                    case GffFieldType.Struct:
                        foreach (var target in Enumerate(field.Struct!))
                            yield return target;
                        break;
                    case GffFieldType.List:
                        foreach (var child in field.Elements!)
                        foreach (var target in Enumerate(child))
                            yield return target;
                        break;
                    case GffFieldType.CExoLocString:
                        foreach (var entry in field.LocStringEntries!)
                            yield return entry;
                        break;
                }
            }
        }
    }
}
