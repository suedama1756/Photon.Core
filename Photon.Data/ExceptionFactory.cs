using System;
using System.Collections.Generic;

namespace Photon.Data
{
    internal static class ExceptionFactory
    {
        public static ArgumentException RecordSetColumnAlreadyOwnedByThisRecordSet(string paramName)
        {
            return new ArgumentException("Column already belongs to this record set.", paramName);
        }

        public static ArgumentException RecordSetColumnAlreadyOwnerByAnotherRecordSet(string paramName)
        {
            return new ArgumentException("Column already belongs to another record set.", paramName);
        }

        public static ArgumentException RecordSetColumnDuplicateName(string paramName)
        {
            return new ArgumentException("Column with name already exists.", paramName);
        }

        public static KeyNotFoundException RecordSetColumnNotFound()
        {
            return new KeyNotFoundException();
        }
    }
}