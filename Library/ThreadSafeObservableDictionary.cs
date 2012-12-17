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
    public class ThreadSafeObservableDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        ICollection,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private ReaderWriterLockSlim _accessLock = new ReaderWriterLockSlim();
        private AVLTree<TKey, TValue> store;

        private volatile ReadOnlyCollection<KeyValuePair<TKey, TValue>> _snapshot;
        private object _snapshotLock = new object();
        private int _newSnapshotNeeded = 1;

        private SynchronizationContext syncContext;

        public ThreadSafeObservableDictionary()
        {
            syncContext = SynchronizationContext.Current;
            store = new AVLTree<TKey, TValue>();
        }

        public ThreadSafeObservableDictionary(IDictionary<TKey, TValue> source)
        {
            syncContext = SynchronizationContext.Current;
            store = new AVLTree<TKey, TValue>();
            foreach (var pair in source)
                BaseAdd(pair);
        }

        public ThreadSafeObservableDictionary(IComparer<TKey> comparer)
        {
            syncContext = SynchronizationContext.Current;
            store = new AVLTree<TKey, TValue>(comparer);
        }

        public ThreadSafeObservableDictionary(IDictionary<TKey, TValue> source, IComparer<TKey> comparer)
        {
            syncContext = SynchronizationContext.Current;
            store = new AVLTree<TKey, TValue>(comparer);
            foreach (var pair in source)
                BaseAdd(pair);
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
            OnPropertyChanged("Count");
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
            store.Add(item);
            var index = store.IndexOfKey(item.Key);
            // Debug.WriteLine(string.Format("Add: {1} - {0}", item.Key, index));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void Clear()
        {
            DoWrite(() =>
                        {
                            OnCollectionChanged(
                                new NotifyCollectionChangedEventArgs(
                                    NotifyCollectionChangedAction.Remove,
                                    store.ToArray(), 0));
                            store.Clear();
                        });
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return DoRead(() => store.Contains(item));
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
            BinaryTreeNode<KeyValuePair<TKey, TValue>> node;
            var index = store.IndexOfKey(key, out node);
            if (index >= 0)
            {
                var value = node.Value;
                store.Remove(node);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                                        NotifyCollectionChangedAction.Remove, value,
                                        index));
                return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            DoRead(() => store.ToArray().CopyTo(array, index));
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public int Count
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
            return DoRead(() => store.Values.Any(v => Comparer<TValue>.Default.Compare(v, value) == 0));
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
                    BinaryTreeNode<KeyValuePair<TKey, TValue>> node;
                    var index = store.IndexOfKey(key, out node);
                    node.Value = new KeyValuePair<TKey, TValue>(node.Value.Key, value);
                    OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                             node.Value,
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

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}
