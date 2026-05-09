using System;
using System.Windows;
using System.Windows.Threading;

namespace Dreamine.Logging.Wpf.Services
{
    /// <summary>
    /// Provides UI thread dispatching for WPF log views.
    /// </summary>
    public sealed class WpfLogUiDispatcher
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Gets the underlying WPF <see cref="Dispatcher"/>.
        /// </summary>
        public Dispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfLogUiDispatcher"/> class
        /// using the current application or thread dispatcher.
        /// </summary>
        public WpfLogUiDispatcher()
            : this(Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfLogUiDispatcher"/> class.
        /// </summary>
        /// <param name="dispatcher">The WPF dispatcher.</param>
        public WpfLogUiDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// Executes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Invoke(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcher.CheckAccess())
            {
                action();
                return;
            }

            _dispatcher.Invoke(action);
        }

        /// <summary>
        /// Executes the specified action asynchronously on the UI thread at
        /// <see cref="DispatcherPriority.Background"/> priority.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void BeginInvoke(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_dispatcher.CheckAccess())
            {
                action();
                return;
            }

            _dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }
    }
}
