using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Photon.Data
{
    public class RecordSet : ICollection<Record>
    {
        #region Fields

        private int _count;
        private int _version;
        private readonly IColumnData<Record> _records;
        private readonly List<IColumnData> _columnsData;
        private readonly RecordSetColumnCollection _columns;
        private int _capacity;
        private readonly List<int> _recordPool;

        #endregion

        public RecordSet(params Type[] types)
        {
            types = types ?? Type.EmptyTypes;

            _columns = new RecordSetColumnCollection(this, types);
            _columnsData = new List<IColumnData>(types.Select(CreateColumnData));
            _records = (IColumnData<Record>)CreateColumnData(typeof(Record));
            _recordPool = new List<int>();
        }
        
        public IEnumerator<Record> GetEnumerator()
        {
            var version = _version;
            for (int sourceIndex=0, numberFound = 0; numberFound<_count && sourceIndex < _capacity; sourceIndex++) {
                if (_version != version) {
                    throw new InvalidOperationException("Collection has been modified.");
                }

                var record = _records.GetValue<Record>(sourceIndex);
                if (record != null) {
                    numberFound++;
                    yield return record;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(Record item)
        {
            return item != null && item.Store == this;
        }

        public int Count 
        {
            get
            {
                return _count;
            }
        }

        public void CopyTo(Record[] array, int arrayIndex)
        {

        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

		public RecordSetColumnCollection Columns 
        {
            get  
            {
                return _columns;
            }
        }

        public int Capacity
        {
            get 
            {
                return _capacity;
            }
            set
            {
                if (value < _count)
                {
                    throw new ArgumentOutOfRangeException("value", "Value too small to contain current items.");
                }
                if (value > _capacity)
                {
                    RequireCapacity(value);
                }
                else
                {
                    Compact();
                }
            }
        }

        public void Compact()
        {
            throw new NotSupportedException("Compact:TODO");
        }

        private IColumnData CreateColumnData(Type type)
        {
            var columnData = ColumnData.Create(type, false);
            columnData.Resize(Capacity, 0);
            return columnData;
        }
        
        private void RequireCapacity(int capacity)
        {
            if (_records.Capacity < capacity)
            {
                var newCapacity = Capacity + 16;
                foreach (var item in _columnsData)
                {
                    item.Resize(newCapacity, Count);
                }
                _records.Resize(newCapacity, Count);

                _capacity = newCapacity;
            }
        }
        
        public void Add(Record item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.Store != null && item.Store != this)
            {
                throw new ArgumentException("The item already belongs to another record set.", "item");
            }
                
            if (item.Handle != -1)
            {
                throw new ArgumentException("The item already belongs to this record set.", "item");
            }
                
            //  ensure we have enough capacity
            RequireCapacity(_count + 1);

            Attach(item);

            _count++;
            _version++;
        }

        private void Attach(Record item)
        {
            //  get next handle
            var handle = ReserveHandle();

            //  attach the item
            item.Handle = handle;
            item.Store = this;

            // save handle
            _records.SetValue(handle, item);
        }

        private int Detach(Record item)
        {
            var handle = item.Handle;

            // clear row, leave no references
            for (int i = 0, n = _columnsData.Count; i < n; i++)
            {
                _columnsData[i].Clear(item.Handle);
            }
            _records.Clear(item.Handle);

            // detach row
            item.Handle = -1;
            item.Store = null;

            return handle;
        }

        private int ReserveHandle()
        {
            var handle = _count;
            var poolLength = _recordPool.Count;
            if (poolLength > 0)
            {
                poolLength--;
                handle = _recordPool[poolLength];
                _recordPool.RemoveAt(poolLength);
            }
            return handle;
        }

        public bool Remove(Record item) 
        {
            if (item == null) 
            {
                throw new ArgumentNullException("item");
            }

            if (item.Store != this) 
            {
                return false;
            }

            _recordPool.Add(Detach(item));

            // update tracking information
            _count--;
            _version++;

            // done
            return false;
        }

        public void Clear() 
        {
            //  detach all
            foreach (var item in this)
            {
                item.Handle = -1;
                item.Store = null;
            }

            // clear storage, reset pool
            _records.Resize(0, 0);
            for (int i=0, n=_columns.Count; i<n; i++) {
                _columnsData[i].Resize(0, 0);
            }
            _recordPool.Clear();
            _recordPool.Capacity = 0;
        }

        internal void InsertColumn(int index, Type item)
        {
            _columnsData.Insert(index, CreateColumnData(item));
        }

        internal void InsertColumnComplete(int index, Type item)
        {

        }

        internal void RemoveColumn(int index, Type item)
        {
            _columnsData.RemoveAt(index);
        }

        internal void RemoveColumnComplete(int index, Type item)
        {

        }

        internal void SetColumn(int index, Type oldItem, Type newItem)
        {
            if (oldItem != newItem) 
            {
                _columnsData[index] = CreateColumnData(newItem);
            }
        }

        internal void SetColumnComplete(int index, Type oldItem, Type newItem)
        {

        }

        internal void ClearColumns()
        {
            _columnsData.Clear();
        }

        internal void ClearColumnsComplete()
        {

        }

        internal T Field<T>(int handle, int index)
		{
			var column = _columnsData[index];
			return column.GetValue<T>(handle);
		}
		
		internal bool Field<T>(int handle, int index, T value)
		{
			var column = _columnsData[index];
			return column.SetValue<T>(handle, value);
		}
      
	}
}
