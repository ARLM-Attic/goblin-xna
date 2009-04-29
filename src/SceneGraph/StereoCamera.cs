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
    /// This class represents a stereo camera view. Stereo only works if proper shutter glass
    /// and graphics card are used.
    /// </summary>
    public class StereoCamera : Camera
    {
        #region Enums
        /// <summary>
        /// 
        /// </summary>
        /*public enum DisplayMethod
        {
            Swapping,
            LineSpacing
        }

        public enum StereoMethod
        {
            OffAxis,
            ToeIn
        }*/
        #endregion

        #region Member Fields

        protected float interpupillaryDistance;
        //protected float screenDistance;

        //protected StereoMethod stereoMethod;
        //protected DisplayMethod displayMethod;
        protected Matrix leftViewMatrix;
        protected Matrix rightViewMatrix;
        protected Matrix leftProjectionMatrix;
        protected Matrix rightProjectionMatrix;

        protected bool modifyLeftView;
        protected bool modifyRightView;

        protected bool useUserDefinedLeftProjection;
        protected bool useUserDefinedRightProjection;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a stereo camera.
        /// </summary>
        public StereoCamera()
        {
            interpupillaryDistance = 1.0f;
            //screenDistance = 5.0f;

            //stereoMethod = StereoMethod.OffAxis;
            //displayMethod = DisplayMethod.Swapping;
            leftViewMatrix = Matrix.Identity;
            rightViewMatrix = Matrix.Identity;
            leftProjectionMatrix = Matrix.Identity;
            rightProjectionMatrix = Matrix.Identity;

            modifyLeftView = false;
            modifyRightView = false;

            useUserDefinedLeftProjection = false;
            useUserDefinedRightProjection = false;
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
            //screenDistance = source.ScreenDistance;

            //stereoMethod = source.CurrentStereoMethod;
            //displayMethod = source.CurrentDisplayMethod;
            leftViewMatrix = source.LeftView;
            rightViewMatrix = source.RightView;
            leftProjectionMatrix = source.LeftProjection;
            rightProjectionMatrix = source.RightProjection;

            modifyLeftView = false;
            modifyRightView = false;

            useUserDefinedLeftProjection = source.UseUserDefinedLeftProjection;
            useUserDefinedRightProjection = source.UseUserDefinedRightProjection;
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
                //RecomputeProjection();
            }
        }

        /*/// <summary>
        /// Gets or sets the distance
        /// </summary>
        public virtual float ScreenDistance
        {
            get { return screenDistance; }
            set 
            { 
                screenDistance = value;
                RecomputeProjection();
            }
        }

        /// <summary>
        /// Gets or sets the current stereo method.
        /// </summary>
        public virtual StereoMethod CurrentStereoMethod
        {
            get { return stereoMethod; }
            set { stereoMethod = value; }
        }

        /// <summary>
        /// Gets or sets the current display method.
        /// </summary>
        public virtual DisplayMethod CurrentDisplayMethod
        {
            get { return displayMethod; }
            set { displayMethod = value; }
        }*/

        /// <summary>
        /// Gets or sets the view matrix of the left eye.
        /// </summary>
        public virtual Matrix LeftView
        {
            get 
            {
                if (modifyLeftView)
                {
                    Matrix leftCameraTransformation = Matrix.CreateFromQuaternion(rotation) *
                        Matrix.CreateTranslation(translation - Vector3.UnitX * interpupillaryDistance / 2);
                    Vector3 location = translation - Vector3.UnitX * interpupillaryDistance / 2;
                    Vector3 target = new Vector3(0.0f, 0.0f, -1.0f);
                    target = Vector3.Transform(target, leftCameraTransformation);
                    Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
                    up = Vector3.Transform(up, leftCameraTransformation);
                    up = Vector3.Subtract(up, location);
                    leftViewMatrix = Matrix.CreateLookAt(location, target, up);

                    modifyLeftView = false;
                }

                return leftViewMatrix; 
            }
            set { leftViewMatrix = value; }
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
                    Matrix rightCameraTransformation = Matrix.CreateFromQuaternion(rotation) *
                        Matrix.CreateTranslation(translation + Vector3.UnitX * interpupillaryDistance / 2);
                    Vector3 location = translation + Vector3.UnitX * interpupillaryDistance / 2;
                    Vector3 target = new Vector3(0.0f, 0.0f, -1.0f);
                    target = Vector3.Transform(target, rightCameraTransformation);
                    Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
                    up = Vector3.Transform(up, rightCameraTransformation);
                    up = Vector3.Subtract(up, location);
                    rightViewMatrix = Matrix.CreateLookAt(location, target, up);

                    modifyRightView = false;
                }

                return rightViewMatrix; 
            }
            set { rightViewMatrix = value; }
        }

        /// <summary>
        /// Gets or sets the projection matrix of the left eye.
        /// </summary>
        public virtual Matrix LeftProjection
        {
            get 
            {
                /*if (stereoMethod == StereoMethod.OffAxis)
                    return leftProjectionMatrix;
                else*/
                if (useUserDefinedLeftProjection)
                    return leftProjectionMatrix;
                else
                    return Projection;
            }
            set 
            {
                useUserDefinedLeftProjection = true;
                leftProjectionMatrix = value; 
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix of the right eye.
        /// </summary>
        public virtual Matrix RightProjection
        {
            get 
            {
                /*if (stereoMethod == StereoMethod.OffAxis)
                    return rightProjectionMatrix;
                else*/
                if (useUserDefinedRightProjection)
                    return rightProjectionMatrix;
                else
                    return Projection;
            }
            set 
            {
                useUserDefinedRightProjection = true;
                rightProjectionMatrix = value; 
            }
        }

        internal bool UseUserDefinedLeftProjection
        {
            get { return useUserDefinedLeftProjection; }
        }

        internal bool UseUserDefinedRightProjection
        {
            get { return useUserDefinedRightProjection; }
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
        /*/// <summary>
        /// Recompute the left and right projection matrix
        /// </summary>
        protected void RecomputeProjection()
        {
            // This off-axis projection matrix calculation is taken from Bespoke framework
            if (stereoMethod == StereoMethod.OffAxis)
            {
                float widthRatio = (float)State.Width / State.Height;
                float wd2 = zNearPlane * (float)Math.Tan(MathHelper.PiOver4 / 2.0);
                float ndfl = zNearPlane / screenDistance;

                float left = -widthRatio * wd2 - 0.5f * interpupillaryDistance * ndfl;
                float right = widthRatio * wd2 - 0.5f * interpupillaryDistance * ndfl;
                float top = wd2;
                float bottom = -wd2;

                leftProjectionMatrix = Matrix.CreatePerspectiveOffCenter(left, right, bottom, 
                    top, zNearPlane, zFarPlane);

                left = -widthRatio * wd2 + 0.5f * interpupillaryDistance * ndfl;
                right = widthRatio * wd2 + 0.5f * interpupillaryDistance * ndfl; ;

                rightProjectionMatrix = Matrix.CreatePerspectiveOffCenter(left, right, bottom, 
                    top, zNearPlane, zFarPlane);
            }
        }*/
        #endregion
    }
}
