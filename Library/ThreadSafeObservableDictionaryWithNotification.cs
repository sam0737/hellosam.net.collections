using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Hellosam.Net.Collections
{
    public class ThreadSafeObservableDictionaryWithNotification<TKey, TValue> :
        ThreadSafeObservableDictionary<TKey, TValue> where TValue : class
    {
        private bool _sourceHasNotification;

        public ThreadSafeObservableDictionaryWithNotification()
            : base()
        {
            CheckSourceCapability();
        }

        public ThreadSafeObservableDictionaryWithNotification(IDictionary<TKey, TValue> source)
            : base(source)
        {
            CheckSourceCapability();
        }

        public ThreadSafeObservableDictionaryWithNotification(IComparer<TKey> comparer)
            : base(comparer)
        {
            CheckSourceCapability();
        }

        public ThreadSafeObservableDictionaryWithNotification(IDictionary<TKey, TValue> source, IComparer<TKey> comparer)
            : base(source, comparer)
        {
            CheckSourceCapability();
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

        private class InverseNotificationRelay
        {
            private readonly WeakReference _dictionaryRef;
            private KeyValuePair<TKey, TValue> _item;

            public InverseNotificationRelay(ThreadSafeObservableDictionaryWithNotification<TKey, TValue> dictionary,
                                            KeyValuePair<TKey, TValue> item)
            {
                _dictionaryRef = new WeakReference(dictionary);
                _item = item;

                ((INotifyPropertyChanged)item.Value).PropertyChanged +=
                    new PropertyChangedEventHandler(Item_PropertyChanged);
            }

            private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var dictionary = _dictionaryRef.Target as ThreadSafeObservableDictionaryWithNotification<TKey, TValue>;
                if (dictionary == null) return;

                dictionary.EmitNotification(_item);
            }
        }

    }
}