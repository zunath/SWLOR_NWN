namespace SWLOR.Game.Server.Core
{
    /// <inheritdoc cref="IProcessEventSubscriptions"/>
    public sealed class ProcessEventSubscriptions : IProcessEventSubscriptions
    {
        private readonly IProcessEventSource _source;
        private readonly object _lock = new object();
        private readonly UnhandledExceptionEventHandler _unhandledHandler;
        private readonly EventHandler _exitHandler;
        private Action<object> _onUnhandled;
        private Action _onExit;

        public ProcessEventSubscriptions(IProcessEventSource source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _unhandledHandler = OnUnhandledException;
            _exitHandler = OnProcessExit;
        }

        public bool IsSubscribedToUnhandledException
        {
            get { lock (_lock) { return _onUnhandled != null; } }
        }

        public bool IsSubscribedToProcessExit
        {
            get { lock (_lock) { return _onExit != null; } }
        }

        public void SubscribeUnhandledException(Action<object> onUnhandled)
        {
            if (onUnhandled == null)
                throw new ArgumentNullException(nameof(onUnhandled));

            lock (_lock)
            {
                if (_onUnhandled != null)
                    return;

                _onUnhandled = onUnhandled;
                _source.UnhandledException += _unhandledHandler;
            }
        }

        public void SubscribeProcessExit(Action onExit)
        {
            if (onExit == null)
                throw new ArgumentNullException(nameof(onExit));

            lock (_lock)
            {
                if (_onExit != null)
                    return;

                _onExit = onExit;
                _source.ProcessExit += _exitHandler;
            }
        }

        public void UnsubscribeAll()
        {
            lock (_lock)
            {
                if (_onUnhandled != null)
                {
                    _source.UnhandledException -= _unhandledHandler;
                    _onUnhandled = null;
                }

                if (_onExit != null)
                {
                    _source.ProcessExit -= _exitHandler;
                    _onExit = null;
                }
            }
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Action<object> callback;
            lock (_lock) { callback = _onUnhandled; }
            callback?.Invoke(args.ExceptionObject);
        }

        private void OnProcessExit(object sender, EventArgs args)
        {
            Action callback;
            lock (_lock) { callback = _onExit; }
            callback?.Invoke();
        }
    }
}
