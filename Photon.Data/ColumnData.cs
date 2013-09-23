using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal class ColumnData<TDataType> : IColumnData<TDataType>
    {
        #region Fields

        private readonly ColumnDataStore<TDataType> _storage;
        private IColumnDataObserver[] _observers;
        private readonly IEqualityComparer<TDataType> _equalityComparer;

        #endregion

        public ColumnData() : this(EqualityComparer<TDataType>.Default) 
        {
        }

        public ColumnData(IEqualityComparer<TDataType> equalityComparer)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException("equalityComparer");
            }

            _equalityComparer = equalityComparer;
            _storage = new ColumnDataStore<TDataType>();
        }

        public int Capacity 
        {
            get
            {
                return _storage.Capacity;
            } 
        }

        public void Resize(int capacity, int preserve) 
        {
            _storage.Resize(capacity, preserve);
        }

        public Type DataType
        {
            get
            {
                return typeof(TDataType);
            }
        }

        public bool IsNullable
        {
            get 
            {
                return _storage.IsNullable;
            }
        }

        public bool IsNull(int index) 
        {
            return IsNullable && Generics.IsNull(_storage[index]);
        }

        public void Move(int sourceIndex, int targetIndex)
        {
            SetValue(targetIndex, GetValue(sourceIndex));
        }

        public void Subscribe(IColumnDataObserver observer)
        {
            _observers = Arrays.Concat(_observers, observer);
        }

        public bool Clear(int index) 
        {
            return SetValue(index, default(TDataType));
        }

        public T GetValue<T>(int index)
        {
            return Generics.Convert<TDataType, T>(GetValue(index));
        }

        public bool SetValue<T>(int index, T value)
        {
            return SetValue(index, Generics.Convert<T, TDataType>(value));
        }

        public TDataType GetValue(int index) 
        {
            return _storage[index];
        }

        public bool SetValue(int index, TDataType value)
        {
            var oldValue = GetValue(index);
            if (!_equalityComparer.Equals(oldValue, value))
            {
                _storage[index] = value;
                Changed(oldValue, value);
                return true;
            }
            return false;
        }

        protected void Changed(TDataType oldValue, TDataType newValue)
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