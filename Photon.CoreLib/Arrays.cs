using System;

namespace Photon
{
    public static class Arrays
    {
        public static T[] Concat<T>(T[] array, T value)
        {
            if (array == null)
            {
                return new[] { value };
            }
            
            var index = array.Length;
            if (index == 0)
            {
                return new[] {value};
            }

            var newArray = new T[index + 1];
            Array.Copy(array, newArray, index);
            newArray[index] = value;

            return newArray;
        }

        public static T[] Remove<T>(T[] array, T value)
        {
            if (array == null || array.Length == 0)
            {
                return array;
            }

            var index = Array.IndexOf(array, value);
            if (index == -1)
            {
                return array;
            }

            if (index == 0 && array.Length == 1)
            {
                return null;
            }

            var newArray = new T[array.Length - 1];
            Array.Copy(array, 0, newArray, 0, index);
            Array.Copy(array, index + 1, newArray, index, newArray.Length - index);
            return newArray;
        }
    }
}
