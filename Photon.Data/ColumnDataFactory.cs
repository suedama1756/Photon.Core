using System;

namespace Photon.Data
{
    internal static class ColumnDataFactory 
    {
        public static IColumnData Create(Type dataType)
        {
            var type = SelectColumnDataType(dataType);
            return (IColumnData)Activator.CreateInstance(type);
        }

        private static Type SelectColumnDataType(Type dataType)
        {
            var underlyingType = Nullable.GetUnderlyingType(dataType);
            if (underlyingType != null)
            {
                return typeof(ColumnDataNullable<>)
                    .MakeGenericType(underlyingType);
            }

            return typeof(ColumnData<>).MakeGenericType(dataType);
        }
    }
}
