using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Photon.Data
{
    using System.Collections.Specialized;

    public class RecordSetColumnCollection  : Collection<RecordSetColumn>, INotifyCollectionChanged
    {
        private readonly Dictionary<string, RecordSetColumn> _columnMap = new Dictionary<string, RecordSetColumn>();

        internal RecordSetColumnCollection (RecordSet owner) 
        {
            Owner = owner;
        }
        
        protected RecordSet Owner
        {
            get;
            private set;
        }
        
        protected override void InsertItem(int index, RecordSetColumn item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.RecordSet == Owner)
            {
                throw RecordSetExceptions.ColumnAlreadyOwnedByThisRecordSet("item");
            }

            if (item.RecordSet != null)
            {
                throw RecordSetExceptions.ColumnAlreadyOwnerByAnotherRecordSet("item");
            }

            if (_columnMap.ContainsKey(item.Name))
            {
                throw RecordSetExceptions.ColumnDuplicateName("item");
            }

            base.InsertItem(index, item);
            _columnMap[item.Name] = item;
            Owner.ColumnInserted(index, item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            base.RemoveItem(index);
            _columnMap.Remove(item.Name);
            Owner.ColumnRemoved(index, item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        protected override void ClearItems()
        {
            var columns = this.ToArray();
            base.ClearItems();
            _columnMap.Clear();
            Owner.ColumnsCleared(columns);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void SetItem(int index, RecordSetColumn item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            
            if (item.RecordSet == Owner)
            {
                throw RecordSetExceptions.ColumnAlreadyOwnedByThisRecordSet("item");
            }

            if (item.RecordSet != null)
            {
                throw RecordSetExceptions.ColumnAlreadyOwnerByAnotherRecordSet("item");
            }

            var oldItem = Items[index];
            if (oldItem.Name != item.Name && _columnMap.ContainsKey(item.Name))
            {
                throw RecordSetExceptions.ColumnDuplicateName("item");
            }
            
            //  set new item
            base.SetItem(index, item);
            
            //  re-index by name
            if (oldItem.Name != item.Name)
            {
                _columnMap.Remove(oldItem.Name);    
            }
            _columnMap[item.Name] = item;

            //  notify
            Owner.ColumnSet(index, oldItem, item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, 
                item, oldItem, index));
        }

        public RecordSetColumn this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                var result = Find(name);
                if (result == null)
                {
                    throw RecordSetExceptions.ColumnNotFound();
                }
                return result;
            }
        }

        public RecordSetColumn Find(string name)
        {
            RecordSetColumn result;
            _columnMap.TryGetValue(name, out result);
            return result;
        }

        public void AddRange(IEnumerable<RecordSetColumn> columns)
        {
            if (columns == null)
            {
                throw new ArgumentNullException("columns");
            }

            foreach (var column in columns)
            {
                Add(column);
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var collectionChanged = CollectionChanged;
            if (collectionChanged != null)
            {
                collectionChanged(this, args);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
