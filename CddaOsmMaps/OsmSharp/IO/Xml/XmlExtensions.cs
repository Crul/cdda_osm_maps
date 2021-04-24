﻿// The MIT License (MIT)

// Copyright (c) 2016 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using OsmSharp.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace OsmSharp.IO.Xml
{
    /// <summary>
    /// Contains extension methods for xml readers and writers.
    /// </summary>
    public static class XmlExtensions
    {
        private static string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

        /// <summary>
        /// Writes a datetime as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, DateTime? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToString(DATE_FORMAT, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Writes an Int64 as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, long? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToInvariantString());
            }
        }

        /// <summary>
        /// Writes an Int32 as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, int? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToInvariantString());
            }
        }

        /// <summary>
        /// Writes a single as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, float? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToInvariantString());
            }
        }

        /// <summary>
        /// Writes a double as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, double? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToInvariantString());
            }
        }

        /// <summary>
        /// Writes a string as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, string value)
        {
            if (value != null)
            {
                writer.WriteAttributeString(name, value);
            }
        }

        /// <summary>
        /// Writes a bool as an attribute.
        /// </summary>
        public static void WriteAttribute(this XmlWriter writer, string name, bool? value)
        {
            if (value.HasValue)
            {
                writer.WriteAttributeString(name, value.Value.ToInvariantString().ToLowerInvariant());
            }
        }

        /// <summary>
        /// Writes an xml serializable object as an element.
        /// </summary>
        public static void WriteElement(this XmlWriter writer, string name, IXmlSerializable xmlSerializable)
        {
            if (xmlSerializable != null)
            {
                writer.WriteStartElement(name);
                xmlSerializable.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes an xml serializable object as an element.
        /// </summary>
        public static void WriteElements(this XmlWriter writer, string name, IXmlSerializable[] xmlSerializables)
        {
            if (xmlSerializables != null)
            {
                for (var i = 0; i < xmlSerializables.Length; i++)
                {
                    var xmlSerializable = xmlSerializables[i];

                    writer.WriteStartElement(name);
                    xmlSerializable.WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }

        /// <summary>
        /// Writes a xml start [Name] element, then the string content, then an end element.
        /// </summary>
        public static void WriteStartAndEndElementWithContent(this XmlWriter writer, string name, string content)
        {
            if (content != null)
            {
                writer.WriteStartElement(name);
                writer.WriteString(content);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Reads a double attribute.
        /// </summary>
        public static double? GetAttributeDouble(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads a single attribute.
        /// </summary>
        public static float? GetAttributeSingle(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (float.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Gets elements using the given actions.
        /// </summary>
        public static void GetElements(this XmlReader  reader, params Tuple<string, Action>[] getElements)
        {
            var getElementsDictionary = new Dictionary<string, Action>();
            foreach (var element in getElements)
            {
                getElementsDictionary.Add(element.Item1, element.Item2);
            }
            GetElements(reader, getElementsDictionary);
        }

        /// <summary>
        /// Gets elements using the given actions.
        /// </summary>
        public static void GetElements(this XmlReader reader, Dictionary<string, Action> getElements)
        {
            var parentName = reader.Name;
            reader.Read();

            while (reader.MoveToContent() != XmlNodeType.None)
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == parentName)
                    {
                        reader.Read();
                        break;
                    }

                    reader.Read();
                }
                else if (getElements.TryGetValue(reader.Name, out Action action))
                {
                    action();
                }
                else
                {
                    Logger.Log("XmlExtensions", TraceEventType.Verbose, "No action found for xml node with name {0}. Skipping it.", reader.Name);
                    reader.Read();
                }
            }
        }

        /// <summary>
        /// Reads an Int64 attribute.
        /// </summary>
        public static long? GetAttributeInt64(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (long.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out long value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads an Int32 attribute.
        /// </summary>
        public static int? GetAttributeInt32(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (int.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads a boolean attribute.
        /// </summary>
        public static bool? GetAttributeBool(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (bool.TryParse(valueString, out bool value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads a datetime attribute.
        /// </summary>
        public static DateTime? GetAttributeDateTime(this XmlReader reader, string name)
        {
            var valueString = reader.GetAttribute(name);
            if (DateTime.TryParse(valueString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads an enum attribute.
        /// </summary>
        public static T? GetAttributeEnum<T>(this XmlReader reader, string name) where T : struct
        {
            var valueString = reader.GetAttribute(name);
            if (Enum.TryParse(valueString, true, out T value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Reads the current element content and returns the contents as an enum.
        /// </summary>
        public static T? ReadElementContentAsEnum<T>(this XmlReader reader) where T : struct
        {
            var valueString = reader.ReadElementContentAsString();
            if (Enum.TryParse(valueString, true, out T value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Serializes to xml with default settings for OSM-related entities.
        /// </summary>
        public static string SerializeToXml<T>(this T value)
        {
            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = false;
            settings.NewLineChars = string.Empty;
            var emptyNamespace = new XmlSerializerNamespaces();
            emptyNamespace.Add(string.Empty, string.Empty);

            using (var resultStream = new MemoryStream())
            {
                using (var stringWriter = XmlWriter.Create(resultStream, settings))
                {
                    serializer.Serialize(stringWriter, value, emptyNamespace);
                    resultStream.Seek(0, SeekOrigin.Begin);
                    var streamReader = new StreamReader(resultStream);
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}