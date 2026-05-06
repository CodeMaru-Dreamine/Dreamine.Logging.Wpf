using System;
using System.Windows;
using System.Windows.Threading;

namespace Dreamine.Logging.Wpf.Services;

/// <summary>
/// Provides UI thread dispatching for WPF log views.
/// </summary>
public sealed class WpfLogUiDispatcher
{
    private readonly Dispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfLogUiDispatcher"/> class.
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
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.Invoke(action);
    }

    /// <summary>
    /// Executes the specified action asynchronously on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void BeginInvoke(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (_dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _dispatcher.BeginInvoke(action);
    }
}