using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hellosam.Net.Collections
{
    /// <summary>
    /// Represents a thread-safe, observable collection of key/value pairs that can be accessed by multiple threads concurrently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class has INotifyCollectionChanged implemented, so that it could be bound to WPF items control as ItemsSource.
    /// </para>
    /// <para>
    /// The implementation is AVLTree backed - key lookup, insert, delete, replacement is O(log n). 
    /// However, the observable part in the framework handles insertion and removal in O(n).
    /// </para>
    /// <para>
    /// Multiple thread could read and write this dictionary. Lock is used. 
    /// If performance is critical, please consider using System.Collections.Concurrent.ConcurrentDictionary provided by .NET 4 instead.
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class ThreadSafeObservableDictionary<TKey, TValue> : ObservableDictionary<TKey, TValue>
    {
        private SynchronizationContext syncContext;
        private object _postQueueLock = new object();

        private volatile Queue<NotifyCollectionChangedEventArgs> _postQueue =
            new Queue<NotifyCollectionChangedEventArgs>();

        private ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();

        public ThreadSafeObservableDictionary() : base()
        {
            syncContext = SynchronizationContext.Current;
        }

        public ThreadSafeObservableDictionary(IDictionary<TKey, TValue> source) : base(source)
        {
            syncContext = SynchronizationContext.Current;
        }

        public ThreadSafeObservableDictionary(IComparer<TKey> comparer) : base(comparer)
        {
            syncContext = SynchronizationContext.Current;
        }

        public ThreadSafeObservableDictionary(IDictionary<TKey, TValue> source, IComparer<TKey> comparer) : base(source, comparer)
        {
            syncContext = SynchronizationContext.Current;
        }


        protected override TResult DoRead<TResult>(System.Func<TResult> callback)
        {
            if (_accessLock.IsWriteLockHeld || _accessLock.IsReadLockHeld)
            {
                return callback();
            }
            _accessLock.EnterReadLock();
            try { return callback(); }
            finally
            {
                _accessLock.ExitReadLock();
            }
        }

        protected override TResult DoWrite<TResult>(Func<TResult> callback)
        {
            if (_accessLock.IsWriteLockHeld)
            {
                var x = callback();
                NewSnapshopNeeded();
                return x;
            }
            _accessLock.EnterWriteLock();
            try
            {
                var x = callback();
                NewSnapshopNeeded();
                return x;
            }
            finally
            {
                _accessLock.ExitWriteLock();
            }
        }
        
        protected internal override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (syncContext == null)
            {
                base.OnCollectionChanged(args);
                return;
            }
            lock (_postQueueLock)
            {
                _postQueue.Enqueue(args);
            }
            syncContext.Post(ProcessPostQueue, null);
        }

        private void ProcessPostQueue(object state)
        {
            var oldQueue = _postQueue;
            lock (_postQueueLock)
            {
                _postQueue = new Queue<NotifyCollectionChangedEventArgs>();
            }
            foreach (var arg in oldQueue)
            {
                base.OnCollectionChanged(arg);
            }
        }
    }
}
