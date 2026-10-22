namespace SWLOR.CraftSim.Gates
{
    /// <summary>Collects the rows of one evidence table a gate attaches to its result.</summary>
    internal sealed class EvidenceTable
    {
        private readonly string[] _header;
        private readonly List<IReadOnlyList<string>> _rows = new();

        public EvidenceTable(params string[] header)
        {
            _header = header;
        }

        public int RowCount => _rows.Count;

        public EvidenceTable Add(params string[] cells)
        {
            if (cells.Length != _header.Length)
                throw new ArgumentException($"A row needs {_header.Length} cells but got {cells.Length}.");

            _rows.Add(cells);
            return this;
        }

        public GateTable ToGateTable(string title) => new(title, _header, _rows);
    }
}
