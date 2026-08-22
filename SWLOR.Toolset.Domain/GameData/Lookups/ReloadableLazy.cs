namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>
    /// A small resettable equivalent of <see cref="Lazy{T}"/> for lookup data derived from the
    /// active HAK stack. Reset never exposes a partially rebuilt value: the next reader constructs
    /// the complete replacement while holding the instance lock.
    /// </summary>
    internal sealed class ReloadableLazy<T>
    {
        private readonly object _gate = new();
        private readonly Func<T> _factory;
        private T? _value;
        private bool _isValueCreated;

        public ReloadableLazy(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T Value
        {
            get
            {
                lock (_gate)
                {
                    if (!_isValueCreated)
                    {
                        _value = _factory();
                        _isValueCreated = true;
                    }

                    return _value!;
                }
            }
        }

        public bool IsValueCreated
        {
            get
            {
                lock (_gate)
                    return _isValueCreated;
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _value = default;
                _isValueCreated = false;
            }
        }
    }
}
