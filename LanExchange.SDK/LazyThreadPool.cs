﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LanExchange.SDK
{
    public class LazyThreadPool : IDisposable
    {
        private const int NUM_CYCLES_IN_THREAD = 10;

        private readonly LinkedList<PanelItemBase> m_AsyncQueue;
        private List<Thread> m_Threads;
        private long m_NumThreads;

        public event EventHandler<DataReadyArgs> DataReady;
        public event EventHandler NumThreadsChanged;

        public LazyThreadPool()
        {
            m_AsyncQueue = new LinkedList<PanelItemBase>();
            m_Threads = new List<Thread>();
        }

        public long NumThreads
        {
            get { return Interlocked.Read(ref m_NumThreads); }
        }

        private void DoDataReady(PanelItemBase item)
        {
            if (DataReady != null)
                DataReady(this, new DataReadyArgs() { Item = item });
        }

        private void DoNumThreadsChanged()
        {
            if (NumThreadsChanged != null)
                NumThreadsChanged(this, EventArgs.Empty);
        }

        public IComparable AsyncGetData(LazyPanelColumn column, PanelItemBase panelItem)
        {
            IComparable result;
            bool found;
            lock (column)
            {
                found = column.Dict.TryGetValue(panelItem, out result);
            }
            if (found)
                return result;
            lock (m_AsyncQueue)
            {
                if (m_AsyncQueue.Contains(panelItem))
                    m_AsyncQueue.Remove(panelItem);
                m_AsyncQueue.AddFirst(panelItem);
            }
            UpdateThreads(column);
            return null;
        }

        private void AsyncEnum(object state)
        {
            var column = state as LazyPanelColumn;
            if (column == null)
                return;
            Interlocked.Increment(ref m_NumThreads);
            DoNumThreadsChanged();
            int number = 0;
            while (m_AsyncQueue.Count > 0 && number < NUM_CYCLES_IN_THREAD)
            {
                PanelItemBase item = null;
                try
                {
                    lock (m_AsyncQueue)
                    {
                        item = m_AsyncQueue.First.Value;
                        m_AsyncQueue.RemoveFirst();
                    }
                    var result = column.SyncGetData(item);
                    bool bFound = false;
                    IComparable found;
                    lock (column)
                    {
                        bFound = column.Dict.TryGetValue(item, out found);
                        if (!bFound)
                            column.Dict.Add(item, result);
                    }
                    if (!bFound)
                        DoDataReady(item);
                }
                catch
                {
                }
                ++number;
            }
            Interlocked.Decrement(ref m_NumThreads);
            DoNumThreadsChanged();
        }

        private void UpdateThreads(LazyPanelColumn column)
        {
            if (NumThreads * NUM_CYCLES_IN_THREAD < m_AsyncQueue.Count)
                lock (m_Threads)
                {
                    // remove stopped threads
                    for (int i = m_Threads.Count - 1; i >= 0; i--)
                        if (m_Threads[i].ThreadState == ThreadState.Stopped)
                            m_Threads.RemoveAt(i);
                    // start new threads
                    while (NumThreads * NUM_CYCLES_IN_THREAD < m_AsyncQueue.Count)
                    {
                        var thread = new Thread(AsyncEnum);
                        m_Threads.Add(thread);
                        thread.Start(column);
                    }
                }
        }

        public void Dispose()
        {
            lock (m_Threads)
            {
                if (!m_Disposed)
                {
                    for (int i = 0; i < m_Threads.Count; i++)
                        if (m_Threads[i].ThreadState == ThreadState.Running)
                            m_Threads[i].Abort();
                    m_Disposed = true;
                }
            }
        }


        private bool m_Disposed;
    }
}
