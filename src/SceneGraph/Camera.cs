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
using Microsoft.Xna.Framework;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// Camera represents a view frustum (camera) in the 3D space.
    /// It describes how the viewed 3D space is transformed to a 2D surface (render window).
    /// </summary>
    public class Camera
    {
        #region Fields

        protected Vector3 translation;
        protected Quaternion rotation;
        protected Matrix view;
        protected Matrix projection;
        protected Matrix cameraTransformation;
        protected float fieldOfView;
        protected float aspectRatio;
        protected float zNearPlane;
        protected float zFarPlane;

        protected bool modifyView;
        protected bool modifyProjection;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a camera with vertical field of view of 45 degrees, 1.0f near clipping plane and 
        /// 1000f far clipping plane.
        /// </summary>
        public Camera()
        {
            translation = new Vector3(0, 0, 0);
            rotation = Quaternion.Identity;
            view = Matrix.Identity;
            projection = Matrix.Identity;
            
            fieldOfView = (float)Math.PI / 4;
            aspectRatio = State.Width / (float)State.Height;
            zNearPlane = 1.0f;
            zFarPlane = 1000.0f;

            modifyView = false;
            modifyProjection = false;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="cam"></param>
        public Camera(Camera cam)
        {
            translation = cam.Translation;
            rotation = cam.Rotation;
            view = cam.View;
            projection = cam.Projection;
            fieldOfView = cam.FieldOfViewY;
            aspectRatio = cam.AspectRatio;
            zNearPlane = cam.ZNearPlane;
            zFarPlane = cam.ZFarPlane;

            modifyView = false;
            modifyProjection = false;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the position of this camera's view point. The default value is (0, 0, 0).
        /// </summary>
        public virtual Vector3 Translation
        {
            get{ return translation; }
            set
            { 
                translation = value;
                modifyView = true;
                cameraTransformation = Matrix.CreateFromQuaternion(rotation) *
                    Matrix.CreateTranslation(translation);
            }
        }
        /// <summary>
        /// Gets or sets the rotation of this camera's view point. The initial forward vector is
        /// (0, 0, -1) (into the display), and the up vector is (0, 1, 0).
        /// </summary>
        public virtual Quaternion Rotation
        {
            get{ return rotation; }
            set
            { 
                rotation = value;
                modifyView = true;
                cameraTransformation = Matrix.CreateFromQuaternion(rotation) *
                    Matrix.CreateTranslation(translation);
            }
        }

        /// <summary>
        /// Gets or sets the view transform.
        /// </summary>
        public virtual Matrix View
        {
            get
            {
                if (modifyView)
                {
                    Vector3 location = translation;
                    Vector3 target = new Vector3(0.0f, 0.0f, -1.0f);
                    target = Vector3.Transform(target, cameraTransformation);
                    Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
                    up = Vector3.Transform(up, cameraTransformation);
                    up = Vector3.Subtract(up, location);
                    view = Matrix.CreateLookAt(location, target, up);

                    modifyView = false;
                }
                return view; 
            }
            set 
            { 
                view = value;
            }
        }

        /// <summary>
        /// Gets the transformation created by composing Rotation and Translation.
        /// </summary>
        /// <remarks>
        /// = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation)
        /// </remarks>
        /// <see cref="Translation"/>
        /// <seealso cref="Rotation"/>
        public virtual Matrix CameraTransformation
        {
            get { return cameraTransformation; }
        }

        /// <summary>
        /// Gets or sets the projection matrix of this camera.
        /// If this matrix is not set, then the project matrix is calculated by using
        /// the FieldOfViewY, AspectRatio, ZNearPlane, and ZFarPlane. If this matrix is set to a 
        /// matrix, then that matrix will be returned when accessing this value.
        /// </summary>
        public virtual Matrix Projection
        {
            get
            {
                if (modifyProjection)
                {
                    projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView,
                        aspectRatio, zNearPlane, zFarPlane);

                    modifyProjection = false;
                }

                return projection;
            }
            set { projection = value; }
        }

        /// <summary>
        /// Gets or sets the vertical field of view in radians. 
        /// The default value is MathHelper.ToRadians(45)
        /// </summary>
        public virtual float FieldOfViewY 
        {
            get{ return fieldOfView; }
            set
            { 
                fieldOfView = value;
                modifyProjection = true;
            }
        }

        /// <summary>
        /// Gets or sets the aspect ratio. Default value is set to Viewport.Width / Viewport.Height
        /// </summary>
        public virtual float AspectRatio
        {
            get { return aspectRatio; }
            set 
            { 
                aspectRatio = value;
                modifyProjection = true;
            }
        }

        /// <summary>
        /// Gets or sets the distance of the near clipping plane from the view position.
        /// The default value is 1.0f.
        /// </summary>
        public virtual float ZNearPlane 
        {
            get{ return zNearPlane; }
            set
            { 
                zNearPlane = value;
                modifyProjection = true;
            }
        }

        /// <summary>
        /// Gets or sets the distance of the far clipping plane from the view position.
        /// The default value is 1000.0f;
        /// </summary>
        public virtual float ZFarPlane
        {
            get{ return zFarPlane; }
            set
            { 
                zFarPlane = value;
                modifyProjection = true;
            }
        }

        #endregion
    }
}
