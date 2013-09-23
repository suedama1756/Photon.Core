using System;

namespace Photon.Data
{
    public interface IRecord 
    {
        object this [int index]
        {
            get; set;
        }

        object this[string name]
        {
            get;
            set;
        }    

        T GetField<T>(int index);

        bool SetField<T>(int index, T value);

        T GetField<T>(string name);

        bool SetField<T>(string name, T value);

        Type GetFieldType(int index);

        int GetOrdinal(string name);

        string GetName(int ordinal);

        int FieldCount { get; }
        
        bool IsNull(int index);
    }
}
