using System;
using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class RecordSet : ICollection<RecordSetRecord>
    {
        #region Fields

        private int _count;
        private int _version;
        private readonly IColumnData<RecordSetRecord> _records;
        private readonly List<IColumnData> _columnsData;
        private readonly RecordSetColumnCollection _columns;
        private int _capacity;
        private readonly List<int> _recordsPool;
        private int _lastRecordIndex = -1;

        #endregion

        public RecordSet()
        {
            _columns = new RecordSetColumnCollection(this);
            _columnsData = new List<IColumnData>();
            _records = (IColumnData<RecordSetRecord>)CreateColumnData(typeof(RecordSetRecord));
            _recordsPool = new List<int>();
        }
        
        public IEnumerator<RecordSetRecord> GetEnumerator()
        {
            var version = _version;
            for (int sourceIndex=0, numberFound = 0; numberFound<_count && sourceIndex < _capacity; sourceIndex++) {
                if (_version != version) {
                    throw new InvalidOperationException("Collection has been modified.");
                }

                var record = _records.GetValue<RecordSetRecord>(sourceIndex);
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

        public bool Contains(RecordSetRecord item)
        {
            return item != null && item.RecordSet == this;
        }

        public int Count 
        {
            get
            {
                return _count;
            }
        }

        public void CopyTo(RecordSetRecord[] array, int arrayIndex)
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
                    Resize(value);
                }
                else
                {
                    CompactTo(value);
                    Resize(value);
                }
            }
        }

        public void Compact()
        {
            CompactTo(Count);
        }

        private void CompactTo(int requiredCapacity)
        {
            var index = _lastRecordIndex;
            while (index >= 0 && index > requiredCapacity)
            {
                //  sort the record pool
                var comparer = Comparer<int>.Default;
                _recordsPool.Sort((x, y) => comparer.Compare(x, y) * -1);

                //  Cleared items do not need to be added back into the
                //  pool as they are removed from the end and new items are 
                //  added after "last index" if the pool is exhausted.
                var handle = ReserveRecordHandle();

                MoveRecord(index, handle);

                // locate next item
                do
                {
                    index--;
                } while (index >= 0 && _records.GetValue(index) == null);
            }

            _lastRecordIndex = index;
        }

        private void MoveRecord(int fromIndex, int toIndex)
        {
            //  move record data
            var source = _records.GetValue(fromIndex);
            foreach (var data in _columnsData)
            {
                data.Move(source.Handle, toIndex);
                data.Clear(source.Handle);
            }

            //  move record
            _records.Move(source.Handle, toIndex);
            source.Handle = toIndex;
        }

        private IColumnData CreateColumnData(Type type)
        {
            var columnData = ColumnData.Create(type);
            columnData.Resize(Capacity, 0);
            return columnData;
        }
        
        private void RequireCapacity(int capacity)
        {
            if (_records.Capacity < capacity)
            {
                var newCapacity = Math.Max(Capacity * 2, 4);
                Resize(newCapacity);
            }
        }

        private void Resize(int newCapacity)
        {
            var preserve = _lastRecordIndex + 1;
            foreach (var item in _columnsData)
            {
                item.Resize(newCapacity, preserve);
            }
            _records.Resize(newCapacity, preserve);

            _capacity = newCapacity;
        }

        public void Add(RecordSetRecord item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.RecordSet != null && item.RecordSet != this)
            {
                throw new ArgumentException("The item already belongs to another record set.", "item");
            }
                
            if (item.Handle != -1)
            {
                throw new ArgumentException("The item already belongs to this record set.", "item");
            }
                
            //  ensure we have enough capacity
            RequireCapacity(_count + 1);

            AddRecord(item);

            _count++;
            _version++;
        }

        private void AddRecord(RecordSetRecord item)
        {
            //  get next handle
            var handle = ReserveRecordHandle();
            if (handle > _lastRecordIndex)
            {
                _lastRecordIndex = handle;
            }

            AttachRecord(item, handle);

            // save handle
            _records.SetValue(handle, item);
        }

        private void AttachRecord(RecordSetRecord item, int handle)
        {
            //  attach the item
            item.Handle = handle;
            item.RecordSet = this;
        }

        private int RemoveRecord(RecordSetRecord item)
        {
            var handle = item.Handle;
            
            // clear row, leave no references
            for (int i = 0, n = _columnsData.Count; i < n; i++)
            {
                _columnsData[i].Clear(item.Handle);
            }
            _records.Clear(item.Handle);

            DetachRecord(item);

            if (handle == _lastRecordIndex)
            {
                do
                {
                    handle--;
                } while (handle >= 0 && _records.GetValue(handle) == null);

                _lastRecordIndex = handle;
            }

            return handle;
        }

        private static void DetachRecord(RecordSetRecord item)
        {
            item.Handle = -1;
            item.RecordSet = null;
        }

        private int ReserveRecordHandle()
        {
            var handle = _lastRecordIndex + 1;
            var poolLength = _recordsPool.Count;
            if (poolLength > 0)
            {
                poolLength--;
                handle = _recordsPool[poolLength];
                _recordsPool.RemoveAt(poolLength);
            }
            return handle;
        }

        public bool Remove(RecordSetRecord item) 
        {
            if (item == null) 
            {
                throw new ArgumentNullException("item");
            }

            if (item.RecordSet != this) 
            {
                return false;
            }

            _recordsPool.Add(RemoveRecord(item));

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
                DetachRecord(item);
            }

            // clear storage, reset pool
            _records.Resize(0, 0);
            for (int i=0, n=_columns.Count; i<n; i++) {
                _columnsData[i].Resize(0, 0);
            }
            _recordsPool.Clear();
            _recordsPool.Capacity = 0;
        }

        private void UpdateColumnOrdinals(int index)
        {
            var n = Columns.Count;
            for (; index < n; index++)
            {
                Columns[index].Ordinal = index;
            }
        }

        internal void ColumnInserted(int index, RecordSetColumn item)
        {
            _columnsData.Insert(index, CreateColumnData(item.DataType));
            AttachColumn(item, index);
        }

        internal void ColumnRemoved(int index, RecordSetColumn item)
        {
            _columnsData.RemoveAt(index);
            DetachColumn(item);
        }

        internal void ColumnSet(int index, RecordSetColumn oldItem, RecordSetColumn newItem)
        {
            if (oldItem != newItem)
            {
                _columnsData[index] = CreateColumnData(newItem.DataType);
            }
            DetachColumn(oldItem);
            AttachColumn(newItem, index);
        }

        internal void ColumnsCleared(IEnumerable<RecordSetColumn> columns)
        {
            _columnsData.Clear();
            foreach (var column in columns)
            {
                DetachColumn(column);
            }
        }

        private void AttachColumn(RecordSetColumn column, int oridinal)
        {
            column.RecordSet = this;
            column.Ordinal = oridinal;
            if (oridinal < Columns.Count - 1)
            {
                UpdateColumnOrdinals(oridinal + 1);
            }
        }

        private void DetachColumn(RecordSetColumn column)
        {
            if (column.Ordinal < Columns.Count)
            {
                UpdateColumnOrdinals(column.Ordinal);
            }
            column.RecordSet = null;
            column.Ordinal = -1;
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

        internal bool IsNull(int handle, int index)
        {
            var column = _columnsData[index];
            return column.IsNull(handle);
        }
    }
}
