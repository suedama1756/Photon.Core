using System;

using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    internal class ColumnDataStore<TDataType> 
	{
		private IEqualityComparer<TDataType> _equalityComparer;
        private TDataType[] Data;
        private bool _isNullable;

        internal ColumnDataStore(IEqualityComparer<TDataType> equalityComparer)
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
