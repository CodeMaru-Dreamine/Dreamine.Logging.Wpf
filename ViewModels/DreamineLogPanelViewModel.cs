using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Dreamine.Logging.Interfaces;
using Dreamine.Logging.Models;
using Dreamine.Logging.Wpf.Services;

namespace Dreamine.Logging.Wpf.ViewModels
{
    /// <summary>
    /// Provides the view model for the Dreamine log panel.
    /// </summary>
    /// <remarks>
    /// Bounds the visible <see cref="Entries"/> collection so memory usage cannot
    /// grow unboundedly under sustained logging. Incoming entries from any thread
    /// are coalesced into UI-thread batches via <see cref="BatchedDispatcher{T}"/>,
    /// preventing the WPF dispatcher queue from accumulating pending operations.
    /// </remarks>
    public sealed class DreamineLogPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Default maximum number of entries kept in <see cref="Entries"/>.
        /// </summary>
        public const int DefaultDisplayCapacity = 1000;

        private readonly IDreamineLogStore _logStore;
        private readonly BatchedDispatcher<DreamineLogEntry?> _uiDispatcher;
        private readonly int _displayCapacity;
        private DreamineLogEntry? _selectedEntry;
        private bool _autoScroll = true;
        private bool _disposed;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raised on the UI thread after a batch has been appended, with the most
        /// recent entry. Bind a view-side handler here to scroll the DataGrid.
        /// </summary>
        public event EventHandler<DreamineLogEntry>? EntryAppended;

        /// <summary>
        /// Gets the log entries displayed by the log panel.
        /// </summary>
        public ObservableCollection<DreamineLogEntry> Entries { get; } = new();

        /// <summary>
        /// Gets or sets whether the most recent entry is auto-selected.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>true</c>. When the user manually selects an older entry
        /// the caller can set this to <c>false</c> so the selection no longer jumps.
        /// </remarks>
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                if (_autoScroll == value)
                {
                    return;
                }

                _autoScroll = value;
                OnPropertyChanged();
            }
        }

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
        /// <param name="dispatcher">The WPF UI dispatcher wrapper.</param>
        public DreamineLogPanelViewModel(
            IDreamineLogStore logStore,
            WpfLogUiDispatcher dispatcher)
            : this(logStore, dispatcher, DefaultDisplayCapacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DreamineLogPanelViewModel"/> class.
        /// </summary>
        /// <param name="logStore">The log store.</param>
        /// <param name="dispatcher">The WPF UI dispatcher wrapper.</param>
        /// <param name="displayCapacity">Maximum number of entries kept in <see cref="Entries"/>.</param>
        public DreamineLogPanelViewModel(
            IDreamineLogStore logStore,
            WpfLogUiDispatcher dispatcher,
            int displayCapacity)
        {
            _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));
            ArgumentNullException.ThrowIfNull(dispatcher);

            _displayCapacity = displayCapacity > 0 ? displayCapacity : DefaultDisplayCapacity;

            _uiDispatcher = new BatchedDispatcher<DreamineLogEntry?>(
                dispatcher.Dispatcher,
                OnEntriesBatch,
                DispatcherPriority.Background);

            // Seed with whatever the store already has, capped to display capacity.
            var existing = _logStore.GetEntries();
            var skip = Math.Max(0, existing.Count - _displayCapacity);
            for (var i = skip; i < existing.Count; i++)
            {
                Entries.Add(existing[i]);
            }

            _logStore.LogAdded += OnLogAdded;
        }

        /// <summary>
        /// Clears all displayed log entries and the underlying store.
        /// </summary>
        /// <remarks>
        /// The clear command is serialized through the same UI batch queue as
        /// pending appends, so any in-flight entries arriving from worker threads
        /// are discarded together with the existing display.
        /// </remarks>
        public void Clear()
        {
            _logStore.Clear();

            // Sentinel: null indicates "clear" within the batch stream.
            _uiDispatcher.Enqueue(null);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _logStore.LogAdded -= OnLogAdded;
        }

        private void OnLogAdded(object? sender, DreamineLogEntry entry)
        {
            if (_disposed)
            {
                return;
            }

            // Producer side: any thread. Just enqueue.
            _uiDispatcher.Enqueue(entry);
        }

        private void OnEntriesBatch(IReadOnlyList<DreamineLogEntry?> batch)
        {
            // UI thread.
            if (_disposed)
            {
                return;
            }

            DreamineLogEntry? lastAppended = null;

            foreach (var entry in batch)
            {
                if (entry is null)
                {
                    // Clear sentinel.
                    Entries.Clear();
                    SelectedEntry = null;
                    lastAppended = null;
                    continue;
                }

                Entries.Add(entry);
                lastAppended = entry;
            }

            // Enforce display cap. Trim from the front.
            var overflow = Entries.Count - _displayCapacity;
            for (var i = 0; i < overflow; i++)
            {
                Entries.RemoveAt(0);
            }

            if (lastAppended is not null)
            {
                if (_autoScroll)
                {
                    SelectedEntry = lastAppended;
                }

                EntryAppended?.Invoke(this, lastAppended);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
