using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MultiDownloader
{
    /// <summary>
    /// Utility to debug object.
    /// </summary>
    class ObjectDumper
    {
        private readonly int depth;
        private readonly Dictionary<object, int> hashListOfFoundElements;
        private readonly char indentChar;
        private readonly int indentSize;
        private readonly StringBuilder stringBuilder;
        private int currentIndent;
        private int currentLine;

        private ObjectDumper(int depth, int indentSize, char indentChar)
        {
            this.depth = depth;
            this.indentSize = indentSize;
            this.indentChar = indentChar;
            stringBuilder = new StringBuilder();
            hashListOfFoundElements = new Dictionary<object, int>();
        }

        /// <summary>
        /// Dumps object's content.
        /// </summary>
        /// <returns>
        /// String of contents.
        /// </returns>
        /// <param name="element">Object to dump.</param>
        /// <param name="depth">Depth of contents to dump.</param>
        /// <param name="indentSize">Indent size of contents.</param>
        /// <param name="indentChar">Character for indentation.</param>
        public static string Dump(object element, int depth = 4, int indentSize = 2, char indentChar = ' ')
        {
            var instance = new ObjectDumper(depth, indentSize, indentChar);
            return instance.DumpElement(element, true);
        }

        /// <summary>
        /// Checks whether object already dumped.
        /// </summary>
        /// <returns>
        /// Boolean of whether object already dumped.
        /// </returns>
        /// <param name="value">Object to check.</param>
        private bool AlreadyDumped(object value)
        {
            if (value == null)
                return false;

            if (hashListOfFoundElements.TryGetValue(value, out int lineNo))
            {
                Write("(reference already dumped - line:{0})", lineNo);
                return true;
            }

            hashListOfFoundElements.Add(value, currentLine);
            return false;
        }

        /// <summary>
        /// Dumps an object element.
        /// </summary>
        /// <returns>
        /// String of contents.
        /// </returns>
        /// <param name="element">Object element to dump.</param>
        /// <param name="isTopOfTree">Whether element is at the top of tree.</param>
        private string DumpElement(object element, bool isTopOfTree = false)
        {
            if (currentIndent > depth) return null;

            if (element == null || element is string)
            {
                Write(FormatValue(element));
            }
            else if (element is ValueType)
            {
                Type objectType = element.GetType();
                bool isWritten = false;

                if (objectType.GetTypeInfo().IsGenericType)
                {
                    Type baseType = objectType.GetGenericTypeDefinition();
                    if (baseType == typeof(KeyValuePair<,>))
                    {
                        isWritten = true;
                        Write("Key:");
                        currentIndent++;
                        DumpElement(objectType.GetProperty("Key").GetValue(element, null));
                        currentIndent--;
                        Write("Value:");
                        currentIndent++;
                        DumpElement(objectType.GetProperty("Value").GetValue(element, null));
                        currentIndent--;
                    }
                }

                if (!isWritten)
                {
                    Write(FormatValue(element));
                }
            }
            else
            {
                if (element is IEnumerable enumerableElement)
                {
                    foreach (object item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            currentIndent++;
                            DumpElement(item);
                            currentIndent--;
                        }
                        else
                        {
                            DumpElement(item);
                        }
                    }
                }
                else
                {
                    Type objectType = element.GetType();
                    Write("{{{0}(HashCode:{1})}}", objectType.FullName, element.GetHashCode());

                    if (!AlreadyDumped(element))
                    {
                        currentIndent++;
                        MemberInfo[] members = objectType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var memberInfo in members)
                        {
                            var fieldInfo = memberInfo as FieldInfo;
                            var propertyInfo = memberInfo as PropertyInfo;

                            if (fieldInfo == null && (propertyInfo == null || !propertyInfo.CanRead || propertyInfo.GetIndexParameters().Length > 0))
                                continue;

                            var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                            object value;

                            try
                            {
                                value = fieldInfo != null ? fieldInfo.GetValue(element) : propertyInfo.GetValue(element, null);
                            }
                            catch (Exception e)
                            {
                                Write("{0} failed with:{1}", memberInfo.Name, (e.GetBaseException() ?? e).Message);
                                continue;
                            }

                            if (type.GetTypeInfo().IsValueType || type == typeof(string))
                            {
                                Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                            }
                            else
                            {
                                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                                Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");
                                currentIndent++;
                                DumpElement(value);
                                currentIndent--;
                            }
                        }

                        currentIndent--;
                    }
                }
            }

            return isTopOfTree ? stringBuilder.ToString() : null;
        }

        /// <summary>
        /// Formats object's contents.
        /// </summary>
        /// <returns>
        /// String of formatted contents.
        /// </returns>
        /// <param name="o">Object to format.</param>
        private string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime)
                return (((DateTime)o).ToShortDateString());

            if (o is string)
                return "\"" + (string)o + "\"";

            if (o is char)
            {
                if (o.Equals('\0'))
                {
                    return "''";
                }
                else
                {
                    return "'" + (char)o + "'";
                }
            }

            if (o is ValueType)
                return (o.ToString());

            if (o is IEnumerable)
                return ("...");

            return ("{ }");
        }

        /// <summary>
        /// Write contents into StringBuilder.
        /// </summary>
        /// <param name="value">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        private void Write(string value, params object[] args)
        {
            var space = new string(indentChar, currentIndent * indentSize);
            if (args != null) value = string.Format(value, args);
            stringBuilder.AppendLine(space + value);
            currentLine++;
        }
    }
}
