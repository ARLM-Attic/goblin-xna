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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using GoblinXNA.Helpers;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision.Marker;
using ImageFormat = GoblinXNA.Device.Capture.ImageFormat;

namespace GoblinXNA.Device.Vision.Feature
{
    /// <summary>
    /// A feature tracker implementation using the ALVAR (http://virtual.vtt.fi/virtual/proj2/multimedia/) 
    /// library developed by VTT. This only works for ALVAR 2.0.0 and above.
    /// </summary>
    public class ALVARFeatureTracker : IMarkerTracker
    {
        #region Member Fields

        private Matrix lastMarkerMatrix;

        private Matrix camProjMat;

        private double cameraFovX;
        private double cameraFovY;

        private bool initialized;

        private String configFilename;

        private float zNearPlane;
        private float zFarPlane;

        private int colorChannel;

        private double[] poseMats;
        private double inlierRatio;
        private int mappedPoints;
        private bool featureFound;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an ALVAR marker tracker.
        /// </summary>
        public ALVARFeatureTracker()
        {
            configFilename = "";

            lastMarkerMatrix = Matrix.Identity;
            MinInlierRatio = 0.15;
            MinMappedPoints = 4;

            cameraFovX = 0;
            cameraFovY = 0;

            camProjMat = Matrix.Identity;
            initialized = false;

            zNearPlane = 0.1f;
            zFarPlane = 1000;

            colorChannel = 0;

            poseMats = new double[16];

            EnableTracking = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the camera's horizontal field of view in radians.
        /// </summary>
        public double CameraFovX
        {
            get { return cameraFovX; }
        }

        /// <summary>
        /// Gets the camera's vertical field of view in radians.
        /// </summary>
        public double CameraFovY
        {
            get { return cameraFovY; }
        }

        /// <summary>
        /// Default value is 0.15.
        /// </summary>
        public double MinInlierRatio
        {
            get;
            set;
        }

        /// <summary>
        /// Default value is 4.
        /// </summary>
        public int MinMappedPoints
        {
            get;
            set;
        }

        public bool Initialized
        {
            get { return initialized; }
        }

        public Matrix CameraProjection
        {
            get { return camProjMat; }
        }

        /// <summary>
        /// Gets or sets the near clipping plane used to compute CameraProjection.
        /// The default value is 0.1f.
        /// </summary>
        /// <remarks>
        /// This property should be set before calling InitTracker(...).
        /// </remarks>
        public float ZNearPlane
        {
            get { return zNearPlane; }
            set
            {
                if (initialized)
                    throw new MarkerException("You need to set this property before initialization");

                zNearPlane = value;
            }
        }

        /// <summary>
        /// Gets or sets the far clipping plane used to compute CameraProjection.
        /// The default value is 1000.
        /// </summary>
        /// <remarks>
        /// This property should be set before calling InitTracker(...).
        /// </remarks>
        public float ZFarPlane
        {
            get { return zFarPlane; }
            set
            {
                if (initialized)
                    throw new MarkerException("You need to set this property before initialization");

                zFarPlane = value;
            }
        }

        public bool EnableTracking
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initilizes the marker tracker with a set of configuration parameters.
        /// </summary>
        /// <param name="configs">
        /// There are two ways to pass the parameters. One way is to pass in the order of
        /// (int imageWidth, int imageHeight, String cameraCalibFilename, double markerSize,
        /// int markerRes, double margin), and the other way is (int imageWidth, int imageHeight, 
        /// String cameraCalibFilename, double markerSize).
        /// </param>
        public void InitTracker(params Object[] configs)
        {
            if (configs.Length != 3)
                throw new MarkerException(GetInitTrackerUsage());

            int img_width = 0;
            int img_height = 0;

            try
            {
                img_width = (int)configs[0];
                img_height = (int)configs[1];
                configFilename = (String)configs[2];
            }
            catch (Exception)
            {
                throw new MarkerException(GetInitTrackerUsage());
            }

            ALVARDllBridge.alvar_init();

            int ret = ALVARDllBridge.alvar_add_camera(configFilename, img_width, img_height);
            if (ret < 0)
                throw new MarkerException("Camera calibration file is either not specified or not found");

            ALVARDllBridge.alvar_add_fern_estimator(configFilename, img_width, img_height);

            double[] projMat = new double[16];
            ALVARDllBridge.alvar_get_camera_params(0, projMat, ref cameraFovX, ref cameraFovY, zFarPlane, zNearPlane);
            camProjMat = new Matrix(
                (float)projMat[0], (float)projMat[1], (float)projMat[2], (float)projMat[3],
                (float)projMat[4], (float)projMat[5], (float)projMat[6], (float)projMat[7],
                (float)projMat[8], (float)projMat[9], (float)projMat[10], (float)projMat[11],
                (float)projMat[12], (float)projMat[13], (float)projMat[14], (float)projMat[15]);

            initialized = true;
        }

        /// <summary>
        /// Associates a marker with an identifier so that the identifier can be used to find this
        /// marker after processing the image. 
        /// </summary>
        /// <param name="markerConfigs">There are three ways to pass the parameters; (int markerID),
        /// (int markerID, double markerSize), or (String multiMarkerConfig). </param>
        /// <returns>An identifier for this marker object</returns>
        public Object AssociateMarker(params Object[] markerConfigs)
        {
            // make sure we are initialized
            if (!initialized)
                throw new MarkerException("ALVARFeatureTracker is not initialized. Call InitTracker(...)");

            if (markerConfigs.Length != 1)
                throw new MarkerException(GetAssocMarkerUsage());

            Object id = null;

            if (markerConfigs[0] is string)
            {
                String classifierName = (String)markerConfigs[0];
                if (classifierName.Equals(""))
                    throw new MarkerException(GetAssocMarkerUsage());
                else
                {
                    if (ALVARDllBridge.alvar_add_feature_detector(classifierName) != 0)
                        throw new MarkerException(classifierName + " is not found or not an appropriate .dat file");
                    id = classifierName;
                }
            }
            else
                throw new MarkerException(GetAssocMarkerUsage());

            return id;
        }

        /// <summary>
        /// Processes the video image captured from an initialized video capture device. 
        /// </summary>
        /// <param name="captureDevice">An initialized video capture device</param>
        public void ProcessImage(IVideoCapture captureDevice, IntPtr imagePtr)
        {
            String channelSeq = "";
            int nChannles = 1;
            switch(captureDevice.Format)
            {
                case ImageFormat.R5G6B5_16:
                case ImageFormat.R8G8B8_24:
                    channelSeq = "RGB";
                    nChannles = 3;
                    break;
                case ImageFormat.R8G8B8A8_32:
                    channelSeq = "RGBA";
                    nChannles = 4;
                    break;
                case ImageFormat.B8G8R8_24:
                    channelSeq = "BGR";
                    nChannles = 3;
                    break;
                case ImageFormat.B8G8R8A8_32:
                    channelSeq = "BGRA";
                    nChannles = 4;
                    break;
                case ImageFormat.A8B8G8R8_32:
                    channelSeq = "ARGB";
                    nChannles = 4;
                    break;
            }

            featureFound = ALVARDllBridge.alvar_detect_feature(0, nChannles, channelSeq, channelSeq, 
                imagePtr, MinInlierRatio, MinMappedPoints, ref inlierRatio, ref mappedPoints);

            if (featureFound)
            {
                ALVARDllBridge.alvar_get_feature_pose(poseMats);

                lastMarkerMatrix = new Matrix(
                    (float)poseMats[0], (float)poseMats[1], (float)poseMats[2], (float)poseMats[3],
                    (float)poseMats[4], (float)poseMats[5], (float)poseMats[6], (float)poseMats[7],
                    (float)poseMats[8], (float)poseMats[9], (float)poseMats[10], (float)poseMats[11],
                    (float)poseMats[12], (float)poseMats[13], (float)poseMats[14], (float)poseMats[15]);
            }
        }

        /// <summary>
        /// Checks whether a marker identified by 'markerID' is found in the processed image
        /// after calling ProcessImage(...) method.
        /// </summary>
        /// <param name="markerID">An ID associated with a marker returned from AssociateMarker(...)
        /// method.</param>
        /// <returns>A boolean value representing whether a marker was found</returns>
        public bool FindMarker(Object markerID)
        {
            return featureFound;
        }

        /// <summary>
        /// Gets the pose transformation of the found marker after calling the FindMarker(...) method.
        /// </summary>
        /// <remarks>
        /// This method should be called if and only if FindMarker(...) method returned true for
        /// the marker you're looking for. 
        /// </remarks>
        /// <returns>The pose transformation of a found marker</returns>
        public Matrix GetMarkerTransform()
        {
            return lastMarkerMatrix;
        }

        /// <summary>
        /// Disposes this marker tracker.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }

        #endregion

        #region Private Methods

        private String GetInitTrackerUsage()
        {
            return "Usage: InitTracker(int imgWidth, int imgHeight, String cameraCalibFilename)";
        }

        private String GetAssocMarkerUsage()
        {
            return "Usage: AssociateMarker(String classifierFilename)";
        }

        #endregion
    }
}
