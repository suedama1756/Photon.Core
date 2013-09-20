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

                return typeof(ColumnDataNullable<>)
                    .MakeGenericType(dataType);
            }

            return typeof(ColumnData<>).MakeGenericType(dataType);
        }
    }


    internal class ColumnData<TDataType> : IColumnData<TDataType>
    {
        #region Fields

        private ColumnDataStore<TDataType> _storage;

        #endregion

        public ColumnData() : this(EqualityComparer<TDataType>.Default) 
        {
        }

        public ColumnData(IEqualityComparer<TDataType> equalityComparer) 
        {
            _storage = new ColumnDataStore<TDataType>(equalityComparer);
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
    
}
