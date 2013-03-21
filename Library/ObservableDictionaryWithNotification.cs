﻿using System;
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

        private class InverseNotificationRelay
        {
            private readonly WeakReference _dictionaryRef;
            private KeyValuePair<TKey, TValue> _item;
            
            public InverseNotificationRelay(ObservableDictionary<TKey, TValue> dictionary,
                                            KeyValuePair<TKey, TValue> item)
            {
                _dictionaryRef = new WeakReference(dictionary);
                _item = item;

                ((INotifyPropertyChanged) item.Value).PropertyChanged +=
                    new PropertyChangedEventHandler(Item_PropertyChanged);
            }

            private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var dictionary = _dictionaryRef.Target as ObservableDictionary<TKey, TValue>;
                if (dictionary == null) return;

                dictionary.DoRead(
                    () =>
                        {
                            var index = dictionary.IndexOfKey(_item.Key);
                            if (index >= 0)
                            {
                                dictionary.OnCollectionChanged(
                                    new NotifyCollectionChangedEventArgs(
                                        NotifyCollectionChangedAction.Replace,
                                        _item.Value, _item.Value, index));
                            }
                        });
            }
        }
    }
}