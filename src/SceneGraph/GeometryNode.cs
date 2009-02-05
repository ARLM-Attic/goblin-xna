/************************************************************************************ 
 * Copyright (c) 2008, Columbia University
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;
using GoblinXNA.Physics;
using GoblinXNA.Network;
using GoblinXNA.Helpers;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// A scene graph node that holds the model geometry, physical properties, etc.
    /// </summary>
    public class GeometryNode : BranchNode, IComparable<GeometryNode>
    {
        #region Field Members

        protected IModel model;
        protected NewtonVehicle vehicle;
        protected bool isRendered;
        private bool shouldRender;
        protected Material material;
        protected List<LightNode> illuminationLights;
        protected Matrix worldTransform;
        protected Matrix markerTransform;

        protected IPhysicsObject physicsProperties;
        protected NetworkObject networkProperties;

        protected BoundingSphere boundingVolume;
        protected bool showBoundingVolume;

        protected bool addToPhysicsEngine;
        protected bool isOccluder;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a geometry node that contains the actual geometry information required for
        /// rendering, physics simulation, and networking with a specified node name.
        /// </summary>
        /// <param name="name">
        /// The name of this geometry node (has to be unique for correct networking behavior)
        /// </param>
        public GeometryNode(String name)
            : base(name)
        {
            model = null;
            worldTransform = Matrix.Identity;
            markerTransform = Matrix.Identity;
            material = new Material();
            illuminationLights = new List<LightNode>();
            isOccluder = false;
            addToPhysicsEngine = false;

            physicsProperties = new PhysicsObject(this);
            networkProperties = new NetworkObject(name, id);

            boundingVolume = new BoundingSphere();
            showBoundingVolume = false;

            shouldRender = false;
        }

        /// <summary>
        /// Creates a geometry node that contains the actual geometry information required for
        /// rendering, physics simulation, and networking.
        /// </summary>
        public GeometryNode() : this("") { }

        #endregion

        #region Properties
        public override string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
                networkProperties.Identifier = value;
            }
        }
        /// <summary>
        /// Gets or sets the actual model used for rendering and physics simulation.
        /// </summary>
        public virtual IModel Model
        {
            get { return model; }
            set 
            { 
                model = value;
                physicsProperties.Model = value;
                boundingVolume.Radius = model.MinimumBoundingSphere.Radius;
            }
        }

        /// <summary>
        /// Gets or sets the material properties of this model.
        /// </summary>
        public virtual Material Material
        {
            get { return material; }
            set { material = value; }
        }

        /// <summary>
        /// Gets or sets the list of local light sources that will be used for illumination.
        /// </summary>
        internal List<LightNode> LocalLights
        {
            get { return illuminationLights; }
            set { illuminationLights = value; }
        }

        /// <summary>
        /// Gets or sets whether to add this geometry node to the physics engine.
        /// </summary>
        public virtual bool AddToPhysicsEngine
        {
            get { return addToPhysicsEngine; }
            set { addToPhysicsEngine = value; }
        }

        /// <summary>
        /// Gets or sets the physics properties associated with this geometry node.
        /// </summary>
        public virtual IPhysicsObject Physics
        {
            get { return physicsProperties; }
            set 
            { 
                physicsProperties = value;
                physicsProperties.Model = model;
            }
        }

        /// <summary>
        /// Gets the network properties associated with this geometry node.
        /// </summary>
        public virtual NetworkObject Network
        {
            get { return networkProperties; }
        }

        /// <summary>
        /// Gets the transformation of the model.
        /// </summary>
        public virtual Matrix WorldTransformation
        {
            get 
            {
                if (networkProperties.HasChange)
                {
                    worldTransform = networkProperties.WorldTransform;
                    physicsProperties.PhysicsWorldTransform = networkProperties.WorldTransform;
                }
                return worldTransform; 
            }
            internal set 
            { 
                worldTransform = value;
                networkProperties.WorldTransform = value;
            }
        }

        /// <summary>
        /// Gets the transform updated by a marker. This information is valid only when
        /// at least one of its successor is a MarkerNode.
        /// </summary>
        public Matrix MarkerTransform
        {
            get { return markerTransform; }
            internal set { markerTransform = value; }
        }

        /// <summary>
        /// Gets or sets whether this node is added to rendering routine
        /// </summary>
        internal bool IsRendered
        {
            get { return isRendered; }
            set { isRendered = value; }
        }

        /// <summary>
        /// Gets or sets whether this node should be rendered
        /// </summary>
        internal bool ShouldRender
        {
            get { return shouldRender; }
            set { shouldRender = value; }
        }

        /// <summary>
        /// Gets or sets whether this node is used as an occluder that occludes any object
        /// that is rendered behind this object
        /// </summary>
        public virtual bool IsOccluder
        {
            get { return isOccluder; }
            set { isOccluder = value; }
        }

        /// <summary>
        /// Gets a sphere that encloses the contents of all the nodes below the current one.
        /// </summary>
        /// <remarks>
        /// This node itself is not included in this bounding sphere
        /// </remarks>
        public virtual BoundingSphere BoundingVolume
        {
            get { return boundingVolume; }
            internal set { boundingVolume = value; }
        }

        /// <summary>
        /// Gets or sets if the bounding volume (sphere) should be displayed
        /// </summary>
        public virtual bool ShowBoundingVolume
        {
            get { return showBoundingVolume; }
            set { showBoundingVolume = value; }
        }
        #endregion

        #region IComparable<GeometryNode> Members
        /// <summary>
        /// Compares which GeometryNode's center is closer to the viewer location.
        /// </summary>
        /// <param name="other">The other GeometryNode objec to compare to</param>
        /// <returns></returns>
        public int CompareTo(GeometryNode other)
        {
            double thisDist = Vector3.Distance(boundingVolume.Center,
                State.CameraTransform.Translation);
            double otherDist = Vector3.Distance(other.BoundingVolume.Center,
                State.CameraTransform.Translation);

            if (thisDist > otherDist)
                return 1;
            else if (thisDist == otherDist)
                return 0;
            else
                return -1;
        }

        #endregion

        #region Override Methods

        public override byte[] Encode()
        {
            // 1 byte (bool) for needToComputBoundingVolume
            // 1 (bool) byte for showBoundingVolume
            // 4 (float) * 4 bytes for bounding volume center (x, y, z) and radius
            /*data[index++] = BitConverter.GetBytes(showBoundingVolume)[0];
            List<float> floats = new List<float>();
            floats.Add(boundingVolume.Center.X);
            floats.Add(boundingVolume.Center.Y);
            floats.Add(boundingVolume.Center.Z);
            floats.Add(boundingVolume.Radius);
            ByteHelper.FillByteArray(ref data, index, ByteHelper.ConvertFloatArray(floats));
            index += sizeof(float) * 4;*/

            return base.Encode();
        }

        public override Node CloneNode()
        {
            throw new GoblinException("You should not clone Geometry node");
        }

        public override void Dispose()
        {
            base.Dispose();
            material.Dispose();
            model.Dispose();
        }

        #endregion
    }
}
