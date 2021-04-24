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

using OsmSharp.IO.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace OsmSharp.Streams
{
    /// <summary>
    /// A stream target that writes OSM-XML.
    /// </summary>
    public class XmlOsmStreamTarget : OsmStreamTarget, IDisposable
    {
        private readonly XmlWriter _writer;
        private readonly bool _disposeStream = false;

        private readonly XmlSerializerNamespaces _emptyNamespace;
        private readonly XmlSerializer _nodeSerializer;
        private readonly XmlSerializer _waySerializer;
        private readonly XmlSerializer _relationSerializer;
        
        /// <summary>
        /// Creates a new stream target.
        /// </summary>
        public XmlOsmStreamTarget(Stream stream)
        {
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            _writer = XmlWriter.Create(stream, settings);

            _nodeSerializer = new XmlSerializer(typeof(Node));
            _waySerializer = new XmlSerializer(typeof(Way));
            _relationSerializer = new XmlSerializer(typeof(Relation));

            _emptyNamespace = new XmlSerializerNamespaces();
            _emptyNamespace.Add(String.Empty, String.Empty);
        }

        private bool _initialized = false;

        /// <summary>
        /// Gets or sets the generator.
        /// </summary>
        public string Generator { get; set; } = "OsmSharp";

        /// <summary>
        /// Gets or sets the bounds.
        /// </summary>
        public API.Bounds Bounds { get; set; }

        /// <summary>
        /// Initializes this target.
        /// </summary>
        public override void Initialize()
        {
            if (!_initialized)
            {
                _writer.WriteRaw("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                _writer.WriteStartElement("osm");
                _writer.WriteAttributeString("version", "0.6");
                if (string.IsNullOrWhiteSpace(this.Generator))
                {
                    _writer.WriteAttributeString("generator", "OsmSharp");
                }
                else
                {
                    _writer.WriteAttributeString("generator", this.Generator);
                }

                if (this.ExtraRootAttributes.Count > 0)
                {
                    foreach (var pair in this.ExtraRootAttributes)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Item1) &&
                            string.IsNullOrWhiteSpace(pair.Item2))
                        {
                            continue;
                        }

                        _writer.WriteAttributeString(pair.Item1, pair.Item2);
                    }
                }

                if (this.Bounds != null)
                {
                    _writer.WriteRaw(this.Bounds.SerializeToXml());
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Gets or sets a list of extra root attributes.
        /// </summary>
        public List<Tuple<string, string>> ExtraRootAttributes { get; private set; } = new List<Tuple<string, string>>();

        /// <summary>
        /// Adds a node to the xml output stream.
        /// </summary>
        public override void AddNode(Node node)
        {
            _nodeSerializer.Serialize(_writer, node);
        }

        /// <summary>
        /// Adds a way to this target.
        /// </summary>
        public override void AddWay(Way way)
        {
            _waySerializer.Serialize(_writer, way);
        }

        /// <summary>
        /// Adds a relation to this target.
        /// </summary>
        public override void AddRelation(Relation relation)
        {
            _relationSerializer.Serialize(_writer, relation);
        }
        
        private bool _closed = false;

        /// <summary>
        /// Closes this target.
        /// </summary>
        public override void Close()
        {
            base.Close();

            if (!_closed)
            {
                _writer.WriteRaw("</osm>");
                _writer.Flush();
                _closed = true;
            }
        }

        /// <summary>
        /// Disposes of all resource associated with this stream target.
        /// </summary>
        public void Dispose()
        {
            if (_disposeStream)
            {
#if !NET40
                _writer.Dispose();
#endif
            }
        }
    }
}