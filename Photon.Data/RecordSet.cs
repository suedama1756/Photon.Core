using System;
using System.Collections.Generic;
using System.Collections;

namespace Photon.Data
{
    using System.Collections.Specialized;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class RecordSet : ICollection<Record>, INotifyCollectionChanged
    {
        #region Fields

        private int _count;
        private int _version;
        private readonly IColumnData<Record> _records;
        private readonly List<IColumnData> _columnsData;
        private readonly RecordSetColumnCollection _columns;
        private int _capacity;
        private readonly List<int> _recordsPool;
        private int _lastRecordIndex = -1;
        private IRecordObserver[] _observers;
        
        #endregion

        private class ColumnDataObserver : IColumnDataObserver
        {
            #region Fields

            private readonly RecordSet _recordSet;
            private readonly RecordSetColumn _column;

            #endregion

            public ColumnDataObserver(RecordSet recordSet, RecordSetColumn column)
            {
                _recordSet = recordSet;
                _column = column;
            }

            public void Changed<T>(IColumnData data, int index, T oldValue, T newValue)
            {
                _recordSet.FieldChanged(index, _column.Ordinal, oldValue, newValue);
            }
        }

        public RecordSet()
        {
            _columns = new RecordSetColumnCollection(this);
            _columnsData = new List<IColumnData>();
            _records = (IColumnData<Record>)CreateColumnData(typeof(Record));
            _recordsPool = new List<int>();
        }
        
        public IEnumerator<Record> GetEnumerator()
        {
            var version = _version;
            for (int sourceIndex=0, numberFound = 0; numberFound<_count && sourceIndex < _capacity; sourceIndex++) {
                if (_version != version) {
                    throw new InvalidOperationException("Collection has been modified.");
                }

                var record = _records.GetValue<Record>(sourceIndex);
                if (record != null && !record.IsRemoved) {
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
            return item != null && item.RecordSet == this;
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
            source.Attach(this, toIndex);
        }

        private IColumnData CreateColumnData(RecordSetColumn column)
        {
            var result = CreateColumnData(column.DataType);
            result.Subscribe(new ColumnDataObserver(this, column));
            return result;
        }

        private IColumnData CreateColumnData(Type type)
        {
            var columnData = ColumnDataFactory.Create(type);
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

        public void Add(Record item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.RecordSet != null && item.RecordSet != this)
            {
                throw new ArgumentException("The item already belongs to another record set.", "item");
            }
                
            if (!item.IsDetached)
            {
                throw new ArgumentException("The item already belongs to this record set.", "item");
            }
                
            //  ensure we have enough capacity
            RequireCapacity(_count + 1);

            AddRecord(item);

            _count++;
            _version++;

            OnAdded(item);
        }
        
        private void AddRecord(Record item)
        {
            //  get next handle
            var handle = ReserveRecordHandle();
            if (handle > _lastRecordIndex)
            {
                _lastRecordIndex = handle;
            }

            item.Attach(this, handle);
            
            // save handle
            _records.SetValue(handle, item);
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

        public bool Remove(Record item) 
        {
            if (item == null) 
            {
                throw new ArgumentNullException("item");
            }

            if (item.RecordSet != this || item.IsRemoved) 
            {
                return false;
            }

            item.Remove();

            // update tracking information
            _count--;
            _version++;
            
            try
            {
                OnRemoved(item);
            }
            finally
            {
                DetachRecord(item);
            }

            // done
            return true;
        }

        private void DetachRecord(Record item)
        {
            var handle = item.Handle;
            for (int i = 0, n = _columnsData.Count; i < n; i++)
            {
                _columnsData[i].Clear(item.Handle);
            }
            _records.Clear(item.Handle);

            item.Detach();

            if (handle == _lastRecordIndex)
            {
                do
                {
                    handle--;
                } while (handle >= 0 && _records.GetValue(handle) == null);

                _lastRecordIndex = handle;
            }

            _recordsPool.Add(handle);
        }

        public void Clear() 
        {
            // TODO: No notification on clear

            //  detach all
            foreach (var item in this)
            {
                item.Detach();
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
                var column = Columns[index];
                if (column.Ordinal == index)
                {
                    return;
                }
                column.Ordinal = index;
            }
        }

        internal void ColumnInserted(int index, RecordSetColumn item)
        {
            _columnsData.Insert(index, CreateColumnData(item));
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
                _columnsData[index] = CreateColumnData(newItem);
                
                DetachColumn(oldItem);
                AttachColumn(newItem, index);
            }
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
            // The only way to get column change information is to 
            // do it in the column, every other access point is 
            // not neccessarily working with the underlying type.
            // The interesting thing here is that it would seem the best place to 
            // store oridinals would now be in the columns data's this 
            // remove the constraint that columns have to be owned by the recordset
            // wich always seemed a bit pointles. However, we would still need a 
            // way to getOriginal (options....), column, the easiest is to associate the 
            // columnsdata with a column, that way when it changes it can notify through
            // 
			return column.SetValue(handle, value);
		}

        internal bool IsNull(int handle, int index)
        {
            var column = _columnsData[index];
            return column.IsNull(handle);
        }

        public void Subscribe(IRecordObserver observer)
        {
            _observers = Arrays.Concat(_observers, observer);
        }

        private void FieldChanged<T>(int handle, int index, T oldValue, T newValue)
        {
            var observers = _observers;
            if (observers == null)
            {
                return;
            }

            var record = _records.GetValue(handle);
            record.Changed(index, oldValue, newValue);
            foreach (var observer in observers)
            {
                
                observer.Changed(_records.GetValue(handle), index, oldValue, newValue);
            }
        }

        private void OnRemoved(Record item)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
        }

        private void OnAdded(Record item)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, Record item)
        {
            var collectionChanged = CollectionChanged;
            if (collectionChanged != null)
            {
                collectionChanged(this, new NotifyCollectionChangedEventArgs(
                    action, item));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Unsubscribe(IRecordObserver observer)
        {
            _observers = Arrays.Remove(_observers, observer);
        }
    }

    
}
