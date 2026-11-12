using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BehaviourTreeEditor.Common
{
    public class CustomFormatter : IFormatProvider, ICustomFormatter
    {
        public static readonly CustomFormatter Singleton = new();

        public object GetFormat(Type formatType) =>
            formatType == typeof(ICustomFormatter) ? this : null;

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var type = arg.GetType();

            if (type.Namespace.Contains("Unity"))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (fields.Length > 0)
                {
                    var fValue = fields.Select(f => string.Format(Singleton, "{0}", f.GetValue(arg)));
                    var args = string.Join(", ", fValue);
                    return $"new {type.Name}({args})";
                }
            }

            return arg switch
            {
                bool b => Bool(b),
                float f => Float(f),
                _ => arg?.ToString() ?? string.Empty
            };
        }

        private string Bool(bool b) => b ? "true" : "false";

        string Float(float f) =>
            f.ToString("G9", CultureInfo.InvariantCulture) + "f";
    }
}
