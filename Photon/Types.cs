using System;
using System.Reflection;
using System.Linq;

namespace Photon
{
    internal static class Types
    {
        internal static bool CanCastTo(this Type sourceType, Type targetType)
        {
            if (targetType.IsAssignableFrom(sourceType))
            {
                return true;
            }

            var result = HasCastOperator(sourceType, sourceType, targetType) ||
                HasCastOperator(targetType, sourceType, targetType);
            return result;
        }

        private static bool HasCastOperator(IReflect type, Type sourceType, Type targetType)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(x => IsCastOperatorMethod(x, sourceType, targetType));
        }

        private static bool IsCastOperatorMethod(MethodInfo method, Type sourceType, Type targetType)
        {
            var result = (method.Name == "op_Implicit" || method.Name == "op_Explicit") &&
                method.ReturnType == targetType;
            if (result)
            {
                var parameters = method.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == sourceType;
            }
            return false;       
        }
    }
}

