using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal class ColumnDataStore<TDataType>
    {
        #region Fields

        private readonly IEqualityComparer<TDataType> _equalityComparer;
        private TDataType[] _data;
        private bool _isNullable;

        #endregion

        public ColumnDataStore()
        {
            // determine whether the type is nullable, e.g. if its a ref type, or nullable<>
            _isNullable = !typeof(TDataType).IsValueType ||
                Nullable.GetUnderlyingType(typeof(TDataType)) != null;
        }

        public void Resize(int capacity, int preserveCount)
        {
            // allocate data
            var newData = new TDataType[capacity];
            var oldData = _data;

            if (oldData != null)
            {
                Array.Copy(oldData, newData, preserveCount);
            }
            _data = newData;
        }

        public int Capacity
        {
            get 
            {
                return _data != null ? _data.Length : 0;
            } 
            set
            {
                Resize(value, value);
            }
        }

        public bool IsNullable
        {
            get { return _isNullable; }
        }
        
        public TDataType this[int index] 
        {
            get
            {
                return _data[index];
            }
            set
            {
                _data[index] = value;
            }
        }
	}
}
