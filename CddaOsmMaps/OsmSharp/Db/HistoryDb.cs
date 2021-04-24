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
using System.Collections.Generic;
using OsmSharp.Changesets;
using OsmSharp.Db.Impl;
using System.Linq;

namespace OsmSharp.Db
{
    /// <summary>
    /// An internal class implementing a snapshot db.
    /// </summary>
    public class HistoryDb : IHistoryDb
    {
        private readonly IHistoryDbImpl _db;

        /// <summary>
        /// Creates a new history db.
        /// </summary>
        public HistoryDb(IHistoryDbImpl db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private long _lastNodeId = -1;
        private long _lastWayId = -1;
        private long _lastRelationId = -1;
        private long _lastChangesetId = -1;

        /// <summary>
        /// Gets the next id for the given type.
        /// </summary>
        private long GetNextId(OsmGeoType type)
        {
            switch(type)
            {
                case OsmGeoType.Node:
                    if (_lastNodeId < 0)
                    {
                        _lastNodeId = _db.GetLastId(OsmGeoType.Node);
                        if (_lastNodeId < 0)
                        {
                            _lastNodeId = 0;
                        }
                    }
                    _lastNodeId++;
                    return _lastNodeId;
                case OsmGeoType.Way:
                    if (_lastWayId < 0)
                    {
                        _lastWayId = _db.GetLastId(OsmGeoType.Way);
                        if (_lastWayId < 0)
                        {
                            _lastWayId = 0;
                        }
                    }
                    _lastWayId++;
                    return _lastWayId;
                case OsmGeoType.Relation:
                    if (_lastRelationId < 0)
                    {
                        _lastRelationId = _db.GetLastId(OsmGeoType.Relation);
                        if (_lastRelationId < 0)
                        {
                            _lastRelationId = 0;
                        }
                    }
                    _lastRelationId++;
                    return _lastRelationId;
            }
            throw new System.Exception("Unknown OsmGeo type.");
        }

        /// <summary>
        /// Gets the next changeset id.
        /// </summary>
        private long GetNextChangesetId()
        {
            if (_lastChangesetId < 0)
            {
                _lastChangesetId = _db.GetLastChangesetId();
                if (_lastChangesetId < 0)
                {
                    _lastChangesetId = 0;
                }
            }
            _lastChangesetId++;
            return _lastChangesetId;
        }

        /// <summary>
        /// Clears all data.
        /// </summary>
        public void Clear()
        {
            _db.Clear();
        }

        /// <summary>
        /// Adds the given objects.
        /// </summary>
        public void Add(IEnumerable<OsmGeo> osmGeos)
        {
            if (osmGeos == null) throw new ArgumentNullException(nameof(osmGeos));
            
            _db.Add(osmGeos);
        }

        /// <summary>
        /// Adds the given changeset.
        /// </summary>
        public void Add(Changeset meta, OsmChange changes)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            
            _db.AddOrUpdate(meta);
            _db.AddChanges(meta.Id.Value, changes);
        }

        /// <summary>
        /// Gets all visible objects.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OsmGeo> Get()
        {
            return _db.Get();
        }

        /// <summary>
        /// Gets the last visible version of the object of the given type and given id.
        /// </summary>
        public OsmGeo Get(OsmGeoType type, long id)
        {
            return _db.Get(new OsmGeoKey[] { new OsmGeoKey(type, id) }).FirstOrDefault();
        }

        /// <summary>
        /// Gets all visible objects for the given keys.
        /// </summary>
        public IEnumerable<OsmGeo> Get(IEnumerable<OsmGeoKey> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            
            return _db.Get(keys);
        }

        /// <summary>
        /// Gets all objects for the given keys.
        /// </summary>
        public IEnumerable<OsmGeo> Get(IEnumerable<OsmGeoVersionKey> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            
            return _db.Get(keys);
        }

        /// <summary>
        /// Gets all visible objects within the given bounding box.
        /// </summary>
        public IEnumerable<OsmGeo> Get(float minLatitude, float minLongitude, float maxLatitude, float maxLongitude)
        {
            return _db.Get(minLatitude, minLongitude, maxLatitude, maxLongitude);
        }

        /// <summary>
        /// Opens a changeset.
        /// </summary>
        public long OpenChangeset(Changeset info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            
            info.Id = this.GetNextChangesetId();

            _db.AddOrUpdate(info);
            return info.Id.Value;
        }

