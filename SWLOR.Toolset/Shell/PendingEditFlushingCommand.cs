using System.Windows.Input;

namespace SWLOR.Toolset.Shell
{
    /// <summary>
    /// Wraps a shell command so it commits any in-progress spinner edit before executing. Save
    /// shortcuts use this: a keyboard save never moves focus, so without the flush a number still
    /// being typed in a <c>NumericUpDown</c> would be silently absent from the saved file.
    /// </summary>
    internal sealed class PendingEditFlushingCommand : ICommand
    {
        private readonly MainWindow _window;
        private readonly ICommand _inner;

        public PendingEditFlushingCommand(MainWindow window, ICommand inner)
        {
            _window = window;
            _inner = inner;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => _inner.CanExecuteChanged += value;
            remove => _inner.CanExecuteChanged -= value;
        }

        public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);

        public void Execute(object? parameter)
        {
            _window.CommitPendingSpinnerEdit();
            _inner.Execute(parameter);
        }
    }
}
