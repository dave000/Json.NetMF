// Source code is modified from Mike Jones's JSON Serialization and Deserialization library (https://www.ghielectronics.com/community/codeshare/entry/357)

using System;
using Microsoft.SPOT;
using System.Reflection;
using System.Collections;
using System.Text;

namespace Json.NETMF
{
    /// <summary>
    /// JSON.NetMF - JSON Serialization and Deserialization library for .NET Micro Framework
    /// </summary>
    public class JsonSerializer
    {
        public JsonSerializer(DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            DateFormat = dateTimeFormat;
        }

        /// <summary>
        /// Gets/Sets the format that will be used to display
        /// and parse dates in the Json data.
        /// </summary>
        public DateTimeFormat DateFormat { get; set; }

        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only public properties with getters are converted.</remarks>
        public string Serialize(object o)
        {
            return SerializeObject(o, this.DateFormat);
        }

        /// <summary>
        /// Desrializes a Json string into an object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns>An ArrayList, a Hashtable, a double, a long, a string, null, true, or false</returns>
        public object Deserialize(string json)
        {
            return DeserializeString(json);
        }

        public object Deserialize(string json, Type targetType)
        {

            var jsonObject = DeserializeString(json);
            if (jsonObject is Hashtable)
            {
                return loadJsonToType(targetType, (Hashtable)jsonObject);
            }
            
            return jsonObject;
        }

        private object loadJsonToType(Type targetType, Hashtable jsonObject)
        {
            var targetObject = targetType.GetConstructor(new Type[0]).Invoke(new object[0]);
            
            var methods = targetType.GetMethods();

            foreach (var method in methods)
            {
                
                if (StringExtensions.StartsWith(method.Name, "set_"))
                {
                    var propName = method.Name.Substring(4);
                    var attr = targetType.GetField("JSON_Name_" + propName);
                    if (attr != null)
                    {
                        propName = (string)attr.GetValue(null);
                    }

                    method.Invoke(targetObject, new[] { jsonObject[propName] });

                }

            }

            return targetObject;
        }



        /// <summary>
        /// Deserializes a Json string into an object.
        /// </summary>
        /// <param name="json"></param>
        /// <returns>An ArrayList, a Hashtable, a double, a long, a string, null, true, or false</returns>
        public static object DeserializeString(string json)
        {
            return JsonParser.JsonDecode(json);
        }

        /// <summary>
        /// Convert an object to a JSON string.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, Array, IDictionary, IEnumerable, Guid, Datetime, DictionaryEntry, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only public properties with getters are converted.</remarks>
        public static string SerializeObject(object o, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            if (o == null)
                return "null";

            Type type = o.GetType();

            switch (type.Name)
            {
                case "Boolean":
                    {
                        return (bool)o ? "true" : "false";
                    }
                case "String":
                case "Char":
                case "Guid":
                    {
                        return "\"" + o.ToString() + "\"";
                    }
                case "Single":
                case "Double":
                case "Decimal":
                case "Float":
                case "Byte":
                case "SByte":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                    {
                        return o.ToString();
                    }
                case "DateTime":
                    {
                        switch (dateTimeFormat)
                        {
                            case DateTimeFormat.Ajax:
                                // This MSDN page describes the problem with JSON dates:
                                // http://msdn.microsoft.com/en-us/library/bb299886.aspx
                                return "\"" + DateTimeExtensions.ToASPNetAjax((DateTime)o) + "\"";
                            case DateTimeFormat.ISO8601:
                            case DateTimeFormat.Default:
                            default:
                                return "\"" + DateTimeExtensions.ToIso8601((DateTime)o) + "\"";
                        }
                    }
            }

            if (o is IDictionary && !type.IsArray)
            {
                IDictionary dictionary = o as IDictionary;
                return SerializeIDictionary(dictionary, dateTimeFormat);
            }

            if (o is IEnumerable)
            {
                IEnumerable enumerable = o as IEnumerable;
                return SerializeIEnumerable(enumerable, dateTimeFormat);
            }

            if (type == typeof(System.Collections.DictionaryEntry))
            {
                DictionaryEntry entry = o as DictionaryEntry;
                Hashtable hashtable = new Hashtable();
                hashtable.Add(entry.Key, entry.Value);
                return SerializeIDictionary(hashtable, dateTimeFormat);
            }

            if (type.IsClass)
            {
                Hashtable hashtable = new Hashtable();

                // Iterate through all of the methods, looking for public GET properties
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    // We care only about property getters when serializing
                    if (StringExtensions.StartsWith(method.Name, "get_"))
                    {
                        // Ignore abstract and virtual objects
                        if (method.IsAbstract)
                        {
                            continue;
                        }

                        // Ignore delegates and MethodInfos
                        if ((method.ReturnType == typeof(System.Delegate)) ||
                            (method.ReturnType == typeof(System.MulticastDelegate)) ||
                            (method.ReturnType == typeof(System.Reflection.MethodInfo)))
                        {
                            continue;
                        }
                        // Ditto for DeclaringType
                        if ((method.DeclaringType == typeof(System.Delegate)) ||
                            (method.DeclaringType == typeof(System.MulticastDelegate)))
                        {
                            continue;
                        }

                        var methodName = method.Name.Substring(4);

                        //override method name
                        var attr = type.GetField("JSON_Name_" + methodName);
                        if (attr != null)
                        {
                            methodName = (string)attr.GetValue(null);
                        }

                        object returnObject = method.Invoke(o, null);
                        hashtable.Add(methodName, returnObject);
                    }
                }
                return SerializeIDictionary(hashtable, dateTimeFormat);
            }

            return null;
        }

        /// <summary>
        /// Convert an IEnumerable to a JSON string.
        /// </summary>
        /// <param name="enumerable">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        protected static string SerializeIEnumerable(IEnumerable enumerable, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            String result = "[";

            foreach (object current in enumerable)
            {
                if (result.Length > 1)
                {
                    result += ",";
                }

                result += SerializeObject(current, dateTimeFormat);
            }

            result += "]";
            return result;
        }

        /// <summary>
        /// Convert an IDictionary to a JSON string.
        /// </summary>
        /// <param name="dictionary">The value to convert.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        protected static string SerializeIDictionary(IDictionary dictionary, DateTimeFormat dateTimeFormat = DateTimeFormat.Default)
        {
            String result = "{";

            foreach (DictionaryEntry entry in dictionary)
            {
                if (result.Length > 1)
                {
                    result += ",";
                }

                result += "\"" + entry.Key + "\"";
                result += ":";
                result += SerializeObject(entry.Value, dateTimeFormat);
            }

            result += "}";
            return result;
        }

    }

    /// <summary>
    /// Enumeration of the popular formats of time and date
    /// within Json.  It's not a standard, so you have to
    /// know which on you're using.
    /// </summary>
    public enum DateTimeFormat
    {
        Default = 0,
        ISO8601 = 1,
        Ajax = 2
    }
}
