using System;

namespace Photon.Data
{
    public interface IRecord 
    {
        object this [int index]
        {
            get; set;
        }    

        T Field<T>(int index);

        void Field<T>(int index, T value);

        Type FieldType(int index);

        int FieldCount { get; }
    }
	
}
