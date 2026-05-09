using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace Dreamine.Logging.Wpf.Services
{
    /// <summary>
    /// Coalesces high-frequency UI updates into batches dispatched on the WPF UI thread.
    /// </summary>
    /// <remarks>
    /// Producers call <see cref="Enqueue"/> from any thread. The dispatcher schedules
    /// at most one pending <see cref="DispatcherOperation"/> at a time; all items
    /// queued in between are drained in one callback. A single batch is capped at
    /// <see cref="MaxBatchSize"/> items so the UI thread is not blocked too long
    /// even under bursty logging — overflow is processed in the next pass.
    /// </remarks>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class BatchedDispatcher<T>
    {
        /// <summary>
        /// Maximum number of items processed in a single UI-thread batch.
        /// </summary>
        public const int MaxBatchSize = 256;

        private readonly Dispatcher _dispatcher;
        private readonly Action<IReadOnlyList<T>> _onBatch;
        private readonly DispatcherPriority _priority;
        private readonly ConcurrentQueue<T> _pending = new();
        private int _scheduled;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchedDispatcher{T}"/> class.
        /// </summary>
        /// <param name="dispatcher">The target WPF dispatcher.</param>
        /// <param name="onBatch">Callback invoked on the UI thread with the drained batch.</param>
        /// <param name="priority">Dispatcher priority. Defaults to <see cref="DispatcherPriority.Background"/>.</param>
        public BatchedDispatcher(
            Dispatcher dispatcher,
            Action<IReadOnlyList<T>> onBatch,
            DispatcherPriority priority = DispatcherPriority.Background)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _onBatch = onBatch ?? throw new ArgumentNullException(nameof(onBatch));
            _priority = priority;
        }

        /// <summary>
        /// Enqueues an item for batched UI delivery.
        /// </summary>
        /// <param name="item">The item to deliver.</param>
        public void Enqueue(T item)
        {
            _pending.Enqueue(item);
            ScheduleFlushIfNeeded();
        }

        private void ScheduleFlushIfNeeded()
        {
            // Ensure only one DispatcherOperation is pending at any time.
            if (Interlocked.CompareExchange(ref _scheduled, 1, 0) != 0)
            {
                return;
            }

            _dispatcher.BeginInvoke(_priority, new Action(Flush));
        }

        private void Flush()
        {
            try
            {
                var buffer = new List<T>(Math.Min(_pending.Count, MaxBatchSize));

                while (buffer.Count < MaxBatchSize && _pending.TryDequeue(out var item))
                {
                    buffer.Add(item);
                }

                if (buffer.Count > 0)
                {
                    _onBatch(buffer);
                }
            }
            finally
            {
                Volatile.Write(ref _scheduled, 0);

                // Re-arm if producers added more while we were running.
                if (!_pending.IsEmpty)
                {
                    ScheduleFlushIfNeeded();
                }
            }
        }
    }
}
