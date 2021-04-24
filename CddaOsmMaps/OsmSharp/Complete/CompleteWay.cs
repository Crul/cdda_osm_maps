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

using System;

namespace OsmSharp.Complete
{
    /// <summary>
    /// Represents a complete way.
    /// </summary>
    public class CompleteWay : CompleteOsmGeo
    {
        /// <summary>
        /// Creates a new way.
        /// </summary>
        public CompleteWay()
        {

        }

        /// <summary>
        /// Returns the way type.
        /// </summary>
        public override OsmGeoType Type
        {
            get { return OsmGeoType.Way; }
        }

        /// <summary>
        /// Gets the ordered list of nodes.
        /// </summary>
        public Node[] Nodes { get; set; }

        /// <summary>
        /// Converts this way into it's simple counterpart.
        /// </summary>
        /// <returns></returns>
        public override OsmGeo ToSimple()
        {
            var way = new Way();
            way.Id = this.Id;
            way.ChangeSetId = this.ChangeSetId;
            way.Tags = this.Tags;
            way.TimeStamp = this.TimeStamp;
            way.UserId = this.UserId;
            way.UserName = this.UserName;
            way.Version = this.Version;
            way.Visible = this.Visible;

            way.Nodes = new long[this.Nodes.Length];
            for (var i = 0; i < this.Nodes.Length; i++)
            {
                way.Nodes[i] = this.Nodes[i].Id.Value;
            }

            return way;
        }

        /// <summary>
        /// Returns a description of this object.
        /// </summary>
        public override string ToString()
        {
            return String.Format("http://www.openstreetmap.org/?way={0}",
                this.Id);
        }
    }
}