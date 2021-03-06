// The MIT License (MIT)

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
using OsmSharp.Tags;

namespace OsmSharp.Complete
{
    /// <summary>
    /// An abstract representation of a complete OsmGeo object.
    /// </summary>
    public interface ICompleteOsmGeo
    {
        /// <summary>
        /// Gets or sets the changeset id.
        /// </summary>
        long? ChangeSetId { get; set; }

        /// <summary>
        /// Gets or sets the visible flag.
        /// </summary>
        bool? Visible { get; set; }

        /// <summary>
        /// The id of this object.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// Returns the type of osm data.
        /// </summary>
        OsmGeoType Type { get; }

        /// <summary>
        /// Returns the tags dictionary.
        /// </summary>
        TagsCollectionBase Tags { get; set; }

        /// <summary>
        /// Gets/Sets the timestamp.
        /// </summary>
        DateTime? TimeStamp { get; set; }

        /// <summary>
        /// The user that created this object
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        long? Version { get; set; }
    }
}