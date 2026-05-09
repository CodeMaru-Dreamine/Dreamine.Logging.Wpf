using System;
using System.Windows;
using Dreamine.Logging.Options;
using Dreamine.Logging.Registration;
using Dreamine.Logging.Wpf.Services;
using Dreamine.Logging.Wpf.ViewModels;
using Dreamine.MVVM.Core;

namespace Dreamine.Logging.Wpf.Registration;

/// <summary>
/// Provides registration helpers for Dreamine WPF logging services.
/// </summary>
public static class DreamineLoggingWpfRegistration
{
    private static bool _isExitHandlerRegistered;

    /// <summary>
    /// Registers Dreamine core logging services and WPF logging services.
    /// </summary>
    /// <param name="configure">The optional logging configuration action.</param>
    public static void Register(Action<DreamineLoggingOptions>? configure = null)
    {
        var options = new DreamineLoggingOptions();
        configure?.Invoke(options);

        var asyncSink = DreamineLoggingRegistration.Register(copy =>
        {
            copy.Category = options.Category;
            copy.LogDirectory = options.LogDirectory;
            copy.StoreCapacity = options.StoreCapacity;
            copy.QueueCapacity = options.QueueCapacity;
            copy.DrainBatchSize = options.DrainBatchSize;
            copy.FlushEveryWriteCount = options.FlushEveryWriteCount;
            copy.ShutdownTimeout = options.ShutdownTimeout;
        });

        DMContainer.RegisterSingleton<WpfLogUiDispatcher>(
            new WpfLogUiDispatcher());

        DMContainer.Register<DreamineLogPanelViewModel>(() =>
            new DreamineLogPanelViewModel(
                DMContainer.Resolve<Dreamine.Logging.Interfaces.IDreamineLogStore>(),
                DMContainer.Resolve<WpfLogUiDispatcher>()));

        RegisterShutdownHandler(asyncSink, options.ShutdownTimeout);
    }

    private static void RegisterShutdownHandler(
        Dreamine.Logging.Sinks.AsyncQueueSink asyncSink,
        TimeSpan shutdownTimeout)
    {
        if (_isExitHandlerRegistered)
        {
            return;
        }

        _isExitHandlerRegistered = true;

        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        app.Exit += (_, _) =>
        {
            try
            {
                asyncSink.ShutdownAsync(shutdownTimeout)
                         .GetAwaiter()
                         .GetResult();
            }
            catch
            {
                // Shutdown errors must not prevent process exit.
            }
        };
    }
}