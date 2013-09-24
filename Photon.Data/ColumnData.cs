using System.Collections.Generic;

namespace Photon.Data
{
    internal class ColumnData<TDataType> : ColumnDataBase<TDataType>
    {
        #region Fields

        private readonly ColumnDataStore<TDataType> _storage;

        #endregion

        public ColumnData() : this(EqualityComparer<TDataType>.Default) 
        {
        }

        public ColumnData(IEqualityComparer<TDataType> equalityComparer) : base(equalityComparer)
        {
            _storage = new ColumnDataStore<TDataType>();
        }

        public override int Capacity 
        {
            get
            {
                return _storage.Capacity;
            } 
        }

        public override void Resize(int capacity, int preserve) 
        {
            _storage.Resize(capacity, preserve);
        }

        protected override TDataType GetValueRaw(int index) 
        {
            return _storage[index];
        }

        protected override void SetValueRaw(int index, TDataType value)
        {
            _storage[index] = value;
        }
    }
}