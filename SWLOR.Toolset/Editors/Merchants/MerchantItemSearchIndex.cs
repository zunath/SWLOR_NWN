namespace SWLOR.Toolset.Editors.Merchants
{
    /// <summary>
    /// Progressively classifies the shared item catalog into merchant store panels. Each blueprint
    /// pays the lightweight BaseItem lookup at most once while every category reuses the result;
    /// full item stats are loaded only for the requested visible page.
    /// </summary>
    public sealed class MerchantItemSearchIndex
    {
        private readonly IReadOnlyList<MerchantItemDefinition> _catalog;
        private readonly Func<string, MerchantItemDefinition?> _loadSummary;
        private readonly Func<string, MerchantItemDefinition?> _loadDetails;
        private readonly Dictionary<int, List<MerchantItemDefinition>> _itemsByStorePanel = new();
        private readonly SemaphoreSlim _searchGate = new(1, 1);
        private int _scanOffset;

        public MerchantItemSearchIndex(
            IReadOnlyList<MerchantItemDefinition> catalog,
            Func<string, MerchantItemDefinition?> loadSummary,
            Func<string, MerchantItemDefinition?> loadDetails)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _loadSummary = loadSummary ?? throw new ArgumentNullException(nameof(loadSummary));
            _loadDetails = loadDetails ?? throw new ArgumentNullException(nameof(loadDetails));
        }

        public async Task<IReadOnlyList<MerchantItemDefinition>> SearchAsync(
            string query,
            int storePanel,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            if (take <= 0)
                return Array.Empty<MerchantItemDefinition>();

            await _searchGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await Task.Run(
                    () => SearchCore(
                        query.Trim(),
                        storePanel,
                        Math.Max(0, skip),
                        take,
                        cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _searchGate.Release();
            }
        }

        private IReadOnlyList<MerchantItemDefinition> SearchCore(
            string query,
            int storePanel,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var summaries = query.Length == 0
                ? SearchUnfiltered(storePanel, skip, take, cancellationToken)
                : SearchFiltered(query, storePanel, skip, take, cancellationToken);

            var detailed = new List<MerchantItemDefinition>(summaries.Count);
            foreach (var summary in summaries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                detailed.Add(_loadDetails(summary.ResRef) ?? summary);
            }

            return detailed;
        }

        private IReadOnlyList<MerchantItemDefinition> SearchUnfiltered(
            int storePanel,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var required = skip + take;
            var bucket = StorePanelBucket(storePanel);
            while (bucket.Count < required && _scanOffset < _catalog.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = _catalog[_scanOffset];
                var summary = _loadSummary(candidate.ResRef);
                if (summary != null)
                    StorePanelBucket(summary.StorePanel).Add(summary);
                _scanOffset++;
            }

            return bucket.Skip(skip).Take(take).ToList();
        }

        private IReadOnlyList<MerchantItemDefinition> SearchFiltered(
            string query,
            int storePanel,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            var matches = new List<MerchantItemDefinition>();
            var matched = 0;
            foreach (var candidate in _catalog)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!candidate.ResRef.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var summary = _loadSummary(candidate.ResRef);
                if (summary == null || summary.StorePanel != storePanel)
                    continue;
                if (matched++ < skip)
                    continue;

                matches.Add(summary);
                if (matches.Count == take)
                    break;
            }

            return matches;
        }

        private List<MerchantItemDefinition> StorePanelBucket(int storePanel)
        {
            if (!_itemsByStorePanel.TryGetValue(storePanel, out var bucket))
            {
                bucket = new List<MerchantItemDefinition>();
                _itemsByStorePanel.Add(storePanel, bucket);
            }

            return bucket;
        }
    }
}
