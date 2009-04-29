/************************************************************************************ 
 * Copyright (c) 2008-2009, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;

using GoblinXNA.Helpers;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// An implementation of a scene graph node that can have children node
    /// </summary>
    public class BranchNode : Node
    {
        #region Member Fields
        protected List<Node> children;
        protected bool prune;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a branch node with a specified name
        /// </summary>
        /// <param name="name">The name of this node</param>
        public BranchNode(String name) : base(name)
        {
            children = new List<Node>();
            prune = false;
        }

        /// <summary>
        /// Creates a branch node with an empty name
        /// </summary>
        public BranchNode() : this("") { }
        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of this node's children.
        /// </summary>
        /// <returns></returns>
        public virtual List<Node> Children
        {
            get { return children; }
        }

        /// <summary>
        /// Gets or sets whether to prune the node's children
        /// </summary>
        /// <remarks>
        /// If set to true, children nodes will simply not be traversed in the scene graph, but won't 
        /// actually be removed from the scene graph. The default value is false.
        /// </remarks>
        public virtual bool Prune
        {
            get { return prune; }
            set { prune = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a child to this node.
        /// </summary>
        /// <param name="node">The child node to be added</param>
        public virtual void AddChild(Node node)
        {
            if (node.Parent != null)
                throw new GoblinException("Node " + Name + " already has a parent");
            if (children.Contains(node))
                throw new GoblinException("This child is already added to this node");

            CheckForLoops(node);

            if (scene != null)
            {
                // busy wait if the scene graph is processing
                while (scene.Processing) { }
            }
            children.Add(node);
            node.Enabled = this.Enabled;
            node.Parent = this;
            PropagateSceneGraph(node);
        }

        /// <summary>
        /// Removes a child from this node.
        /// </summary>
        /// <param name="node">The child to be removed</param>
        public virtual void RemoveChild(Node node)
        {
            children.Remove(node);
            node.Parent = null;
            node.SceneGraph = null;
            if(node is BranchNode)
                foreach (Node child in ((BranchNode)node).Children)
                    PropagateSceneGraph(child);

            if (scene != null)
                scene.RecursivelyRemoveFromRendering(node);
        }

        /// <summary>
        /// Removes the child at the specified index from this node.
        /// </summary>
        /// <param name="index">The index where to remove a child</param>
        public virtual void RemoveChildAt(int index)
        {
            Node node = children[index];
            children.Remove(node);
            node.Parent = null;
            node.SceneGraph = null;
            if(node is BranchNode)
                foreach (Node child in ((BranchNode)node).Children)
                    PropagateSceneGraph(child);

            if (scene != null)
                scene.RecursivelyRemoveFromRendering(node);
        }

        /// <summary>
        /// Removes all children from this node.
        /// </summary>
        public virtual void RemoveChildren()
        {
            if (children.Count == 0)
                return;

            foreach (Node node in children)
            {
                node.Parent = null;
                node.SceneGraph = null;
                if(node is BranchNode)
                    foreach (Node child in ((BranchNode)node).Children)
                        PropagateSceneGraph(child);

                if (scene != null)
                    scene.RecursivelyRemoveFromRendering(node);
            }

            children.Clear();
        }

        /// <summary>
        /// Clones this scene node including its children.
        /// </summary>
        /// <returns>A cloned node with its children</returns>
        public virtual Node CloneTree()
        {
            BranchNode node = (BranchNode) base.CloneNode();

            foreach (Node child in children)
            {
                Node clone;
                if(child is BranchNode)
                    clone = ((BranchNode)child).CloneTree();
                else
                    clone = child.CloneNode();
                node.AddChild(clone);
            }

            return node;
        }
        #endregion

        #region Override Methods

        public override byte[] Encode()
        {
            // 1 byte for prune
            // child nodes information is not encoded because the scene graph can be reconstructed
            // with the parent ID information

            // Encodes the base class's data first
            byte[] data = base.Encode();

            byte[] additionalData = new byte[sizeof(bool)];
            data[0] = BitConverter.GetBytes(prune)[0];

            return ByteHelper.ConcatenateBytes(data, additionalData);
        }

        public override int Decode(byte[] data, out int numBytesDecoded)
        {
            int bytesDecoded = 0;
            int parentID = base.Decode(data, out bytesDecoded);

            prune = BitConverter.ToBoolean(data, bytesDecoded);
            bytesDecoded++;

            numBytesDecoded = bytesDecoded;

            return parentID;
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (Node node in children)
                node.Dispose();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Check whether this node is part of a cyclic graph
        /// </summary>
        /// <param name="node"></param>
        protected void CheckForLoops(Node node)
        {
            Node predecessor = this;
            while (predecessor.Parent != null)
            {
                predecessor = predecessor.Parent;
                if (predecessor == node)
                    throw new GoblinException("Not allowed to create cyclic scene graph");
            }
        }

        protected void PropagateSceneGraph(Node node)
        {
            node.SceneGraph = node.Parent.SceneGraph;
            if(node is BranchNode)
                foreach (Node child in ((BranchNode)node).Children)
                    PropagateSceneGraph(child);
        }

        #endregion
    }
}
