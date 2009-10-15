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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;

using GoblinXNA.Helpers;
using GoblinXNA.Device.Capture;
using ImageFormat = GoblinXNA.Device.Capture.ImageFormat;

namespace GoblinXNA.Device.Vision.Marker
{
    /// <summary>
    /// A marker tracker implementation using the ALVAR (http://virtual.vtt.fi/virtual/proj2/multimedia/) 
    /// library developed by VTT.
    /// </summary>
    public class ALVARMarkerTracker : IMarkerTracker
    {
        #region Member Fields

        private Dictionary<int, Matrix> detectedMarkers;
        private Dictionary<String, Matrix> detectedMultiMarkers;

        private List<int> singleMarkerIDs;
        private IntPtr singleMarkerIDsPtr;
        private List<String> multiMarkerIDs;
        private int multiMarkerID;

        private int img_width;
        private int img_height;
        private float camera_fx;
        private float camera_fy;

        private Matrix lastMarkerMatrix;
        private double max_marker_error;
        private double max_track_error;

        private int[] imageData;
        private IntPtr cam_image;
        private Matrix camProjMat;

        private double cameraFovX;
        private double cameraFovY;

        private bool initialized;

        private String configFilename;
        private String imageFilename;
        private bool staticImageProcessed;

        private float zNearPlane;
        private float zFarPlane;

        private bool hideMarkers;
        private List<object> hideList;
        private bool hideMarkerConfigured;
        private int textureSize;
        private int colorChannel;
        private Dictionary<object, int[]> hideTextureMap;

        private int[] ids;
        private double[] poseMats;
        private IntPtr idPtr;
        private IntPtr posePtr;
        private IntPtr hideTexturePtr;
        private int prevMarkerNum;

        private int[] multiIDs;
        private double[] multiPoseMats;
        private double[] multiErrors;
        private IntPtr multiIdPtr;
        private IntPtr multiPosePtr;
        private IntPtr multiHideTexturePtr;
        private IntPtr multiErrorPtr;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an ALVAR marker tracker.
        /// </summary>
        public ALVARMarkerTracker()
        {
            img_width = 0;
            img_height = 0;
            camera_fx = 0;
            camera_fy = 0;
            configFilename = "";
            imageFilename = "";
            staticImageProcessed = false;

            lastMarkerMatrix = Matrix.Identity;
            max_marker_error = 0.08;
            max_track_error = 0.2;

            cameraFovX = 0;
            cameraFovY = 0;

            cam_image = IntPtr.Zero;
            camProjMat = Matrix.Identity;
            imageData = null;
            initialized = false;

            zNearPlane = 10;
            zFarPlane = 2000;

            detectedMarkers = new Dictionary<int, Matrix>();
            detectedMultiMarkers = new Dictionary<string, Matrix>();

            singleMarkerIDs = new List<int>();
            singleMarkerIDsPtr = IntPtr.Zero;
            multiMarkerIDs = new List<string>();
            multiMarkerID = 0;

            hideMarkers = false;
            hideList = null;
            hideMarkerConfigured = false;
            textureSize = 0;
            colorChannel = 0;
            hideTextureMap = new Dictionary<object, int[]>();

            ids = null;
            poseMats = null;
            prevMarkerNum = 0;
            idPtr = IntPtr.Zero;
            posePtr = IntPtr.Zero;
            hideTexturePtr = IntPtr.Zero;

            multiIDs = null;
            multiPoseMats = null;
            multiErrors = null;
            multiIdPtr = IntPtr.Zero;
            multiPosePtr = IntPtr.Zero;
            multiHideTexturePtr = IntPtr.Zero;
            multiErrorPtr = IntPtr.Zero;
        }

        #endregion

        #region Properties

        public float CameraFx
        {
            get { return camera_fx; }
        }

        public float CameraFy
        {
            get { return camera_fy; }
        }

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

        public int ImageWidth
        {
            get { return img_width; }
        }

        public int ImageHeight
        {
            get { return img_height; }
        }

        /// <summary>
        /// Default value is 0.08.
        /// </summary>
        public double MaxMarkerError
        {
            get { return max_marker_error; }
            set { max_marker_error = value; }
        }

        /// <summary>
        /// Default value is 0.2.
        /// </summary>
        public double MaxTrackError
        {
            get { return max_track_error; }
            set { max_track_error = value; }
        }

        public bool Initialized
        {
            get { return initialized; }
        }

