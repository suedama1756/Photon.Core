using System;
using System.Linq;

namespace Photon.Data
{
    public class Record : IRecord
    {
        #region Fields

        internal int Handle = -1;
        private RecordSet _recordSet;

        #endregion

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
            return _recordSet.Field<T>(Handle, _recordSet.Columns[name].Ordinal);
        }
		
		public bool SetField<T>(int index, T value)
		{
			ThrowIfDetached();
			return _recordSet.Field(Handle, index, value);
		}

        public bool SetField<T>(string name, T value)
        {
            ThrowIfDetached();
            return _recordSet.Field(Handle, _recordSet.Columns[name].Ordinal, value);
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
            get { return _recordSet; }
            internal set { _recordSet = value; }
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
