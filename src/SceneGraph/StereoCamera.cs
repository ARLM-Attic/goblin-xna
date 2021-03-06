/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
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
using System.Xml;

using Microsoft.Xna.Framework;

using GoblinXNA.Helpers;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// This class represents a stereo camera view. Stereo only works if proper shutter glass
    /// and graphics card are used.
    /// </summary>
    public class StereoCamera : Camera
    {
        #region Member Fields

        protected float interpupillaryDistance;
        protected float interpupillaryShift;
        protected float focalLength;

        protected Matrix leftViewMatrix;
        protected Matrix rightViewMatrix;
        protected Matrix leftProjectionMatrix;
        protected Matrix rightProjectionMatrix;

        protected bool modifyLeftView;
        protected bool modifyRightView;
        protected bool modifyLeftProjection;
        protected bool modifyRightProjection;

        #region Temporary Variables

        protected Matrix tmpMat3;
        protected Matrix tmpMat4;
        protected Vector3 tmpVec1;
        protected Vector3 tmpVec2;

        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a stereo camera.
        /// </summary>
        public StereoCamera() : base()
        {
            interpupillaryDistance = 1.0f;
            interpupillaryShift = 0;
            focalLength = 10;

            aspectRatio /= 2;

            leftViewMatrix = Matrix.Identity;
            rightViewMatrix = Matrix.Identity;
            leftProjectionMatrix = Matrix.Identity;
            rightProjectionMatrix = Matrix.Identity;

            modifyLeftView = false;
            modifyRightView = false;
            modifyLeftProjection = false;
            modifyRightProjection = false;
        }

        /// <summary>
        /// A copy constructor.
        /// </summary>
        /// <param name="source"></param>
        public StereoCamera(StereoCamera source)
        {
            translation = source.Translation;
            rotation = source.Rotation;
            projection = source.Projection;
            fieldOfView = source.FieldOfViewY;
            aspectRatio = source.AspectRatio;
            zNearPlane = source.ZNearPlane;
            zFarPlane = source.ZFarPlane;

            interpupillaryDistance = source.InterpupillaryDistance;
            focalLength = source.FocalLength;

            leftViewMatrix = source.LeftView;
            rightViewMatrix = source.RightView;
            leftProjectionMatrix = source.LeftProjection;
            rightProjectionMatrix = source.RightProjection;

            modifyLeftView = false;
            modifyRightView = false;
            modifyLeftProjection = false;
            modifyRightProjection = false;
        }

        #endregion

        #region Properties
        /// <summary>
        /// ﻿Gets or sets the distance between the two eyes, measured at the pupils.
        /// </summary>
        public virtual float InterpupillaryDistance
        {
            get { return interpupillaryDistance; }
            set 
            { 
                interpupillaryDistance = value;
                ModifyAll();
            }
        }

        /// <summary>
        /// Gets or sets the shift of the interpupillary distance from the center of the
        /// two eyes. Positive value will shift toward the right eye, and negative value
        /// will shift toward the left eye. The default value is 0.
        /// </summary>
        /// <remarks>
        /// This property is used when center of the stereo display is not in the middle
        /// of the two eyes.
        /// </remarks>
        public virtual float InterpupillaryShift
        {
            get { return interpupillaryShift; }
            set
            {
                interpupillaryShift = value;
                ModifyAll();
            }
        }

        /// <summary>
        /// Gets or sets the focal length from the camera/eye.
        /// </summary>
        public virtual float FocalLength
        {
            get { return focalLength; }
            set
            {
                focalLength = value;
                ModifyAll();
            }
        }

        /// <summary>
        /// Gets or sets the view matrix of the left eye.
        /// </summary>
        public virtual Matrix LeftView
        {
            get 
            {
                if (modifyLeftView)
                {
                    Vector3 rightVec = Vector3.UnitX;
                    Vector3 upVec = Vector3.UnitY;
                    Vector3 target = -Vector3.UnitZ;

                    Matrix rotMat = Matrix.CreateFromQuaternion(rotation);
                    Vector3 camPos = translation + (rightVec * (-interpupillaryDistance / 2 + interpupillaryShift));

                    Vector3.Transform(ref target, ref rotMat, out target);
                    Vector3.Transform(ref upVec, ref rotMat, out upVec);
                    target += camPos;

                    Matrix.CreateLookAt(ref camPos, ref target, ref upVec, out leftViewMatrix);

                    modifyLeftView = false;
                }

                return leftViewMatrix; 
            }
            set 
            { 
                leftViewMatrix = value;
                modifyLeftView = false;
            }
        }

        /// <summary>
        /// Gets or sets the view matrix of the right eye.
        /// </summary>
        public virtual Matrix RightView
        {
            get 
            {
                if (modifyRightView)
                {
                    Vector3 rightVec = Vector3.UnitX;
                    Vector3 upVec = Vector3.UnitY;
                    Vector3 target = -Vector3.UnitZ;

                    Matrix rotMat = Matrix.CreateFromQuaternion(rotation);
                    Vector3 camPos = translation + (rightVec * (interpupillaryDistance / 2 + interpupillaryShift));

                    Vector3.Transform(ref target, ref rotMat, out target);
                    Vector3.Transform(ref upVec, ref rotMat, out upVec);
                    target += camPos;

                    Matrix.CreateLookAt(ref camPos, ref target, ref upVec, out rightViewMatrix);

                    modifyRightView = false;
                }

                return rightViewMatrix; 
            }
            set 
            { 
                rightViewMatrix = value;
                modifyRightView = false;
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix of the left eye.
        /// </summary>
        public virtual Matrix LeftProjection
        {
            get 
            {
                if (modifyLeftProjection)
                {
                    float top = zNearPlane * (float)Math.Tan(fieldOfView / 2);
                    float right = aspectRatio * top;
                    float frustumshift = (interpupillaryDistance / 2) * zNearPlane / focalLength;
                    float left = -right - frustumshift;
                    right = right - frustumshift;

                    leftProjectionMatrix = Matrix.CreatePerspectiveOffCenter(left, right,
                        -top, top, zNearPlane, zFarPlane);

                    modifyLeftProjection = false;
                }

                return leftProjectionMatrix;
            }
            set 
            { 
                leftProjectionMatrix = value;
                modifyLeftProjection = false;
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix of the right eye.
        /// </summary>
        public virtual Matrix RightProjection
        {
            get 
            {
                if (modifyRightProjection)
                {
                    float top = zNearPlane * (float)Math.Tan(fieldOfView / 2);
                    float right = aspectRatio * top;
                    float frustumshift = (interpupillaryDistance / 2) * zNearPlane / focalLength;
                    float left = -right + frustumshift;
                    right = right + frustumshift;

                    rightProjectionMatrix = Matrix.CreatePerspectiveOffCenter(left, right,
                        -top, top, zNearPlane, zFarPlane);

                    modifyRightProjection = false;
                }

                return rightProjectionMatrix;
            }
            set 
            { 
                rightProjectionMatrix = value;
                modifyRightProjection = false;
            }
        }
        #endregion

        #region Override Properties
        public override Matrix View
        {
            get
            {
                return base.View;
            }
            set
            {
                throw new GoblinException("You need to set both RightView and LeftView for StereoCamera");
            }
        }

        public override Matrix Projection
        {
            get
            {
                return base.Projection;
            }
            set
            {
                base.Projection = value;

                /*Matrix dummyView = Matrix.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
                BoundingFrustum frustum = new BoundingFrustum(dummyView * Projection);
                Vector3[] corners = frustum.GetCorners();

                float left = corners[0].X;
                float right = corners[1].X;
                float bottom = corners[2].Y;
                float top = corners[0].Y;
                float near = Math.Abs(corners[0].Z);
                float far = Math.Abs(corners[4].Z);

                float frustumshift = (interpupillaryDistance / 2) * near / focalLength;

                LeftProjection = Matrix.CreatePerspectiveOffCenter(left - frustumshift, right - frustumshift,
                    bottom, top, near, far);
                RightProjection = Matrix.CreatePerspectiveOffCenter(left + frustumshift, right + frustumshift,
                    bottom, top, near, far);*/

                if(modifyLeftProjection)
                    LeftProjection = value;
                if(modifyRightProjection)
                    RightProjection = value;
                modifyLeftProjection = false;
                modifyRightProjection = false;
            }
        }

        public override Vector3 Translation
        {
            get
            {
                return base.Translation;
            }
            set
            {
                base.Translation = value;
                modifyLeftView = true;
                modifyRightView = true;
            }
        }

        public override Quaternion Rotation
        {
            get
            {
                return base.Rotation;
            }
            set
            {
                base.Rotation = value;
                modifyLeftView = true;
                modifyRightView = true;
            }
        }
        #endregion 

        #region Protected Methods
        protected void ModifyAll()
        {
            modifyLeftView = true;
            modifyRightView = true;
            modifyLeftProjection = true;
            modifyRightProjection = true;
        }
        #endregion

        #region Override Methods

#if !WINDOWS_PHONE
        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = base.Save(xmlDoc);

            xmlNode.SetAttribute("InterpupillaryDistance", interpupillaryDistance.ToString());
            xmlNode.SetAttribute("InterpupillaryShift", interpupillaryShift.ToString());
            xmlNode.SetAttribute("FocalLength", focalLength.ToString());

            return xmlNode;
        }

        public override void Load(XmlElement xmlNode)
        {
            base.Load(xmlNode);

            if (xmlNode.HasAttribute("InterpupillaryDistance"))
                interpupillaryDistance = float.Parse(xmlNode.GetAttribute("InterpupillaryDistance"));
            if (xmlNode.HasAttribute("InterpupillaryShift"))
                interpupillaryShift = float.Parse(xmlNode.GetAttribute("InterpupillaryShift"));
            if (xmlNode.HasAttribute("FocalLength"))
                focalLength = float.Parse(xmlNode.GetAttribute("FocalLength"));
        }
#endif

        #endregion
    }
}
