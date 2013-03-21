using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Hellosam.Net.Collections
{
    public class ObservableDictionaryWithNotification<TKey, TValue> :
        ObservableDictionary<TKey, TValue> where TValue : class
    {
        private bool _sourceHasNotification;
        private HashSet<TKey> _deferredElementReplace;
        private HashSet<TKey> _deferredSkipEmit;

        public ObservableDictionaryWithNotification()
            : base()
        {
            CheckSourceCapability();
        }

        public ObservableDictionaryWithNotification(IDictionary<TKey, TValue> source)
            : base(source)
        {
            CheckSourceCapability();
        }

        public ObservableDictionaryWithNotification(IComparer<TKey> comparer)
            : base(comparer)
        {
            CheckSourceCapability();
        }

        public ObservableDictionaryWithNotification(IDictionary<TKey, TValue> source, IComparer<TKey> comparer)
            : base(source, comparer)
        {
            CheckSourceCapability();
        }

        protected override void StartDefer()
        {
            base.StartDefer();
            if (_deferredElementReplace == null)
                _deferredElementReplace = new HashSet<TKey>();
            if (_deferredSkipEmit == null)
                _deferredSkipEmit = new HashSet<TKey>();
        }

        protected override void ProcessDefer()
        {
            base.ProcessDefer();
            foreach (var key in _deferredSkipEmit)
                _deferredElementReplace.Remove(key);
            foreach (var key in _deferredElementReplace)
                EmitNotification(key);
            _deferredSkipEmit.Clear();
            _deferredElementReplace.Clear();
        }

        private void CheckSourceCapability()
        {
            _sourceHasNotification = typeof (INotifyPropertyChanged).IsAssignableFrom(typeof (TValue));
        }

        protected override void BaseAdd(KeyValuePair<TKey, TValue> item)
        {
            base.BaseAdd(item);
            if (_sourceHasNotification && item.Value != null)
            {
                new InverseNotificationRelay(this, item);
            }
        }

        protected void EmitNotification(KeyValuePair<TKey, TValue> item)
        {
            if (IsDeferred)
            {
                _deferredElementReplace.Add(item.Key);
                return;
            }
            EmitNotification(item.Key);
        }

        protected void EmitNotification(TKey key)
        {
            DoRead(() =>
            {
                TValue value;
                var index = IndexOfKey(key, out value);
                if (index >= 0)
                {
                    OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            value, value, index));
                }
            });
        }

        protected override void OnCollectionChangedForKey(TKey key, NotifyCollectionChangedEventArgs args)
        {
            if (IsDeferred)
                _deferredSkipEmit.Add(key);
            base.OnCollectionChangedForKey(key, args);
        }

        private class InverseNotificationRelay
        {
            private readonly WeakReference _dictionaryRef;
            private KeyValuePair<TKey, TValue> _item;

            public InverseNotificationRelay(ObservableDictionaryWithNotification<TKey, TValue> dictionary,
                                            KeyValuePair<TKey, TValue> item)
            {
                _dictionaryRef = new WeakReference(dictionary);
                _item = item;

                ((INotifyPropertyChanged) item.Value).PropertyChanged +=
                    new PropertyChangedEventHandler(Item_PropertyChanged);
            }

            private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var dictionary = _dictionaryRef.Target as ObservableDictionaryWithNotification<TKey, TValue>;
                if (dictionary == null) return;

                dictionary.EmitNotification(_item);
            }
        }
    }
}