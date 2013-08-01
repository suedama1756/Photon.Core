using System;
using NUnit.Framework;
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
    /*
     * How do we create a structure that we can inherit from where we don't care about the column storage?
     * 
     * Simple, provide a mechanism for setting the storage, or stick with your origin plan, 
     * anything else is added on top.
     * 
     * This should surely lead to, so the column collection is really just a type collection, and we wrap that with 
     * the concept of string indexing etc.
     * 
     * So how would the descendent provide the same thing we are after here?
     * 
     * Well, it would require a base collection to work right?, but, we can wrap that collection 
     * in a list wrapper, or we can provide the collection to the base.
     * 
     * But in fact, all we want to do really is
     */


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
            ColumnAccessors = new List<RecordSetColumn>(types.Select(x => RecordSetColumn.Create(x)));
		}

		public RecordSetColumnCollection Columns { get; private set; }

        public List<RecordSetColumn> ColumnAccessors
        {
            get;
            private set;
        }

        public virtual void InsertColumn(int index, Type item)
        {
        }

        public void InsertColumnComplete(int index, Type item)
        {
            throw new NotImplementedException();
        }

        public void RemoveColumn(int index, Type item)
        {
            throw new NotImplementedException();
        }

        public void RemoveColumnComplete(int index, Type item)
        {
            throw new NotImplementedException();
        }

        public void ClearColumns()
        {
            throw new NotImplementedException();
        }

        public void ClearColumnsComplete()
        {
            throw new NotImplementedException();
        }
		
		public Record Add()
		{
			if (rows.Count == size)
			{
                var newSize = size + 16;
                foreach (var columnAccessor in ColumnAccessors)
                {
                    columnAccessor.SetCapacity(newSize);
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
			var column = ColumnAccessors[index];
			return column.GetValue<T>(handle);
		}
		
		public bool SetField<T>(int handle, int index, T value)
		{
			var column = ColumnAccessors[index];
			return column.SetValue<T>(handle, value);
		}
	}


}
