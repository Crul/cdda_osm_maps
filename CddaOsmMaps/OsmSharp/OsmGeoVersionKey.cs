﻿using System;

namespace OsmSharp
{
    /// <summary>
    /// A unique identifier including types and version #.
    /// </summary>
    public class OsmGeoVersionKey : IEquatable<OsmGeoVersionKey>
    {
        /// <summary>
        /// Creates a version key.
        /// </summary>
        public OsmGeoVersionKey(OsmGeoType type, long id, long version)
        {
            this.Type = type;
            this.Id = id;
            this.Version = version;
        }

        /// <summary>
        /// Creates a version key for the given object.
        /// </summary>
        public OsmGeoVersionKey(OsmGeo osmGeo)
        {
            this.Type = osmGeo.Type;
            this.Id = osmGeo.Id.Value;
            this.Version = osmGeo.Version.Value;
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public OsmGeoType Type { get; private set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// Gets or sets the version #.
        /// </summary>
        public long Version { get; private set; }
        
        /// <summary>
        /// Gets the previous version.
        /// </summary>
        public OsmGeoVersionKey Previous
        {
            get
            {
                if (this.Version == 1)
                {
                    return null;
                }
                return new OsmGeoVersionKey(this.Type, this.Id, this.Version - 1);
            }
        }

        /// <summary>
        /// Serves as a hashfunction.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode() ^
                this.Type.GetHashCode() ^
                this.Version.GetHashCode();
        }



        /// <summary>
        /// Returns true if the given object represents the same key.
        /// </summary>
        public bool Equals(OsmGeoVersionKey other)
        {
            return other.Id == this.Id &&
                other.Type == this.Type &&
                other.Version == this.Version;
        }

        /// <summary>
        /// Returns true if the given object represents the same key.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is OsmGeoVersionKey && Equals((OsmGeoVersionKey)obj);
        }
    }
}