        /// <summary>
        /// Gets or sets the static image used for tracking. JPEG, GIF, and BMP formats are
        /// supported.
        /// </summary>
        /// <remarks>
        /// You need to set this value if you want to perform marker tracking using a
        /// static image instead of a live video stream. 
        /// </remarks>
        /// <exception cref="GoblinException">Either if tracker is not initialized, or if the
        /// image format is not supported.</exception>
        public String StaticImageFile
        {
            get { return imageFilename; }
            set
            {
                if (!value.Equals(imageFilename))
                {
                    imageFilename = value;

                    // make sure we are initialized
                    if (!initialized)
                        throw new MarkerException("ALVARTracker is not initialized. Call InitTracker(...)");

                    String fileType = Path.GetExtension(imageFilename);

                    int bpp = 0;
                    int w = 0, h = 0;

                    if (fileType.Equals(".jpg") || fileType.Equals(".jpeg") ||
                        fileType.Equals(".gif") || fileType.Equals(".bmp"))
                    {
                        Bitmap image = new Bitmap(imageFilename);

                        w = image.Width;
                        h = image.Height;

                        BitmapData data = image.LockBits(
                            new System.Drawing.Rectangle(0, 0, w, h),
                            ImageLockMode.ReadOnly, image.PixelFormat);

                        imageData = new int[w * h];
                        int imageSize = w * h * 3;
                        cam_image = Marshal.AllocHGlobal(imageSize);

                        ReadBmpData(data, cam_image, imageData, w, h);

                        image.UnlockBits(data);
                    }
                    else
                        throw new MarkerException("We currently do not support reading images other " +
                            "than .jpg, .jpeg, .gif, and .bmp format");

                    // if the image size is different from the previously configured size, then we
                    // need to re-initialize the ARTag tracker. 
                    if ((w != img_width) || (h != img_height))
                        ALVARDllBridge.alvar_init_camera(configFilename, w, h);

                    staticImageProcessed = false;
                }
            }
        }

        public int[] StaticImage
        {
            get { return imageData; }
        }

        public Matrix CameraProjection
        {
            get
            {
                return camProjMat;
            }
        }

        /// <summary>
        /// Gets or sets the near clipping plane used to compute CameraProjection.
        /// The default value is 10.
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
        /// The default value is 2000.
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

