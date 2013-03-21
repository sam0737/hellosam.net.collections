using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Hellosam.Net.Collections
{
    public class ObservableDictionary<TKey, TValue> : IEnumerable<TValue>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private AVLTree<TKey, TValue> store;

        private object _snapshotLock = new object();
        private int _newSnapshotNeeded = 1;
        private volatile ReadOnlyCollection<TValue> _snapshot;

        private int _deferCount;
        private HashSet<string> _deferredPropertyChanges;
        private List<NotifyCollectionChangedEventArgs> _deferredCollectionChanges;

        public ObservableDictionary()
        {
            store = new AVLTree<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> source)
        {
            store = new AVLTree<TKey, TValue>();
            foreach (var pair in source)
                BaseAdd(pair);
        }

        public ObservableDictionary(IComparer<TKey> comparer)
        {
            store = new AVLTree<TKey, TValue>(comparer);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> source, IComparer<TKey> comparer)
        {
            store = new AVLTree<TKey, TValue>(comparer);
            foreach (var pair in source)
                BaseAdd(pair);
        }

        protected void NewSnapshopNeeded()
        {
            Interlocked.CompareExchange(ref _newSnapshotNeeded, 1, 0);
            OnPropertyChanged("Count");
        }

        protected virtual bool IsDeferred
        {
            get { return _deferCount > 0; }
        }

        public virtual IDisposable DeferRefresh()
        {
            if ( _deferCount++ == 0)
                StartDefer();
            return new DeferHelper(this);
        }

        protected virtual void StartDefer()
        {
            if (_deferredPropertyChanges == null)
                _deferredPropertyChanges = new HashSet<string>();
            if (_deferredCollectionChanges == null)
                _deferredCollectionChanges = new List<NotifyCollectionChangedEventArgs>();
        }

        protected virtual void EndDefer()
        {
            if (--_deferCount == 0)
            {
                ProcessDefer();
            }
        }

        protected virtual void ProcessDefer()
        {
            foreach (var key in _deferredPropertyChanges)
                OnPropertyChanged(key);
            _deferredPropertyChanges.Clear();
            foreach (var args in _deferredCollectionChanges)
                OnCollectionChanged(args);
            _deferredCollectionChanges.Clear();
        }

        public class DeferHelper:IDisposable
        {
            private ObservableDictionary<TKey, TValue> _dictionary;
            public DeferHelper(ObservableDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public void Dispose()
            {
                _dictionary.EndDefer();
            }
        }

        protected ReadOnlyCollection<TValue> UpdateSnapshot()
        {
            if (_newSnapshotNeeded > 0)
                lock (_snapshotLock)
                    if (Interlocked.CompareExchange(ref _newSnapshotNeeded, 0, 1) == 1)
                        _snapshot = new ReadOnlyCollection<TValue>(store.Values.ToList());
            return _snapshot;
        }

        /// <summary>
        /// Gets an immutable snapshot of the collection
        /// </summary>
        public ReadOnlyCollection<TValue> Snapshot
        {
            get
            {
                return DoRead(
                    () =>
                        {
                            return UpdateSnapshot();
                        });
            }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public int Count
        {
            get { return DoRead(() => store.Count); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<TKey> Keys
        {
            get { return DoRead(() => store.Keys); }
        }

        public ICollection<TValue> Values
        {
            get { return DoRead(() => store.Values); }
        }

        virtual protected TResult DoRead<TResult>(Func<TResult> callback)
        {
            return callback();
        }

        internal protected void DoRead(Action callback)
        {
            DoRead<object>(
                () =>
                    {
                        callback();
                        return null;
                    });
        }

        virtual protected TResult DoWrite<TResult>(Func<TResult> callback)
        {
            var x = callback();
            NewSnapshopNeeded();
            return x;
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

        public IEnumerator<TValue> GetEnumerator()
        {
            return Snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Snapshot.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            DoWrite(() => BaseAdd(item));
        }

        protected virtual void BaseAdd(KeyValuePair<TKey, TValue> item)
        {
            store.Add(item);
            var index = store.IndexOfKey(item.Key);
            // Debug.WriteLine(string.Format("Add: {1} - {0}", item.Key, index));
            OnCollectionChangedForKey(item.Key, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item.Value, index));
        }
        
        protected internal int IndexOfKey(TKey key, out TValue value)
        {
            // ReadLock must be obtained first
            BinaryTreeNode<KeyValuePair<TKey, TValue>> node;
            var index = store.IndexOfKey(key, out node);
            if (index >= 0)
            {
                value = node.Value.Value;
                return index;
            }
            value = default(TValue);
            return -1;
        }

        public void Clear()
        {
            DoWrite(() =>
                        {
                            if (store.Count == 0) return;
                            OnCollectionChanged(
                                new NotifyCollectionChangedEventArgs(
                                    NotifyCollectionChangedAction.Remove,
                                    store.Select(i => i.Value).ToArray(), 0));
                            store.Clear();
                        });
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
                OnCollectionChangedForKey(value.Key,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Remove, value.Value,
                                              index));
                return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            DoRead(() => store.ToArray().CopyTo(array, index));
        }

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
                                if (node == null)
                                {
                                    BaseAdd(new KeyValuePair<TKey, TValue>(key, value));
                                }
                                else
                                {
                                    var oldValue = node.Value.Value;
                                    node.Value = new KeyValuePair<TKey, TValue>(key, value);
                                    OnCollectionChangedForKey(
                                        key,
                                        new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Replace,
                                            node.Value.Value,
                                            oldValue,
                                            index));
                                }
                            });
            }
        }

        protected virtual void OnCollectionChangedForKey(TKey key, NotifyCollectionChangedEventArgs args)
        {
            OnCollectionChanged(args);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        virtual protected internal void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (IsDeferred)
            {
                _deferredCollectionChanges.Add(args);
                return;
            }
            var handler = CollectionChanged;
            if (handler != null)
                handler(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (IsDeferred)
            {
                _deferredPropertyChanges.Add(name);
                return;
            }
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}