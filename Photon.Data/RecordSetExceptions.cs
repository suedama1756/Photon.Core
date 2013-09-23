using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal static class RecordSetExceptions
    {
        public static ArgumentException ColumnAlreadyOwnedByThisRecordSet(string paramName)
        {
            return new ArgumentException("Column already belongs to this record set.", paramName);
        }

        public static ArgumentException ColumnAlreadyOwnerByAnotherRecordSet(string paramName)
        {
            return new ArgumentException("Column already belongs to another record set.", paramName);
        }

        public static ArgumentException ColumnDuplicateName(string paramName)
        {
            return new ArgumentException("Column with name already exists.", paramName);
        }

        public static KeyNotFoundException ColumnNotFound()
        {
            return new KeyNotFoundException();
        }
    }
}