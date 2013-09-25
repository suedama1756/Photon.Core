using System;
using System.Linq;

namespace Photon.Data
{
    public class Record : IRecord
    {
        #region Fields

        private int _handle = Int32.MinValue;
        private RecordSet _recordSet;

        #endregion

        internal void Attach(RecordSet recordSet, int handle)
        {
            _recordSet = recordSet;
            _handle = handle;
        }

        internal void Detach()
        {
            _handle = Int32.MinValue;
            _recordSet = null;
        }

        internal void Remove()
        {
            unchecked
            {
                _handle |= (int)0x80000000;
            }
        }

        internal int Handle
        {
            get
            {
                return _handle & 0x7FFFFFFF;
            }
        }

        public object this[int index]
		{
			get { return GetField<object>(index); }
			set { SetField(index, value); }
		}

        public object this[string name]
        {
            get { return GetField<object>(name); }
            set { SetField(name, value); }
        }
		
        public T GetField<T>(int index)
		{
			ThrowIfDetached();
			return _recordSet.Field<T>(Handle, index);
		}

        public T GetField<T>(string name)
        {
            ThrowIfDetached();
            return _recordSet.Field<T>(Handle, GetOrdinal(name));
        }
		
		public bool SetField<T>(int index, T value)
		{
			ThrowIfDetached();
			return _recordSet.Field(Handle, index, value);
		}

        public bool SetField<T>(string name, T value)
        {
            ThrowIfDetached();
            return _recordSet.Field(Handle, GetOrdinal(name), value);
        }

        Type IRecord.GetFieldType(int index)
	    {
	        return GetFieldType(index);
	    }

        protected Type GetFieldType(int index)
        {
            ThrowIfDetached();
            return _recordSet.Columns[index].DataType;
        }

        int IRecord.GetOrdinal(string name)
        {
            return GetOrdinal(name);
        }

        protected int GetOrdinal(string name)
        {
            ThrowIfDetached();
            return _recordSet.Columns[name].Ordinal;
        }

        string IRecord.GetName(int ordinal)
        {
            return GetName(ordinal);
        }

        protected string GetName(int ordinal)
        {
            ThrowIfDetached();
            return _recordSet.Columns[ordinal].Name;
        }

        public int FieldCount
	    {
	        get
	        {
	            return _recordSet.Columns.Count;
	        }
	    }

        public bool IsNull(int index)
        {
            ThrowIfDetached();
            return _recordSet.IsNull(Handle, index);
        }

        public RecordSet RecordSet
        {
            get
            {
                return _recordSet;
            }
        }

        public bool IsDetached
        {
            get { return _handle == Int32.MinValue; }
        }

        public bool IsRemoved
        {
            get { return _handle < 0; }
        }

        protected void ThrowIfDetached()
		{
			if (_recordSet == null)
			{
				throw new InvalidOperationException("The record is not attached to any storage.");
			}
		}

		public override string ToString()
		{
            if (_recordSet == null)
            {
                return string.Empty;
            }
            return "[" + string.Join(", ", _recordSet.Columns.Select((x, i) => GetField<string>(i))) + "]";
		}

        protected internal virtual void Changed<T>(int ordinal, T oldValue, T newValue)
        {
            
        }
    }
}