        /// <summary>
        /// Gets or sets whether to hide the detected markers with a texture that resembles the
        /// surrounding texture in the video image. The default value is false.
        /// </summary>
        /// <remarks>
        /// You must call SetHideMarkerTextureConfigurations(...) method to configure the settings
        /// of the hide texture. If not configured, the markers won't be hidden. 
        /// 
        /// By default, all of the detected markers are hidden, but you can choose to hide only 
        /// specified markers by setting HideList property.
        /// </remarks>
        /// <see cref="SetHideMarkerTextureConfigurations"/>
        /// <seealso cref="HideList"/>
        internal bool HideMarkers
        {
            get { return hideMarkers; }
            set 
            { 
                hideMarkers = value; 
                hideTexturePtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the textures used for hiding the markers.
        /// </summary>
        internal Dictionary<object, int[]> HideTextures
        {
            get { return hideTextureMap; }
        }

        /// <summary>
        /// Gets or sets the list of markers to hide. The list can contain either int (for single
        /// marker) or String (for multiple markers). If null, which is the default, all detected
        /// markers will be hiden if HideMarkers is set to true.
        /// </summary>
        /// <see cref="HideMarkers"/>
        internal List<object> HideList
        {
            get { return hideList; }
            set { hideList = value; }
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
            if (!(configs.Length == 4 || configs.Length == 6))
                throw new MarkerException(GetInitTrackerUsage());

            int markerRes = 5;
            double markerSize = 1, margin = 2;
            if (configs.Length == 4)
            {
                try
                {
                    img_width = (int)configs[0];
                    img_height = (int)configs[1];
                    configFilename = (String)configs[2];
                    markerSize = (double)configs[3];
                }
                catch (Exception)
                {
                    throw new MarkerException(GetInitTrackerUsage());
                }
            }
            else
            {
                try
                {
                    img_width = (int)configs[0];
                    img_height = (int)configs[1];
                    configFilename = (String)configs[2];
                    markerSize = (double)configs[3];
                    markerRes = (int)configs[4];
                    margin = (double)configs[5];
                }
                catch (Exception)
                {
                    throw new MarkerException(GetInitTrackerUsage());
                }
            }

            int ret = ALVARDllBridge.alvar_init_camera(configFilename, img_width, img_height);
            if (ret < 0)
                Log.Write("camera calibration file is either not specified or not found");

            double[] projMat = new double[16];
            ALVARDllBridge.alvar_get_camera_params(projMat, ref cameraFovX, ref cameraFovY);
            camProjMat = new Matrix(
                (float)projMat[0], (float)projMat[1], (float)projMat[2], (float)projMat[3],
                (float)projMat[4], (float)projMat[5], (float)projMat[6], (float)projMat[7],
                (float)projMat[8], (float)projMat[9], (float)projMat[10], (float)projMat[11],
                (float)projMat[12], (float)projMat[13], (float)projMat[14], (float)projMat[15]);

            ALVARDllBridge.alvar_init_marker_detector(markerSize, markerRes, margin);

            initialized = true;
        }

        /// <summary>
        /// Sets the configurations for textures used to hide the markers. 
        /// </summary>
        /// <param name="size">The pixel width and height of the texture.</param>
        /// <param name="depth"></param>
        /// <param name="channels">The number of color channels.</param>
        /// <param name="margin"></param>
        internal void SetHideMarkerTextureConfigurations(uint size, uint depth, uint channels, 
            double margin)
        {
            ALVARDllBridge.alvar_set_hide_texture_configuration(size, depth, channels, margin);
            textureSize = (int)(size * size);
            colorChannel = (int)channels;
            hideMarkerConfigured = true;
        }

        /// <summary>
        /// Associates a marker with an identifier so that the identifier can be used to find this
        /// marker after processing the image. 
        /// </summary>
        /// <param name="markerConfigs">There are three ways to pass the parameters; (int markerID),
        /// (int markerID, double markerSize), or (String multiMarkerConfig, int[] ids). </param>
        /// <returns>An identifier for this marker object</returns>
        public Object AssociateMarker(params Object[] markerConfigs)
        {
            // make sure we are initialized
            if (!initialized)
                throw new MarkerException("ALVARMarkerTracker is not initialized. Call InitTracker(...)");

            if (!(markerConfigs.Length == 1 || markerConfigs.Length == 2))
                throw new MarkerException(GetAssocMarkerUsage());

            Object id = null;

            if (markerConfigs.Length == 1)
            {
                if (!(markerConfigs[0] is int))
                    throw new MarkerException(GetAssocMarkerUsage());
                else
                    id = markerConfigs[0];
            }
            else
            {
                try
                {
                    if (markerConfigs[0] is int)
                    {
                        id = markerConfigs[0];
                        int markerID = (int)markerConfigs[0];
                        double markerSize = (double)markerConfigs[1];
                        ALVARDllBridge.alvar_set_marker_size(markerID, markerSize);
                        singleMarkerIDs.Add(markerID);
                        if (hideMarkers && hideMarkerConfigured)
                            if((hideList == null) || (hideList != null && hideList.Contains(markerConfigs[0])))
                                hideTextureMap.Add(markerID, new int[textureSize]);

                        singleMarkerIDsPtr = Marshal.AllocHGlobal(singleMarkerIDs.Count * sizeof(int));
                        unsafe
                        {
                            int* dest = (int*)singleMarkerIDsPtr;
                            for (int i = 0; i < singleMarkerIDs.Count; i++)
                                *(dest + i) = singleMarkerIDs[i];
                        }
                    }
                    else
                    {
                        String markerConfigName = (String)markerConfigs[0];
                        int[] ids = (int[])markerConfigs[1];
                        if (markerConfigName.Equals(""))
                        {
                            ALVARDllBridge.alvar_add_multi_marker_bundle(ids.Length, ids);
                            id = "bundle_" + multiMarkerID;
                        }
                        else
                        {
                            ALVARDllBridge.alvar_add_multi_marker(ids.Length, ids, markerConfigName);
                            id = markerConfigName;
                        }

                        multiMarkerIDs.Add((String)id);
                        multiMarkerID++;

                        multiIdPtr = Marshal.AllocHGlobal(multiMarkerIDs.Count * sizeof(int));
                        multiPosePtr = Marshal.AllocHGlobal(multiMarkerIDs.Count * 16 * sizeof(double));
                        multiErrorPtr = Marshal.AllocHGlobal(multiMarkerIDs.Count * sizeof(double));

                        multiIDs = new int[multiMarkerIDs.Count];
                        multiPoseMats = new double[multiMarkerIDs.Count * 16];
                        multiErrors = new double[multiMarkerIDs.Count];
                    }
                }
                catch (Exception)
                {
                    throw new MarkerException(GetAssocMarkerUsage());
                }
            }

            return id;
        }

        /// <summary>
        /// Processes a static image set in the property.
        /// </summary>
        public void ProcessImage()
        {
            if (!staticImageProcessed)
            {
                if (cam_image == IntPtr.Zero)
                    throw new MarkerException("You either forgot to add your video capture " +
                        "device or didn't set the static image file");

                int interestedMarkerNums = singleMarkerIDs.Count;
                int foundMarkerNums = 0;

                ALVARDllBridge.alvar_detect_marker(3, "RGB", "RGB", cam_image, 
                    singleMarkerIDsPtr, ref foundMarkerNums, ref interestedMarkerNums,
                    max_marker_error, max_track_error);

                Process(interestedMarkerNums, foundMarkerNums);

                staticImageProcessed = true;
            }
        }

        /// <summary>
        /// Processes the video image captured from an initialized video capture device. 
        /// </summary>
        /// <param name="captureDevice">An initialized video capture device</param>
        public void ProcessImage(IVideoCapture captureDevice)
        {
            String channelSeq = "";
            switch(captureDevice.Format)
            {
                case ImageFormat.R5G6B5_16:
                case ImageFormat.R8G8B8_24:
                    channelSeq = "RGB";
                    break;
                case ImageFormat.R8G8B8A8_32:
                    channelSeq = "RGBA";
                    break;
                case ImageFormat.B8G8R8_24:
                    channelSeq = "BGR";
                    break;
                case ImageFormat.B8G8R8A8_32:
                    channelSeq = "BGRA";
                    break;
                case ImageFormat.A8B8G8R8_32:
                    channelSeq = "ARGB";
                    break;
            }

            int interestedMarkerNums = singleMarkerIDs.Count;
            int foundMarkerNums = 0;

            ALVARDllBridge.alvar_detect_marker((captureDevice.GrayScale) ? 1 : 3, "RGB", channelSeq, 
                captureDevice.ImagePtr, singleMarkerIDsPtr, ref foundMarkerNums, ref interestedMarkerNums,
                max_marker_error, max_track_error);

            Process(interestedMarkerNums, foundMarkerNums);
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
            bool found = false;
            if (markerID is int)
            {
                if (detectedMarkers.Count == 0)
                    return false;

                int id = (int)markerID;
                found = detectedMarkers.ContainsKey(id);
                if (found)
                {
                    if (State.IsMultiCore)
                    {
                        try
                        {
                            lastMarkerMatrix = detectedMarkers[id];
                        }
                        catch (Exception)
                        {
                            found = false;
                        }
                    }
                    else
                        lastMarkerMatrix = detectedMarkers[id];
                }
            }
            else
            {
                if (detectedMultiMarkers.Count == 0)
                    return false;

                String id = (String)markerID;
                found = detectedMultiMarkers.ContainsKey(id);
                if (found)
                {
                    if (State.IsMultiCore)
                    {
                        try
                        {
                            lastMarkerMatrix = detectedMultiMarkers[id];
                        }
                        catch (Exception)
                        {
                            found = false;
                        }
                    }
                    else
                        lastMarkerMatrix = detectedMultiMarkers[id];
                }
            }

            return found;
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
            return "Usage: InitTracker(int imgWidth, int imgHeight, String cameraCalibFilename, " +
                "double markerSize, int markerRes, double margin) or InitTracker(int imgWidth, " +
                "int imgHeight, String cameraCalibFilename, double markerSize)";
        }

        private String GetAssocMarkerUsage()
        {
            return "Usage: AssociateMarker(int markerID) or AssociateMarker(int markerID, " +
                "double markerSize) or AssociateMarker(String multiMarkerConfig, int[] ids)";
        }

        /// <summary>
        /// A helper function that extracts the image pixels stored in Bitmap instance to an array
        /// of integers as well as copy them to the memory location pointed by 'cam_image'.
        /// </summary>
        /// <param name="bmpDataSource"></param>
        /// <param name="cam_image"></param>
        /// <param name="imageData"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void ReadBmpData(BitmapData bmpDataSource, IntPtr cam_image, int[] imageData, int width,
            int height)
        {
            unsafe
            {
                byte* src = (byte*)bmpDataSource.Scan0;
                byte* dst = (byte*)cam_image;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width * 3; j += 3)
                    {
                        *(dst + j) = *(src + j);
                        *(dst + j + 1) = *(src + j + 1);
                        *(dst + j + 2) = *(src + j + 2);

                        imageData[i * width + j / 3] = (*(src + j + 2) << 16) |
                            (*(src + j + 1) << 8) | *(src + j);
                    }

                    src += bmpDataSource.Stride;
                    dst += (width * 3);
                }
            }
        }

