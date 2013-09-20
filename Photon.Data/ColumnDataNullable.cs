using System;
using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    internal class ColumnDataNullable<TDataType> : IColumnData<TDataType?> where TDataType : struct
    {
        private BitArray _hasValue;
        private readonly ColumnDataStore<TDataType> _values;

        public ColumnDataNullable() : this(EqualityComparer<TDataType>.Default) 
        {
        }

        public ColumnDataNullable(IEqualityComparer<TDataType> equalityComparer) 
        {
            _values = new ColumnDataStore<TDataType>(equalityComparer);  
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

        public bool IsNullable
        {
            get
            {
                return true;
            }
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
            //  if null the clear
            if (value == null) 
            {
                return Clear(index);
            }

            //  if has value then simply update
            if (_hasValue[index]) 
            {
                //  previous value was not null, simply change
                return _values.ChangeValue(index, value.Value);
            }

            // was null, set and return
            _hasValue[index] = true;
            _values[index] = value.Value;
            return true;
        }
    }
    
}
