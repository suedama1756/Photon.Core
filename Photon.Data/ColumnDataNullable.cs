using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    internal class ColumnDataNullable<TDataType> : ColumnDataBase<TDataType?> where TDataType : struct
    {
        private BitArray _hasValue;
        private readonly ColumnDataStore<TDataType> _values;
        
        public ColumnDataNullable() : this(EqualityComparer<TDataType?>.Default) 
        {
        }

        public ColumnDataNullable(IEqualityComparer<TDataType?> equalityComparer) : base(equalityComparer)
        {
            _values = new ColumnDataStore<TDataType>();
        }

        public override void Resize(int capacity, int preserve)
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

        public override int Capacity 
        {
            get { return _values.Capacity; }
        }

        protected override TDataType? GetValueRaw(int index)
        {
            return _hasValue[index] ? _values[index] : (TDataType?)null;
        }

        protected override void SetValueRaw(int index, TDataType? value)
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
        }
    }
}
