using System;

using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    public class ColumnData 
    {
        public static IColumnData<TDataType> Create<TDataType>()
        {
            var dataType = typeof(TDataType);
            if (dataType.IsValueType) 
            {
                var underlyingType = Nullable.GetUnderlyingType(dataType);
                if (underlyingType != null) 
                {
                    return (IColumnData<TDataType>)Create(underlyingType, true);
                }
            }

            return new ColumnData<TDataType>();
        }

        public static IColumnData Create(Type dataType, bool requireNullable)
        {
            var type = SelectColumnDataType(dataType, requireNullable);
            return (IColumnData)Activator.CreateInstance(type);
        }

        private static Type SelectColumnDataType(Type dataType, bool requireNullable) 
        {
            if (requireNullable && dataType.IsValueType)
            {
                dataType = Nullable.GetUnderlyingType(dataType) ?? dataType;

                return typeof(NullableColumnData<>)
                    .MakeGenericType(dataType);
            }

            return typeof(ColumnData<>).MakeGenericType(dataType);
        }
    }

    internal class NullableColumnData<TDataType> : IColumnData<TDataType?> where TDataType : struct
    {
        private BitArray _hasValue;
        private ColumnStorage<TDataType> _values;

        public NullableColumnData() : this(EqualityComparer<TDataType>.Default) 
        {

        }

        public NullableColumnData(IEqualityComparer<TDataType> equalityComparer) 
        {
            _values = new ColumnStorage<TDataType>(equalityComparer);  
        }

        public void Resize(int capacity, int count)
        {
            _values.Resize(capacity, count);
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
            get
            {
                return _values.Capacity;
            }
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
            return Converter.Convert<TDataType?, T>(GetValue(index));
        }

        public bool SetValue<T>(int index, T value)
        {
            return SetValue(index, Converter.Convert<T, TDataType?>(value));
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

    internal class ColumnData<TDataType> : IColumnData<TDataType>
    {
        #region Fields

        private ColumnStorage<TDataType> _storage;

        #endregion

        public ColumnData() : this(EqualityComparer<TDataType>.Default) 
        {
        }

        public ColumnData(IEqualityComparer<TDataType> equalityComparer) 
        {
            _storage = new ColumnStorage<TDataType>(equalityComparer);
        }

        public int Capacity 
        {
            get
            {
                return _storage.Capacity;
            }
        }

        public void Resize(int capacity, int count) 
        {
            _storage.Resize(capacity, count);
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
            return _storage.IsNullable && Converter.IsNull(_storage[index]);
        }

        public bool Clear(int index) 
        {
            return _storage.ChangeValue(index, default(TDataType));
        }

        public T GetValue<T>(int index)
        {
            return Converter.Convert<TDataType, T>(_storage[index]);
        }

        public bool SetValue<T>(int index, T value)
        {
            return _storage.ChangeValue(index, Converter.Convert<T, TDataType>(value));
        }

        public TDataType GetValue(int index) 
        {
            return _storage[index];
        }

        public bool SetValue(int index, TDataType value) 
        {
            return _storage.ChangeValue(index, value);
        }
    }

    internal class ColumnStorage<TDataType> 
	{
		private IEqualityComparer<TDataType> _equalityComparer;
        private TDataType[] Data;
        private bool _isNullable;

        internal ColumnStorage(IEqualityComparer<TDataType> equalityComparer)
        {
            if (equalityComparer == null)
            {
                throw new ArgumentNullException("equalityComparer");
            }
            _equalityComparer = equalityComparer;

            // determine whether the type is nullable, e.g. if its a ref type, or nullable<>
            _isNullable = !typeof(TDataType).IsValueType ||
                Nullable.GetUnderlyingType(typeof(TDataType)) != null;
        }

        internal void Resize(int capacity, int count)
        {
            // allocate data
            var newData = new TDataType[capacity];
            var oldData = Data;

            if (oldData != null)
            {
                Array.Copy(oldData, newData, count);
            }
            Data = newData;
        }

        internal int Capacity
        {
            get 
            {
                return Data != null ? Data.Length : 0;
            }
        }

        internal bool IsNullable 
        {
            get
            {
                return _isNullable;
            }
        }

        internal bool ChangeValue(int index, TDataType value)
        {
            var oldValue = Data[index];
            if (!_equalityComparer.Equals(oldValue, value))
            {
                Data[index] = value;
                return true;
            }
            return false;
        }

        internal TDataType this[int index] 
        {
            get
            {
                return Data[index];
            }
            set
            {
                Data[index] = value;
            }
        }
	}
}
