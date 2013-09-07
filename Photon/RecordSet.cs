using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Photon.Data
{
    public  class RecordSetColumnCollection : Collection<Type> 
    {
        public RecordSetColumnCollection(RecordSet owner, Type[] types) : base(types)
        {
            Owner = owner;
        }

        protected RecordSet Owner
        {
            get;
            private set;
        }

        protected override void InsertItem(int index, Type item)
        {
            Owner.InsertColumn(index, item);
            base.InsertItem(index, item);
            Owner.InsertColumnComplete(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            Owner.RemoveColumn(index, item);
            base.RemoveItem(index);
            Owner.RemoveColumnComplete(index, item);
        }

        protected override void ClearItems()
        {
            Owner.ClearColumns();
            base.ClearItems();
            Owner.ClearColumnsComplete();
        }

        protected override void SetItem(int index, Type item)
        {
            var oldItem = Items[index];
            Owner.RemoveColumn(index, item);
            base.RemoveItem(index);
            Owner.RemoveColumnComplete(index, item);
        }
    }

	public class RecordSet
	{
		private List<Record> rows = new List<Record>();
		private int size;

		public RecordSet(params Type[] types)
		{
            Columns = new RecordSetColumnCollection(this, types);
            ColumnDatas = new List<IColumnData>(
                types.Select(x => ColumnData.Create(x, false)));
		}

		public RecordSetColumnCollection Columns 
        {
            get; 
            private set;
        }

        public List<IColumnData> ColumnDatas
        {
            get;
            private set;
        }

        public int Capacity
        {
            get;
            protected set;
        }

        public void InsertColumn(int index, Type item)
        {
            var columnData = ColumnData.Create(item, false);
            columnData.Resize(Capacity, 0);
            ColumnDatas.Insert(index, columnData);
        }

        public void InsertColumnComplete(int index, Type item)
        {

        }

        public void RemoveColumn(int index, Type item)
        {
            ColumnDatas.RemoveAt(index);
        }

        public void RemoveColumnComplete(int index, Type item)
        {

        }

        public void ClearColumns()
        {
            ColumnDatas.Clear();
        }

        public void ClearColumnsComplete()
        {

        }
		
		public Record Add()
		{
			if (rows.Count == size)
			{
                var newSize = size + 16;
                foreach (var columnData in ColumnDatas)
                {
                    columnData.Resize(newSize, rows.Count);
                }
                size = newSize;
			}
			
			var result = new Record
			{
				Handle = rows.Count,
				RecordSet = this
			};
			rows.Add(result);
			return result;
		}
		
		public T GetField<T>(int handle, int index)
		{
			var column = ColumnDatas[index];
			return column.GetValue<T>(handle);
		}
		
		public bool SetField<T>(int handle, int index, T value)
		{
			var column = ColumnDatas[index];
			return column.SetValue<T>(handle, value);
		}
	}
}
