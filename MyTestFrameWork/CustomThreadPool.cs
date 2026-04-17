using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MyTestFramework
{
    public class CustomThreadPool : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Queue<WorkItem> _workQueue = new Queue<WorkItem>();
        private readonly List<WorkerThread> _workers = new List<WorkerThread>();
        private readonly Thread _managerThread;
        private readonly ManualResetEventSlim _workAvailable = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly TimeSpan _idleTimeout;
        private readonly TimeSpan _queueWaitThreshold;
        private readonly Stopwatch _globalStopwatch = Stopwatch.StartNew();

        public event Action<string>? LogMessage;
        public event Action<string>? LogError;
        public event Action<PoolState>? StateChanged;

        private int _totalTasksProcessed;
        private int _totalThreadsCreated;
        private int _totalThreadsDestroyed;
        private int _pendingTasks;
        private DateTime _lastStateLog = DateTime.UtcNow;
        private readonly TimeSpan _stateLogInterval = TimeSpan.FromSeconds(2);

        private bool _disposed;

        public int ActiveThreads { get { lock (_lock) return _workers.Count; } }
        public int QueueSize { get { lock (_lock) return _workQueue.Count; } }
        public int TotalTasksProcessed => _totalTasksProcessed;
        public int TotalThreadsCreated => _totalThreadsCreated;
        public int TotalThreadsDestroyed => _totalThreadsDestroyed;

        public struct PoolState
        {
            public int ActiveThreads;
            public int QueueSize;
            public int MinThreads;
            public int MaxThreads;
            public int TasksProcessed;
            public int ThreadsCreated;
            public int ThreadsDestroyed;
        }

        public CustomThreadPool(int minThreads = 2, int maxThreads = 10,
            TimeSpan? idleTimeout = null, TimeSpan? queueWaitThreshold = null)
        {
            _minThreads = Math.Max(1, minThreads);
            _maxThreads = Math.Max(_minThreads, maxThreads);
            _idleTimeout = idleTimeout ?? TimeSpan.FromSeconds(5);
            _queueWaitThreshold = queueWaitThreshold ?? TimeSpan.FromMilliseconds(200);

            for (int i = 0; i < _minThreads; i++)
                CreateWorkerThread();

            _managerThread = new Thread(ManagerLoop) { Name = "ThreadPoolManager", IsBackground = true };
            _managerThread.Start();

            LogMessage?.Invoke($"[ThreadPool] Пул создан: min={_minThreads}, max={_maxThreads}, idleTimeout={_idleTimeout.TotalSeconds}s, queueThreshold={_queueWaitThreshold.TotalMilliseconds}ms");
            RaiseStateChanged();
        }

        public void QueueUserWorkItem(Action<CancellationToken> work, string? description = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CustomThreadPool));
            var item = new WorkItem(work, description);
            lock (_lock)
            {
                _workQueue.Enqueue(item);
                _pendingTasks++;
                _workAvailable.Set();
            }
            LogMessage?.Invoke($"[ThreadPool] ➕ Задача добавлена: '{description ?? "без описания"}' (очередь: {QueueSize})");
            RaiseStateChanged();
        }

        private void CreateWorkerThread()
        {
            var worker = new WorkerThread(this, _cts.Token);
            lock (_lock) { _workers.Add(worker); _totalThreadsCreated++; }
            var thread = new Thread(worker.Run) { Name = $"PoolWorker_{worker.Id}", IsBackground = true };
            worker.Thread = thread;
            thread.Start();
            LogMessage?.Invoke($"[ThreadPool] ⬆️  Поток создан (ID: {worker.Id}, всего: {_workers.Count})");
            RaiseStateChanged();
        }

        private void DestroyWorkerThread(WorkerThread worker)
        {
            worker.Stop();
            lock (_lock) { _workers.Remove(worker); _totalThreadsDestroyed++; }
            LogMessage?.Invoke($"[ThreadPool] ⬇️  Поток уничтожен (ID: {worker.Id}, осталось: {_workers.Count})");
            RaiseStateChanged();
        }

        private void ManagerLoop()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                token.WaitHandle.WaitOne(200);
                lock (_lock)
                {
                    if (_disposed) break;
                    int queueSize = _workQueue.Count;
                    int activeWorkers = _workers.Count;

                    if (queueSize > 0 && activeWorkers < _maxThreads)
                    {
                        var now = DateTime.UtcNow;
                        bool starving = false;
                        foreach (var item in _workQueue)
                            if ((now - item.EnqueueTime) > _queueWaitThreshold) { starving = true; break; }
                        if (queueSize > activeWorkers || starving)
                            CreateWorkerThread();
                    }

                    if (activeWorkers > _minThreads)
                    {
                        var idleThreshold = DateTime.UtcNow.Subtract(_idleTimeout);
                        foreach (var worker in _workers)
                            if (!worker.IsBusy && worker.LastActivity < idleThreshold)
                            { DestroyWorkerThread(worker); break; }
                    }

                    if ((DateTime.UtcNow - _lastStateLog) > _stateLogInterval)
                    {
                        _lastStateLog = DateTime.UtcNow;
                        RaiseStateChanged();
                    }
                }
            }
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(new PoolState
            {
                ActiveThreads = ActiveThreads,
                QueueSize = QueueSize,
                MinThreads = _minThreads,
                MaxThreads = _maxThreads,
                TasksProcessed = _totalTasksProcessed,
                ThreadsCreated = _totalThreadsCreated,
                ThreadsDestroyed = _totalThreadsDestroyed
            });
        }

        internal void OnTaskCompleted()
        {
            int remaining = Interlocked.Decrement(ref _pendingTasks);
            if (remaining == 0)
            {
                _globalStopwatch.Stop();
                LogMessage?.Invoke("[ThreadPool] ===== ВСЕ ЗАДАЧИ ВЫПОЛНЕНЫ =====");
                LogMessage?.Invoke($"[ThreadPool] Общее время работы пула: {_globalStopwatch.ElapsedMilliseconds} ms");
                LogMessage?.Invoke($"[ThreadPool] Всего задач обработано: {_totalTasksProcessed}");
                LogMessage?.Invoke($"[ThreadPool] Создано потоков: {_totalThreadsCreated}, уничтожено: {_totalThreadsDestroyed}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            _workAvailable.Set();
            _managerThread.Join(1000);
            lock (_lock) { foreach (var w in _workers) w.Stop(); _workers.Clear(); }
            _cts.Dispose();
            _workAvailable.Dispose();
            LogMessage?.Invoke("[ThreadPool] Пул остановлен.");
        }

        private class WorkItem
        {
            public Action<CancellationToken> Work { get; }
            public string? Description { get; }
            public DateTime EnqueueTime { get; }
            public WorkItem(Action<CancellationToken> work, string? desc) => (Work, Description, EnqueueTime) = (work, desc, DateTime.UtcNow);
        }

        private class WorkerThread
        {
            private static int _nextId = 1;
            private readonly CustomThreadPool _pool;
            private readonly CancellationToken _ct;
            private readonly object _stateLock = new();
            private bool _isBusy;
            private DateTime _lastActivity;
            private volatile bool _shouldStop;

            public int Id { get; }
            public Thread? Thread { get; set; }
            public bool IsBusy { get { lock (_stateLock) return _isBusy; } private set { lock (_stateLock) _isBusy = value; } }
            public DateTime LastActivity { get { lock (_stateLock) return _lastActivity; } private set { lock (_stateLock) _lastActivity = value; } }

            public WorkerThread(CustomThreadPool pool, CancellationToken ct)
            {
                _pool = pool; _ct = ct; Id = Interlocked.Increment(ref _nextId); UpdateActivity();
            }

            public void Run()
            {
                try
                {
                    while (!_shouldStop && !_ct.IsCancellationRequested)
                    {
                        WorkItem? item = null;
                        lock (_pool._lock)
                        {
                            if (_pool._workQueue.Count > 0) item = _pool._workQueue.Dequeue();
                            else _pool._workAvailable.Reset();
                        }

                        if (item != null)
                        {
                            IsBusy = true; UpdateActivity();
                            _pool.LogMessage?.Invoke($"[ThreadPool] ▶️  Поток {Id} начал: '{item.Description}'");
                            _pool.RaiseStateChanged();

                            ExecuteWorkItem(item);

                            _pool.LogMessage?.Invoke($"[ThreadPool] ✅ Поток {Id} завершил: '{item.Description}'");
                            Interlocked.Increment(ref _pool._totalTasksProcessed);
                            _pool.OnTaskCompleted();

                            IsBusy = false; UpdateActivity();
                            _pool.RaiseStateChanged();
                        }
                        else
                        {
                            UpdateActivity();
                            _pool._workAvailable.Wait(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _pool.LogError?.Invoke($"[ThreadPool] ❌ Ошибка в потоке {Id}: {ex.Message}");
                    if (!_shouldStop && !_ct.IsCancellationRequested)
                    {
                        lock (_pool._lock) _pool._workers.Remove(this);
                        _pool.CreateWorkerThread();
                    }
                }
            }

            private void ExecuteWorkItem(WorkItem item)
            {
                try { item.Work(_ct); }
                catch (OperationCanceledException) { _pool.LogMessage?.Invoke($"[ThreadPool] ⏹️  Задача '{item.Description}' отменена."); }
                catch (Exception ex) { _pool.LogError?.Invoke($"[ThreadPool] ❌ Ошибка в задаче '{item.Description}': {ex.Message}"); }
            }

            public void Stop() => _shouldStop = true;
            private void UpdateActivity() => LastActivity = DateTime.UtcNow;
        }
    }
}