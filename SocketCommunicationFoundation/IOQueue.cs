using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;

namespace SocketCommunicationFoundation
{
    public class IOQueue : PipeScheduler
    {
        private static readonly WaitCallback _doWorkCallback = s => ((IOQueue)s).DoWork();

        private readonly object _workSync = new object();
        private readonly ConcurrentQueue<Work> _workItems = new ConcurrentQueue<Work>();
        private bool _doingWork;
        public Action<object> UserCallback { get; set; }
        public override void Schedule(Action<object> action, object state)
        {
            var work = new Work
            {
                Callback = action,
                State = state
            };
            //
            // 摘要:
            //     Adds an object to the end of the System.Collections.Concurrent.ConcurrentQueue`1.
            //
            // 参数:
            //   item:
            //     The object to add to the end of the System.Collections.Concurrent.ConcurrentQueue`1.
            //     The value can be a null reference (Nothing in Visual Basic) for reference types.
            _workItems.Enqueue(work);

            lock (_workSync)
            {
                if (!_doingWork)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(_doWorkCallback, this);
                    _doingWork = true;
                }
            }
        }

        private void DoWork()
        {
            while (true)
            {
                while (_workItems.TryDequeue(out Work item))
                {
                    item.Callback(item.State);
                }

                lock (_workSync)
                {
                    if (_workItems.IsEmpty)
                    {
                        _doingWork = false;
                        return;
                    }
                }
            }
        }

        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }
    }
}