        private void Process(int interestedMarkerNums, int foundMarkerNums)
        {
            detectedMarkers.Clear();
            detectedMultiMarkers.Clear();

            if (foundMarkerNums <= 0)
                return;

            int id = 0;
            if (interestedMarkerNums > 0)
            {
                if (prevMarkerNum != interestedMarkerNums)
                {
                    ids = new int[interestedMarkerNums];
                    poseMats = new double[interestedMarkerNums * 16];
                    idPtr = Marshal.AllocHGlobal(interestedMarkerNums * sizeof(int));
                    posePtr = Marshal.AllocHGlobal(interestedMarkerNums * 16 * sizeof(double));
                }

                if (hideMarkers && hideMarkerConfigured)
                {
                    if (prevMarkerNum != interestedMarkerNums)
                    {
                        hideTexturePtr = Marshal.AllocHGlobal(interestedMarkerNums * textureSize * 
                            colorChannel * sizeof(byte));
                    }
                    ALVARDllBridge.alvar_get_poses(idPtr, posePtr, true, hideTexturePtr);
                }
                else
                    ALVARDllBridge.alvar_get_poses(idPtr, posePtr, false, IntPtr.Zero);

                prevMarkerNum = interestedMarkerNums;

                Marshal.Copy(idPtr, ids, 0, interestedMarkerNums);
                Marshal.Copy(posePtr, poseMats, 0, interestedMarkerNums * 16);

                for (int i = 0; i < interestedMarkerNums; i++)
                {
                    id = ids[i];

                    // If same marker ID exists, then we ignore the 2nd one
                    if (detectedMarkers.ContainsKey(id))
                    {
                        // do nothing
                    }
                    else
                    {
                        int index = i * 16;
                        Matrix mat = new Matrix(
                            (float)poseMats[index], (float)poseMats[index + 1], (float)poseMats[index + 2], (float)poseMats[index + 3],
                            (float)poseMats[index + 4], (float)poseMats[index + 5], (float)poseMats[index + 6], (float)poseMats[index + 7],
                            (float)poseMats[index + 8], (float)poseMats[index + 9], (float)poseMats[index + 10], (float)poseMats[index + 11],
                            (float)poseMats[index + 12], (float)poseMats[index + 13], (float)poseMats[index + 14], (float)poseMats[index + 15]);
                        detectedMarkers.Add(id, mat);

                        if (hideMarkers && hideMarkerConfigured && hideTextureMap.ContainsKey(id))
                        {
                            unsafe
                            {
                                byte* src = (byte*)hideTexturePtr;
                                src += textureSize * colorChannel * i;

                                int[] textureData = hideTextureMap[id];
                                for (int j = 0; j < textureSize; j++, src += colorChannel)
                                {
                                    textureData[j] = (*(src) << 16) | (*(src + 1) << 8) | *(src + 2);
                                }
                            }
                        }
                    }
                }
            }

            if (multiMarkerIDs.Count == 0)
                return;

            double error = -1;

            ALVARDllBridge.alvar_get_multi_marker_poses(multiIdPtr, multiPosePtr, multiErrorPtr,
                false, IntPtr.Zero);

            Marshal.Copy(multiIdPtr, multiIDs, 0, multiMarkerIDs.Count);
            Marshal.Copy(multiPosePtr, multiPoseMats, 0, multiMarkerIDs.Count * 16);
            Marshal.Copy(multiErrorPtr, multiErrors, 0, multiMarkerIDs.Count);

            for (int i = 0; i < multiMarkerIDs.Count; i++)
            {
                id = multiIDs[i];
                error = multiErrors[i];

                if (error == -1)
                    continue;

                int index = i * 16;
                Matrix mat = new Matrix(
                    (float)multiPoseMats[index], (float)multiPoseMats[index + 1], (float)multiPoseMats[index + 2], (float)multiPoseMats[index + 3],
                    (float)multiPoseMats[index + 4], (float)multiPoseMats[index + 5], (float)multiPoseMats[index + 6], (float)multiPoseMats[index + 7],
                    (float)multiPoseMats[index + 8], (float)multiPoseMats[index + 9], (float)multiPoseMats[index + 10], (float)multiPoseMats[index + 11],
                    (float)multiPoseMats[index + 12], (float)multiPoseMats[index + 13], (float)multiPoseMats[index + 14], (float)multiPoseMats[index + 15]);
                detectedMultiMarkers.Add(multiMarkerIDs[i], mat);
            }
        }

        #endregion
    }
}
