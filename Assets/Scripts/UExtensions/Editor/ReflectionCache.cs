using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Callbacks;

namespace UExtensions.Editor
{
    internal static class ReflectionCache
    {
        public class TypeNameCollection
        {
            public string[] Names;
            public string[] FullNames;

            public TypeNameCollection(IEnumerable<Type> types)
            {
                Names = types.Select(t => t.FullName).ToArray();
                FullNames = types.Select(t => t.AssemblyQualifiedName).ToArray();
            }

            public int IndexOfFullName(string fullName) =>
                Array.IndexOf(FullNames, fullName);

            public string FullNameToName(string fullName) =>
                IndexOfFullName(fullName) is var index && index >= 0 ?
                    Names[index] : null;

            public string GetFullName(int index) =>
                index >= 0 && index < FullNames.Length ? FullNames[index] : null;

            public string GetName(int index) =>
                index >= 0 && index < Names.Length ? Names[index] : null;
        }

        public static Type[] Types;
        readonly static Dictionary<HashSet<Type>, TypeNameCollection> _typeNamesCache =
            new(HashSet<Type>.CreateSetComparer());
        public static TypeNameCollection GetTypeNameCollection(HashSet<Type> filter)
        {
            if (!_typeNamesCache.TryGetValue(filter, out var collection))
            {
                collection = new TypeNameCollection(
                    Types.Where(t =>
                        DefaultFilter(t) &&
                        filter.All(f => t.InheritsOrImplements(f))));
                _typeNamesCache[filter] = collection;
            }
            return collection;
        }

        [DidReloadScripts]
        static void OnScriptsReloaded()
        {
            CacheAssemblies();
        }

        static void CacheAssemblies()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(DefaultFilter);
            Types = types.ToArray();
        }

        static bool DefaultFilter(Type t) =>
            !t.IsAbstract &&
            !t.IsInterface &&
            !t.IsGenericType;
    }
}