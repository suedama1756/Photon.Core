using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Photon.Data
{
    public  class RecordSetColumnCollection  : Collection<Type> 
    {
        internal RecordSetColumnCollection (RecordSet owner, Type[] types) : base(new List<Type>(types))
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
            Owner.SetColumn(index, oldItem, item);
            base.RemoveItem(index);
            Owner.SetColumnComplete(index, oldItem, item);
        }
    }
	
}
