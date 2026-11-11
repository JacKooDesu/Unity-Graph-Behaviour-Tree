using System;
using UnityEngine;
using BehaviourTreeEditor.Runtime;

namespace BehaviourTreeEditor.Editor
{
    internal static class RuntimeTypeExtensions
    {
        public static Type TypeMapper(this Runtime.EValue valueType)
        {
            return valueType switch
            {
                EValue.Bool => typeof(bool),
                EValue.Int => typeof(int),
                EValue.Float => typeof(float),
                EValue.String => typeof(string),
                EValue.Vector3 => typeof(Vector3),
                EValue.Vector2 => typeof(Vector2),
                _ => throw new ArgumentOutOfRangeException(nameof(valueType), valueType, null),
            };
        }
    }
}
