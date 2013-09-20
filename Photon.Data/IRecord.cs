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

        T GetValue<T>(int index);

        bool SetValue<T>(int index, T value);

        T GetValue<T>(string name);

        bool SetValue<T>(string name, T value);

        Type GetFieldType(int index);

        int GetOrdinal(string name);

        string GetName(int ordinal);

        int FieldCount { get; }
    }
}
