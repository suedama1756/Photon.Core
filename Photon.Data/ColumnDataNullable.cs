using System;
using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    internal class ColumnDataNullable<TDataType> : IColumnData<TDataType?> where TDataType : struct
    {
        private readonly IEqualityComparer<TDataType?> _equalityComparer;
        private BitArray _hasValue;
        private readonly ColumnDataStore<TDataType> _values;
        private IColumnDataObserver[] _observers;

        public ColumnDataNullable() : this(EqualityComparer<TDataType?>.Default) 
        {
        }

        public ColumnDataNullable(IEqualityComparer<TDataType?> equalityComparer)
        {
            _equalityComparer = equalityComparer;
            _values = new ColumnDataStore<TDataType>();
        }

        public void Resize(int capacity, int preserve)
        {
            _values.Resize(capacity, preserve);
            if (_hasValue == null)
            {
                _hasValue = new BitArray(capacity);
            }
            else
            {
                if (capacity != 0) 
                {
                    _hasValue.Length = capacity;
                } 
                else 
                {
                    _hasValue = null;
                }
            }
        }

        public int Capacity 
        {
            get { return _values.Capacity; }
        }

        public bool IsNull(int index) 
        {
            return !_hasValue[index];
        }

        public void Move(int sourceIndex, int targetIndex)
        {
            SetValue(targetIndex, GetValue(sourceIndex));
        }

        public void Subscribe(IColumnDataObserver observer)
        {
            _observers = Arrays.Concat(_observers, observer);
        }

        public Type DataType 
        {
            get
            {
                return typeof(TDataType?);
            }
        }

        public bool Clear(int index)
        {
            if (_hasValue[index]) 
            {
                // set null
                _hasValue[index] = false;

                // clear data (it may be hold references, unlikely but...
                _values[index] = default(TDataType);

                // signal changed
                return true;
            }

            return false;
        }

        public T GetValue<T>(int index)
        {
            return Generics.Convert<TDataType?, T>(GetValue(index));
        }

        public bool SetValue<T>(int index, T value)
        {
            return SetValue(index, Generics.Convert<T, TDataType?>(value));
        }

        public TDataType? GetValue(int index)
        {
            return _hasValue[index] ? _values[index] : (TDataType?)null;
        }

        public bool SetValue(int index, TDataType? value)
        {
            var oldValue = GetValue(index);
            if (!_equalityComparer.Equals(oldValue, value))
            {
                if (value == null)
                {
                    _hasValue[index] = false;
                    _values[index] = default(TDataType);
                }
                else
                {
                    _hasValue[index] = true;
                    _values[index] = value.Value;
                }
                Changed(oldValue, value);
                return true;
            }
            return false;
        }

        protected void Changed(TDataType? oldValue, TDataType? newValue)
        {
            // take copy of observers (for correct handling of re-entrant subscribe/unsubscribe)
            var observers = _observers;

            //  notify
            foreach (var observer in observers)
            {
                observer.Changed(this, oldValue, newValue);
            }
        }
    }
    
}
