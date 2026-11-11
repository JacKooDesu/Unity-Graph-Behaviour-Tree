using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace System
{
    public static class ExtensionMethods
    {
        public static int IndexOf<T>(this IReadOnlyList<T> collection, T item)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(collection[i], item))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public static class TypeExtensions
    {
        static Type ResolveGeneric(Type type)
        {
            if (!type.IsGenericType) return type;

            var genericType = type.GetGenericTypeDefinition();
            return genericType != type ? genericType : type;
        }

        public static bool InheritsOrImplements(this Type type, Type baseType)
        {
            type = ResolveGeneric(type);
            baseType = ResolveGeneric(baseType);

            while (type != typeof(object))
            {
                if (baseType == type)
                    return true;

                // interfaces
                if (type.GetInterfaces().Any(i => ResolveGeneric(i) == baseType))
                    return true;

                type = ResolveGeneric(type.BaseType);
                if (type == null) return false;
            }
            return false;
        }

        public static bool TryGetEnumerableType(this Type type, out Type elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type.InheritsOrImplements(typeof(IEnumerable<>)))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        /// <summary>
        /// 取得 Indexer
        /// </summary>
        /// <param name="indexerType">預設使用 int</param>
        public static bool TryGetIndexer(
            this Type type,
            out PropertyInfo indexerProperty,
            out PropertyInfo countProperty,
            Type indexerType = null)
        {
            indexerType ??= typeof(int);
            indexerProperty = default;
            countProperty = default;

            if (type.IsArray)
            {
                if (indexerType != typeof(int))
                    return false;
                indexerProperty = null;
                return true;
            }

            // Check indexer
            indexerProperty = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p
                    .GetIndexParameters()
                    .Where(pi => pi.ParameterType == indexerType)
                    .Any());
            countProperty = type.GetProperty(nameof(IList.Count), BindingFlags.Public | BindingFlags.Instance);

            return indexerProperty != null && countProperty != null;
        }
    }
}