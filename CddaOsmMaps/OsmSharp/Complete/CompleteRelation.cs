﻿// The MIT License (MIT)

// Copyright (c) 2017 Ben Abelshausen

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
    /// A complete relation.
    /// </summary>
    public class CompleteRelation : CompleteOsmGeo
    {
        /// <summary>
        /// Creates a new relation.
        /// </summary>
        public CompleteRelation()
        {

        }

        /// <summary>
        /// Returns the relation type.
        /// </summary>
        public override OsmGeoType Type
        {
            get { return OsmGeoType.Relation; }
        }

        /// <summary>
        /// Gets the relation members.
        /// </summary>
        public CompleteRelationMember[] Members { get; set; }

        /// <summary>
        /// Converts this relation into it's simple counterpart.
        /// </summary>
        /// <returns></returns>
        public override OsmGeo ToSimple()
        {
            var relation = new Relation();
            relation.Id = this.Id;
            relation.ChangeSetId = this.ChangeSetId;
            relation.Tags = this.Tags;
            relation.TimeStamp = this.TimeStamp;
            relation.UserId = this.UserId;
            relation.UserName = this.UserName;
            relation.Version = this.Version;
            relation.Visible = this.Visible;

            if (this.Members != null)
            {
                relation.Members = new RelationMember[this.Members.Length];
                for (var i = 0; i < relation.Members.Length; i++)
                {
                    var member = this.Members[i];
                    if (member == null)
                    {
                        continue;
                    }

                    var simpleMember = new RelationMember();
                    simpleMember.Id = member.Member.Id;
                    simpleMember.Role = member.Role;
                    simpleMember.Type = member.Member.Type;

                    relation.Members[i] = simpleMember;
                }
            }
            return relation;
        }

        /// <summary>
        /// Returns a description of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("http://www.openstreetmap.org/?relation={0}",
                this.Id);
        }
    }

    /// <summary>
    /// A complete relation member.
    /// </summary>
    public class CompleteRelationMember
    {
        /// <summary>
        /// The member.
        /// </summary>
        public ICompleteOsmGeo Member { get; set; }

        /// <summary>
        /// The role.
        /// </summary>
        public string Role { get; set; }
    }
}
