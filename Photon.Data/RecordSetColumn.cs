using System;

namespace Photon.Data
{
    public class RecordSetColumn : IColumn
    {
        public RecordSetColumn(Type dataType) : this(Guid.NewGuid().ToString("N"), dataType)
        {
        }

        public RecordSetColumn(string name, Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException("dataType");
            }
            

            DataType = dataType;
            Name = name;
            Ordinal = -1;
        }

        public string Name { get; private set; }

        public Type DataType { get; private set; }
        
        public int Ordinal { get; internal set; }

        public RecordSet RecordSet { get; internal set; }
    }
}