using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hellosam.Net.Collections
{
    public class ConcurrentObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        ICollection,
        INotifyCollectionChanged
    {
        private ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();
        private SortedList<TKey, TValue> store;

        private volatile ReadOnlyCollection<KeyValuePair<TKey, TValue>> _snapshot;
        private object _snapshotLock = new object();
        private int _newSnapshotNeeded = 1;

        private SynchronizationContext syncContext;

        public ConcurrentObservableDictionary()
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>();
        }

        public ConcurrentObservableDictionary(IDictionary<TKey, TValue> source)
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>();
            foreach (var pair in source)
                BaseAdd(pair);
        }

        public ConcurrentObservableDictionary(IComparer<TKey> comparer)
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>(comparer);
        }

        public ConcurrentObservableDictionary(IDictionary<TKey, TValue> source, IComparer<TKey> comparer)
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>(comparer);
            foreach (var pair in source)
                BaseAdd(pair);
        }

        public ConcurrentObservableDictionary(int capactity)
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>(capactity);
        }

        public ConcurrentObservableDictionary(int capactity, IComparer<TKey> comparer)
        {
            syncContext = SynchronizationContext.Current;
            store = new SortedList<TKey, TValue>(capactity, comparer);
        }

        protected TResult DoRead<TResult>(Func<TResult> callback)
        {
            _accessLock.EnterReadLock();
            try { return callback(); }
            finally
            {
                _accessLock.ExitReadLock();
            }
        }

        protected void DoRead(Action callback)
        {
            DoRead<object>(
                () =>
                {
                    callback();
                    return null;
                });
        }

        protected TResult DoWrite<TResult>(Func<TResult> callback)
        {
            _accessLock.EnterWriteLock();
            try
            {
                NewSnapshopNeeded();
                return callback();
            }
            finally
            {
                _accessLock.ExitWriteLock();
            }
        }

        protected void DoWrite(Action callback)
        {
            DoWrite<object>(
                () =>
                {
                    callback();
                    return null;
                });
        }

        /// <summary>
        /// Gets an immutable snapshot of the collection
        /// </summary>
        public ReadOnlyCollection<KeyValuePair<TKey, TValue>> Snapshot
        {
            get
            {
                return DoRead(
                    () =>
                    {
                        UpdateSnapshot();
                        return _snapshot;
                    });
            }
        }

        void NewSnapshopNeeded()
        {
            Interlocked.CompareExchange(ref _newSnapshotNeeded, 1, 0);
        }

        void UpdateSnapshot()
        {
            if (_newSnapshotNeeded > 0)
                lock (_snapshotLock)
                    if (Interlocked.CompareExchange(ref _newSnapshotNeeded, 0, 1) == 1)
                        _snapshot = new ReadOnlyCollection<KeyValuePair<TKey, TValue>>(store.ToList());
        }

        #region Implementation of IEnumerable

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Snapshot.GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            DoWrite(() => BaseAdd(item));
        }

        private void BaseAdd(KeyValuePair<TKey, TValue> item)
        {
            store.Add(item.Key, item.Value);
            var index = store.IndexOfKey(item.Key);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Clear()
        {
            DoWrite(() =>
            {
                store.Clear();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            });
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return DoRead(() => store.ContainsKey(item.Key) && store.ContainsValue(item.Value));
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            DoRead(() => store.ToArray().CopyTo(array, arrayIndex));
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return DoWrite(() => BaseRemove(item.Key));
        }

        private bool BaseRemove(TKey key)
        {
            var index = store.IndexOfKey(key);
            if (index >= 0)
            {
                var item = new KeyValuePair<TKey, TValue>(store.Keys[index], store.Values[index]);
                store.RemoveAt(index);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item,
                                                                         index));
                return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            DoRead(() => store.ToArray().CopyTo(array, index));
        }

        int ICollection.Count
        {
            get { return DoRead(() => store.Count); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return DoRead(() => store.Count); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Implementation of IDictionary<TKey,TValue>

        public bool ContainsKey(TKey key)
        {
            return DoRead(() => store.ContainsKey(key));
        }

        public bool ContainsValue(TValue value)
        {
            return DoRead(() => store.ContainsValue(value));
        }

        public void Add(TKey key, TValue value)
        {
            DoWrite(() => BaseAdd(new KeyValuePair<TKey, TValue>(key, value)));
        }

        public bool Remove(TKey key)
        {
            return DoWrite(() => BaseRemove(key));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TValue innerValue = default(TValue);
            var result = DoRead(() => store.TryGetValue(key, out innerValue));
            value = innerValue;
            return result;
        }

        public TValue this[TKey key]
        {
            get { return DoRead(() => store[key]); }
            set
            {
                DoWrite(() =>
                {
                    var index = store.IndexOfKey(key);
                    store.Values[index] = value;
                    var item = new KeyValuePair<TKey, TValue>(key, value);
                    OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item,
                                                             index));
                });
            }
        }

        public ICollection<TKey> Keys
        {
            get { return DoRead(() => store.Keys); }
        }

        public ICollection<TValue> Values
        {
            get { return DoRead(() => store.Values); }
        }

        #endregion

        #region Implementation of INotifyCollectionChanged

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private object _postQueueLock = new object();
        private volatile Queue<NotifyCollectionChangedEventArgs> _postQueue =
            new Queue<NotifyCollectionChangedEventArgs>();

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var handler = CollectionChanged;
            if (handler != null)
                if (syncContext == null)
                {
                    handler(this, args);
                }
                else
                {
                    lock (_postQueueLock)
                    {
                        _postQueue.Enqueue(args);
                    }
                    syncContext.Post(ProcessPostQueue, handler);
                }
        }

        void ProcessPostQueue(object state)
        {
            var handler = (NotifyCollectionChangedEventHandler)state;

            var oldQueue = _postQueue;
            lock (_postQueueLock)
            {
                _postQueue = new Queue<NotifyCollectionChangedEventArgs>();
            }
            foreach (var arg in oldQueue)
                handler(this, arg);
        }

        #endregion
    }
}
