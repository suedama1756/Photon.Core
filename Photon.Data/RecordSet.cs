using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Photon.Data
{
    public class RecordSet : ICollection<Record>
	{
        private int _count;
        private int _version;
        private IColumnData _records;
        private List<IColumnData> _data;
        private RecordSetColumnCollection _columns;
        private int _capacity;
        private List<int> _recordPool;

        public RecordSet(params Type[] types)
        {
            _columns = new RecordSetColumnCollection(
                this, types);

            _data = new List<IColumnData>(
                types.Select(CreateColumnData));

            _records = CreateColumnData(typeof(Record));
            _recordPool = new List<int>();
        }


        public IEnumerator<Record> GetEnumerator()
        {
            var version = _version;
            for (int sourceIndex=0, targetIndex = 0; targetIndex<_count; sourceIndex++) {
                if (_version != version) {
                    throw new InvalidOperationException("Collection has been modified.");
                }

                var record = _records.GetValue<Record>(targetIndex);
                if (record != null) {
                    targetIndex++;
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
                foreach (var item in AllData)
                {
                    item.Resize(newCapacity, Count);
                }

                _capacity = newCapacity;
            }
        }

        IEnumerable<IColumnData> AllData {
            get{
                yield return _records;
                foreach (var item in _data) {
                    yield return item;
                }
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
                
            RequireCapacity(_count + 1);

            item.Handle = Count;
            item.Store = this;

            _records.SetValue(item.Handle, item);

            _count++;
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

            // clear row, leave no references
            for (int i=0, n=_data.Count; i<n; i++) {
                _data[i].Clear(item.Handle);
            }
            _records.Clear(item.Handle);

            _recordPool.Add(item.Handle);

            // detach row
            item.Handle = -1;
            item.Store = null;
               
            // update tracking information
            _count--;
            _version++;

            // done
            return false;
        }

        public void Clear() 
        {
            _records.Resize(0, 0);
            for (int i=0, n=_columns.Count; i<n; i++) {
                _records.Resize(0, 0);
            }
        }

        internal void InsertColumn(int index, Type item)
        {
            _data.Insert(index, CreateColumnData(item));
        }

        internal void InsertColumnComplete(int index, Type item)
        {

        }

        internal void RemoveColumn(int index, Type item)
        {
            _data.RemoveAt(index);
        }

        internal void RemoveColumnComplete(int index, Type item)
        {

        }

        internal void SetColumn(int index, Type oldItem, Type newItem)
        {
            if (oldItem != newItem) 
            {
                _data[index] = CreateColumnData(newItem);
            }
        }

        internal void SetColumnComplete(int index, Type oldItem, Type newItem)
        {

        }

        internal void ClearColumns()
        {
            _data.Clear();
        }

        internal void ClearColumnsComplete()
        {

        }

        internal T Field<T>(int handle, int index)
		{
			var column = _data[index];
			return column.GetValue<T>(handle);
		}
		
		internal bool Field<T>(int handle, int index, T value)
		{
			var column = _data[index];
			return column.SetValue<T>(handle, value);
		}
      
	}
}
