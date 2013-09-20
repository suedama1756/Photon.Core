using System;
using System.Linq;

namespace Photon.Data
{
    public class RecordSetRecord : IRecord
    {
        #region Fields

        internal int Handle = -1;
        private RecordSet _recordSet;

        #endregion

        public object this[int index]
		{
			get { return GetValue<object>(index); }
			set { SetValue(index, value); }
		}

        public object this[string name]
        {
            get { return GetValue<object>(name); }
            set { SetValue(name, value); }
        }
		
        public T GetValue<T>(int index)
		{
			ThrowIfDetached();
			return _recordSet.Field<T>(Handle, index);
		}

        public T GetValue<T>(string name)
        {
            ThrowIfDetached();
            return _recordSet.Field<T>(Handle, _recordSet.Columns[name].Ordinal);
        }
		
		public bool SetValue<T>(int index, T value)
		{
			ThrowIfDetached();
			return _recordSet.Field(Handle, index, value);
		}

        public bool SetValue<T>(string name, T value)
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
            return "[" + string.Join(", ", _recordSet.Columns.Select((x, i) => GetValue<string>(i))) + "]";
		}
    }
}
