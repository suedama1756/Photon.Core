using System;

namespace Photon.Data
{
    public interface IColumn
    {
        string Name { get; }

        Type DataType { get; }

        int Ordinal { get; }
    }
}