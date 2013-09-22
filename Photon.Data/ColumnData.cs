using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal static class ColumnData 
    {
        public static IColumnData Create(Type dataType)
        {
            var type = SelectColumnDataType(dataType);
            return (IColumnData)Activator.CreateInstance(type);
        }

        private static Type SelectColumnDataType(Type dataType)
        {
            var underlyingType = Nullable.GetUnderlyingType(dataType);
            if (underlyingType != null)
            {
                return typeof(ColumnDataNullable<>)
                    .MakeGenericType(underlyingType);
            }

            return typeof(ColumnData<>).MakeGenericType(dataType);
        }
    }
    
    internal class ColumnData<TDataType> : IColumnData<TDataType>
    {
        #region Fields

        private readonly ColumnDataStore<TDataType> _storage;

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
            return _storage.IsNullable && Generics.IsNull(_storage[index]);
        }

        public void Move(int sourceIndex, int targetIndex)
        {
            SetValue(targetIndex, GetValue(sourceIndex));
        }

        public bool Clear(int index) 
        {
            return _storage.ChangeValue(index, default(TDataType));
        }

        public T GetValue<T>(int index)
        {
            return Generics.Convert<TDataType, T>(_storage[index]);
        }

        public bool SetValue<T>(int index, T value)
        {
            return _storage.ChangeValue(index, Generics.Convert<T, TDataType>(value));
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
