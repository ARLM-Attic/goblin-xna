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

using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Vision.Util;
using GoblinXNA.Helpers;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// A scene graph node that defines an optically tracked fiducial marker.
    /// 
    /// Any nodes added below a MarkerNode with WorldTranformation properties will be affected by the
    /// transformation returned by the marker tracker including GeometryNode, ParticleNode, SoundNode,
    /// CameraNode, and LightNode.
    /// </summary>
    public class MarkerNode : BranchNode
    {
        #region Member Fields

        protected int arTagID;
        protected String arTagArrayName;
        protected int arTagSingleMarkerID;
        protected int maxDropouts;
        protected int dropout;
        protected bool found;
        protected bool optimize;
        protected Smoother matrixSmoother;
        protected MarkerTracker tracker;
        protected Matrix prevMatrix;
        protected Matrix worldTransformation;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a node that is tracked by a fiducial marker array and updated automatically.
        /// </summary>
        /// <param name="name">Name of this marker node (doesn't have to be unique)</param>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagArrayName">The name of this fiducial marker array to look for</param>
        /// <param name="smoothingAlpha">The alpha value used to control the transition
        /// smoothness between consecutive frames in the range of [0.0f - 1.0f] excluding 0 and 1. If the marker
        /// transformation is expected to be more dynamic, then use larger values; otherwise,
        /// use smaller values. Usually, 0.5f would be good</param>
        public MarkerNode(String name, MarkerTracker tracker, String arTagArrayName, 
            float smoothingAlpha)
            : base(name)
        {
            this.arTagArrayName = arTagArrayName;
            this.tracker = tracker;
            arTagID = tracker.SetMarkerArray(arTagArrayName);
            if (smoothingAlpha <= 0 || smoothingAlpha > 1)
                throw new ArgumentException("smoothingAlpha has to be between 0.0f and 1.0f excluding 0");
            if (smoothingAlpha == 1)
                matrixSmoother = null;
            else
                matrixSmoother = new Smoother(smoothingAlpha);
            found = false;
            maxDropouts = 5;
            prevMatrix = Matrix.Identity;
            dropout = 0;
            optimize = false;
        }

        /// <summary>
        /// Creates a node that is tracked by a single fiducial marker and updated automatically.
        /// </summary>
        /// <param name="name">Name of this marker node (doesn't have to be unique)</param>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagSingleMarkerID">The id of a single fiducial marker to look for</param>
        /// <param name="smoothingAlpha">The alpha value used to control the transition
        /// smoothness between consecutive frames in the range of [0.0f - 1.0f] excluding 0 and 1. If the marker
        /// transformation is expected to be more dynamic, then use larger values; otherwise,
        /// use smaller values. Usually, 0.5f would be good</param>
        public MarkerNode(String name, MarkerTracker tracker, int arTagSingleMarkerID,
            float smoothingAlpha)
            : base(name)
        {
            this.arTagSingleMarkerID = arTagSingleMarkerID;
            this.tracker = tracker;
            arTagID = tracker.SetSingleMarker(arTagSingleMarkerID);
            if (smoothingAlpha <= 0 || smoothingAlpha > 1)
                throw new ArgumentException("smoothingAlpha has to be between 0.0f and 1.0f excluding 0");
            if (smoothingAlpha == 1)
                matrixSmoother = null;
            else
                matrixSmoother = new Smoother(smoothingAlpha);
            found = false;
            maxDropouts = 5;
            prevMatrix = Matrix.Identity;
            dropout = 0;
            optimize = false;
        }

        /// <summary>
        /// Creates a node that is tracked by a single fiducial marker and updated automatically.
        /// </summary>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagArrayName">The name of this fiducial marker array to look for</param>
        /// <param name="smoothingAlpha">The alpha value used to control the transition
        /// smoothness between consecutive frames in the range of [0.0f - 1.0f]. If the marker
        /// transformation is expected to be more dynamic, then use larger values; otherwise,
        /// use smaller values. Usually, 0.5f would be good</param>
        public MarkerNode(MarkerTracker tracker, String arTagArrayName,
            float smoothingAlpha)
            : this("", tracker, arTagArrayName, smoothingAlpha) { }

        /// <summary>
        /// Creates a node that is tracked by a fiducial marker array and updated automatically.
        /// </summary>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagSingleMarkerID">The id of a single fiducial marker to look for</param>
        /// <param name="smoothingAlpha">The alpha value used to control the transition
        /// smoothness between consecutive frames in the range of [0.0f - 1.0f]. If the marker
        /// transformation is expected to be more dynamic, then use larger values; otherwise,
        /// use smaller values. Usually, 0.5f would be good</param>
        public MarkerNode(MarkerTracker tracker, int arTagSingleMarkerID,
            float smoothingAlpha)
            : this("", tracker, arTagSingleMarkerID, smoothingAlpha) { }

        /// <summary>
        /// Creates a node that is tracked by a fiducial marker array and updated automatically
        /// with 1.0f smoothingAlpha (smoothing is not applied).
        /// </summary>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagArrayName">The name of this fiducial marker array to look for</param>
        public MarkerNode(MarkerTracker tracker, String arTagArrayName)
            :
            this("", tracker, arTagArrayName, 1) { }

        /// <summary>
        /// Creates a node that is tracked by a single fiducial marker and updated automatically
        /// with 1.0f smoothingAlpha (smoothing is not applied).
        /// </summary>
        /// <param name="tracker">A marker tracker used to track this fiducial marker</param>
        /// <param name="arTagSingleMarkerID">The id of a single fiducial marker to look for</param>
        public MarkerNode(MarkerTracker tracker, int arTagSingleMarkerID)
            :
            this("", tracker, arTagSingleMarkerID, 1) { }

        #endregion

        #region Properties
        /// <summary>
        /// Gets an ID returned by the ARTag library.
        /// </summary>
        public virtual int ARTagID
        {
            get { return arTagID; }
        }

        /// <summary>
        /// Gets the name of the ARTag array associated with this marker node.
        /// </summary>
        public virtual String ARTagArrayName
        {
            get { return arTagArrayName; }
        }

        /// <summary>
        /// Gets the ARTag single marker ID associated with this marker node.
        /// </summary>
        public virtual int ARTagSingleMarkerID
        {
            get { return arTagSingleMarkerID; }
        }

        /// <summary>
        /// Gets or sets the maximum number of dropouts.
        /// </summary>
        /// <remarks>
        /// Dropout count is used to make marker tracking more stable. For example, if MaxDropouts
        /// is set to 5, then even if the marker is not detected for 5 frames, it will use the previously
        /// detected transformation.
        /// </remarks>
        /// <seealso cref="WorldTransformation"/>
        public virtual int MaxDropouts
        {
            get { return maxDropouts; }
            set { maxDropouts = value; }
        }

        /// <summary>
        /// Gets whether the marker is detected.
        /// </summary>
        public virtual bool MarkerFound
        {
            get { return found; }
        }

        /// <summary>
        /// Gets the transformation of the detected marker. 
        /// </summary>
        /// <remarks>
        /// If no marker is detected after MaxDropouts, then transformation matrix with 
        /// all zero values is returned.
        /// </remarks>
        public virtual Matrix WorldTransformation
        {
            get { return worldTransformation; }
        }

        /// <summary>
        /// Gets or sets whether to optimize the scene graph by not traversing the nodes
        /// added below this node if marker is not found.
        /// </summary>
        public virtual bool Optimize
        {
            get { return optimize; }
            set { optimize = value; }
        }

        #endregion

        #region Override Methods
        /// <summary>
        /// Marker node does not allow cloning a node.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="GoblinException">If this method is called</exception>
        public override Node CloneNode()
        {
            throw new GoblinException("You should not clone a Marker node since you should only have one " 
                + "Marker node associated with one marker array");
        }
        #endregion

        #region Update

        /// <summary>
        /// Updates the current matrix of this marker node
        /// </summary>
        internal void Update()
        {
            if (tracker.FindMarker(arTagID))
            {
                if (matrixSmoother == null)
                    worldTransformation = tracker.GetMarkerRHSMatrix();
                else
                    worldTransformation = matrixSmoother.FilterMatrix(tracker.GetMarkerRHSMatrix());
                prevMatrix = worldTransformation;
                dropout = 0;
                found = true;
            }
            else
            {
                if (dropout < maxDropouts)
                {
                    dropout++;
                    worldTransformation = prevMatrix;
                }
                else
                {
                    found = false;
                    worldTransformation = MatrixHelper.Empty;
                }
            }
        }

        #endregion
    }
}
