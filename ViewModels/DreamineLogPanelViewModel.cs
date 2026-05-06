using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Wpf.Services;

namespace Dreamine.Logging.Wpf.ViewModels;

/// <summary>
/// Provides the view model for the Dreamine log panel.
/// </summary>
public sealed class DreamineLogPanelViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IDreamineLogStore _logStore;
    private readonly WpfLogUiDispatcher _dispatcher;
    private DreamineLogEntry? _selectedEntry;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the log entries displayed by the log panel.
    /// </summary>
    public ObservableCollection<DreamineLogEntry> Entries { get; } = new();

    /// <summary>
    /// Gets or sets the selected log entry.
    /// </summary>
    public DreamineLogEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (ReferenceEquals(_selectedEntry, value))
            {
                return;
            }

            _selectedEntry = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDetailText));
        }
    }

    /// <summary>
    /// Gets the detail text of the selected log entry.
    /// </summary>
    public string SelectedDetailText => SelectedEntry?.DisplayText ?? string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DreamineLogPanelViewModel"/> class.
    /// </summary>
    /// <param name="logStore">The log store.</param>
    /// <param name="dispatcher">The WPF UI dispatcher.</param>
    public DreamineLogPanelViewModel(
        IDreamineLogStore logStore,
        WpfLogUiDispatcher dispatcher)
    {
        _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        foreach (var entry in _logStore.GetEntries())
        {
            Entries.Add(entry);
        }

        _logStore.LogAdded += OnLogAdded;
    }

    /// <summary>
    /// Clears all displayed log entries.
    /// </summary>
    public void Clear()
    {
        _logStore.Clear();

        _dispatcher.Invoke(() =>
        {
            Entries.Clear();
            SelectedEntry = null;
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logStore.LogAdded -= OnLogAdded;
    }

    private void OnLogAdded(object? sender, DreamineLogEntry entry)
    {
        _dispatcher.BeginInvoke(() =>
        {
            Entries.Add(entry);
            SelectedEntry = entry;
        });
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}