        /// <summary>
        /// Applies a changeset.
        /// </summary>
        public DiffResultResult ApplyChangeset(long id, OsmChange changeset)
        {
            if (changeset == null) throw new ArgumentNullException(nameof(changeset));

            var results = new List<OsmGeoResult>();
            var nodeTransforms = new Dictionary<long, long>();
            var wayTransforms = new Dictionary<long, long>();
            var relationTransforms = new Dictionary<long, long>();
            
            if (changeset.Create != null)
            {
                foreach (var create in changeset.Create)
                {
                    var newId = this.GetNextId(create.Type);
                    switch(create.Type)
                    {
                        case OsmGeoType.Node:
                            nodeTransforms.Add(create.Id.Value, newId);
                            break;
                        case OsmGeoType.Way:
                            var way = create as Way;
                            for (var i = 0; i < way.Nodes.Length; i++)
                            {
                                long newNodeId;
                                if (nodeTransforms.TryGetValue(way.Nodes[i], out newNodeId))
                                {
                                    way.Nodes[i] = newNodeId;
                                }
                            }
                            wayTransforms.Add(create.Id.Value, newId);
                            break;
                        case OsmGeoType.Relation:
                            var relation = create as Relation;
                            for (var i = 0; i < relation.Members.Length; i++)
                            {
                                long newMemberId;
                                var member = relation.Members[i];
                                switch(member.Type)
                                {
                                    case OsmGeoType.Node:
                                        if (nodeTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                    case OsmGeoType.Way:
                                        if (wayTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                    case OsmGeoType.Relation:
                                        if (relationTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                }
                                relation.Members[i] = member;
                            }
                            relationTransforms.Add(create.Id.Value, newId);
                            break;
                    }

                    results.Add(OsmGeoResult.CreateCreation(
                        create, newId));
                    create.Id = newId;
                    create.Version = 1;
                    create.TimeStamp = DateTime.Now.ToUniversalTime();
                    create.Visible = true;
                }

                this.Add(changeset.Create);
            }

            if (changeset.Modify != null)
            {
                _db.Archive(changeset.Modify.Select(x =>
                    new OsmGeoKey(x.Type, x.Id.Value)));

                foreach(var modify in changeset.Modify)
                {
                    results.Add(OsmGeoResult.CreateModification(
                        modify, modify.Version.Value + 1));

                    switch (modify.Type)
                    {
                        case OsmGeoType.Way:
                            var way = modify as Way;
                            for (var i = 0; i < way.Nodes.Length; i++)
                            {
                                long newNodeId;
                                if (nodeTransforms.TryGetValue(way.Nodes[i], out newNodeId))
                                {
                                    way.Nodes[i] = newNodeId;
                                }
                            }
                            break;
                        case OsmGeoType.Relation:
                            var relation = modify as Relation;
                            for (var i = 0; i < relation.Members.Length; i++)
                            {
                                long newMemberId;
                                var member = relation.Members[i];
                                switch (member.Type)
                                {
                                    case OsmGeoType.Node:
                                        if (nodeTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                    case OsmGeoType.Way:
                                        if (wayTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                    case OsmGeoType.Relation:
                                        if (relationTransforms.TryGetValue(member.Id, out newMemberId))
                                        {
                                            member.Id = newMemberId;
                                        }
                                        break;
                                }
                                relation.Members[i] = member;
                            }
                            break;
                    }

                    modify.Version = modify.Version + 1;
                    modify.TimeStamp = DateTime.Now.ToUniversalTime();
                    modify.Visible = true;
                }
                _db.Add(changeset.Modify);
            }

            if (changeset.Delete != null)
            {
                foreach(var delete in changeset.Delete)
                {
                    results.Add(OsmGeoResult.CreateDeletion(
                        delete));
                }
                _db.Archive(changeset.Delete.Select(x =>
                    new OsmGeoKey(x.Type, x.Id.Value)));
            }

            return new DiffResultResult(new DiffResult()
            {
                Results = results.ToArray(),
                Generator = "OsmSharp",
                Version = 0.6f
            }, DiffResultStatus.BestEffortOK);
        }

        /// <summary>
        /// Updates changeset info.
        /// </summary>
        public void UpdateChangesetInfo(Changeset info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            
            _db.AddOrUpdate(info);
        }

        /// <summary>
        /// Closes a changeset.
        /// </summary>
        public bool CloseChangeset(long id)
        {
            var info = _db.GetChangeset(id);

            if (info == null ||
                info.ClosedAt != null)
            {
                return false;
            }
            info.ClosedAt = DateTime.Now.ToUniversalTime();
            _db.AddOrUpdate(info);
            return true;
        }

        /// <summary>
        /// Gets the changeset with the given id.
        /// </summary>
        public Changeset GetChangeset(long id)
        {
            return _db.GetChangeset(id);
        }

        /// <summary>
        /// Gets the changes for the changeset with the given id.
        /// </summary>
        public OsmChange GetChanges(long id)
        {
            return _db.GetChanges(id);
        }
    }
}