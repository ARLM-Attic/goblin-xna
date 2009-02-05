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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 *          Mike Sorvillo
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Reference for the DirectShow Library for C# originally from
// http://www.codeproject.com/cs/media/directxcapture.asp
// Update of this original library with capability of capture individual frame from
// http://www.codeproject.com/cs/media/DirXVidStrm.asp?df=100&forumid=73014&exp=0&select=1780522
using DirectX.Capture;
using DCapture = DirectX.Capture.Capture;

using GoblinXNA.Helpers;

namespace GoblinXNA.Device.Capture
{
    /// <summary>
    /// A video camera interface for accessing most web cameras, including Point Grey cameras.
    /// </summary>
    public sealed class VideoCapture : IDisposable
    {
        #region Enums
        /// <summary>
        /// The resolution of the camera
        /// </summary>
        public enum Resolution{ 
            _160x120, 
            _320x240, 
            _640x480, 
            _800x600, 
            _1024x768, 
            _1280x960, 
            _1600x1200 
        };

        /// <summary>
        /// The framerate of the camera
        /// </summary>
        public enum FrameRate
        {
            _15Hz,
            _30Hz,
            _50Hz,
            _60Hz,
            _120Hz,
            _240Hz
        };
        #endregion

        private GoblinEnums.CameraLibraryType libType;
        /// <summary>
        /// Video capture class for DirectShow
        /// </summary>
        private DCapture capture;
        /// <summary>
        /// Video capture class for Point Grey API
        /// </summary>
        private PGRFlyCapture flyCapture;
        private PGRFlyModule.FlyCaptureVideoMode flyVideoMode;

        private int cameraWidth;
        private int cameraHeight;
        private int imageSize;
        private IntPtr cameraImage;
        private bool grayscale;
        private bool cameraInitialized;
        private bool captureInitialized;
        private Resolution resolution;
        private FrameRate frameRate;
        private int[] imageData;

        private int deviceID;

        /// <summary>
        /// Used to count the number of times it failed to capture an image
        /// If it fails more than certain times, it will assume that the video
        /// capture device can not be accessed
        /// </summary>
        private int failureCount;

        private const int FAILURE_THRESHOLD = 100;

        /// <summary>
        /// A temporary panel for grabbing the video frame from DirectShow interface
        /// </summary>
        private Panel tmpPanel;
        private Bitmap tmpBitmap;

        /// <summary>
        /// Creates a video capture object that captures a live video stream through
        /// various type of video cameras
        /// </summary>
        public VideoCapture()
        {
            captureInitialized = false;
            cameraInitialized = false;
            flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_ANY;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the camera width in pixels
        /// </summary>
        public int Width
        {
            get
            {
                return cameraWidth;
            }
        }

        /// <summary>
        /// Gets or sets the camera height in pixels
        /// </summary>
        public int Height
        {
            get
            {
                return cameraHeight;
            }
        }

        /// <summary>
        /// Gets the ID of this video capture device.
        /// </summary>
        public int DeviceID
        {
            get { return deviceID; }
        }

        /// <summary>
        /// Gets or sets the camera driver 
        /// </summary>
        public GoblinEnums.CameraLibraryType LibType
        {
            get
            {
                return libType;
            }
        }

        /// <summary>
        /// Gets or sets whether to use grayscale
        /// </summary>
        public bool GrayScale
        {
            get
            {
                return grayscale;
            }
        }

        /// <summary>
        /// Gets the image pointer to the camera image.
        /// </summary>
        internal IntPtr ImagePtr
        {
            get { return cameraImage; }
        }

        /// <summary>
        /// Gets whether the camera is initialized
        /// </summary>
        public bool CameraInitialized
        {
            get
            {
                return cameraInitialized;
            }
        }

        internal bool CaptureInitialized
        {
            get { return captureInitialized; }
        }

        /// <summary>
        /// Gets the camera model of a Point Grey camera
        /// </summary>
        /// <remarks>
        /// Only accessible if LibType is set to GoblinEnums.CameraLibraryType.PGRFly
        /// </remarks>
        public PGRFlyModule.FlyCaptureCameraModel PGRCameraModel
        {
            get
            {
                if (libType != GoblinEnums.CameraLibraryType.PGRFly)
                    throw new GoblinException("You are not using PGRFly capture API");

                return flyCapture.CameraModel;
            }
        }

        /// <summary>
        /// Gets the camera type (e.g., b&w, color) of a Point Grey camera
        /// </summary>
        /// <remarks>
        /// Only accessible if LibType is set to GoblinEnums.CameraLibraryType.PGRFly
        /// </remarks>
        public PGRFlyModule.FlyCaptureCameraType PGRCameraType
        {
            get
            {
                if (libType != GoblinEnums.CameraLibraryType.PGRFly)
                    throw new GoblinException("You are not using PGRFly capture API");

                return flyCapture.CameraType;
            }
        }

        /// <summary>
        /// Sets the video mode of a Point Grey camera
        /// </summary>
        public PGRFlyModule.FlyCaptureVideoMode PGRVideoMode
        {
            set { flyVideoMode = value; }
        }
        #endregion

        /// <summary>
        /// Initializes the video capture device with the desired image resolution and video
        /// decoding library.
        /// </summary>
        /// <param name="resolution">The desired resolution to use</param>
        /// <param name="grayscale">Whether to use grayscale mode</param>
        /// <param name="libType">The video decoding library to use</param>
        public void InitVideoCapture(Resolution resolution, bool grayscale, 
            GoblinEnums.CameraLibraryType libType)
        {
            if (captureInitialized)
                return;

            this.resolution = resolution;

            switch (resolution)
            {
                case Resolution._160x120:
                    cameraWidth = 160;
                    cameraHeight = 120;
                    break;
                case Resolution._320x240:
                    cameraWidth = 320;
                    cameraHeight = 240;
                    break;
                case Resolution._640x480:
                    cameraWidth = 640;
                    cameraHeight = 480;
                    break;
                case Resolution._800x600:
                    cameraWidth = 800;
                    cameraHeight = 600;
                    break;
                case Resolution._1024x768:
                    cameraWidth = 1024;
                    cameraHeight = 768;
                    break;
                case Resolution._1280x960:
                    cameraWidth = 1280;
                    cameraHeight = 960;
                    break;
                case Resolution._1600x1200:
                    cameraWidth = 1600;
                    cameraHeight = 1200;
                    break;
            }

            this.grayscale = grayscale;
            this.libType = libType;

            imageData = new int[cameraWidth * cameraHeight];
            failureCount = 0;

            imageSize = cameraWidth * cameraHeight * ((grayscale) ? 1 : 3);
            cameraImage = Marshal.AllocHGlobal(imageSize);
            captureInitialized = true;
        }

        /// <summary>
        /// Initializes the video capture device with the desired image resolution using
        /// the DirectShow decoding library.
        /// </summary>
        /// <param name="resolution">The desired resolution to use</param>
        /// <param name="grayscale">Whether to use grayscale mode</param>
        public void InitVideoCapture(Resolution resolution, bool grayscale)
        {
            InitVideoCapture(resolution, grayscale, GoblinEnums.CameraLibraryType.DirectShow);
        }

        /// <summary>
        /// Displays video capture device property information on a Windows Form object.
        /// </summary>
        /// <param name="frmOwner">The owner to hold this property form</param>
        public void ShowVideoCaptureDeviceProperties(System.Windows.Forms.Form frmOwner)
        {
            if (libType != GoblinEnums.CameraLibraryType.DirectShow)
                throw new GoblinException("You need to use DirectShow libType to view the " +
                    "video capture device properties");

            capture.PropertyPages[0].Show(frmOwner);
        }

        /// <summary>
        /// Displayes video capture pin information on a Windows Form object.
        /// </summary>
        /// <param name="frmOwner"></param>
        public void ShowVideoCapturePin(System.Windows.Forms.Form frmOwner)
        {
            if (libType != GoblinEnums.CameraLibraryType.DirectShow)
                throw new GoblinException("You need to use DirectShow libType to view the " +
                    "video capture device properties");

            capture.PropertyPages[1].Show(frmOwner);
        }

        /// <summary>
        /// Initializes the camera that uses video and audio device '0', and desired frame 
        /// rate of 30 Hz.
        /// </summary>
        /// <returns>Camera Status: 0 -- Successful, -1 -- Camera Exception, -2 -- Camera not Connected
        /// -3 -- Already initialized</returns>
        /// <exception cref="GoblinException"></exception>
        public int InitCamera()
        {
            return InitCamera(0, 0);
        }

        /// <summary>
        /// Initializes the camera with the specified video and audio device ID, and desired frame 
        /// rate of 30 Hz.
        /// </summary>
        /// <param name="videoDeviceID"></param>
        /// <param name="audioDeviceID"></param>
        /// <returns>Camera Status: 0 -- Successful, -1 -- Camera Exception, -2 -- Camera not Connected
        /// -3 -- Already initialized</returns>
        /// <exception cref="GoblinException"></exception>
        public int InitCamera(int videoDeviceID, int audioDeviceID)
        {
            return InitCamera(videoDeviceID, audioDeviceID, FrameRate._30Hz);
        }

        /// <summary>
        /// Initializes the camera with the specified video and audio device ID, and desired frame rate.
        /// </summary>
        /// <param name="videoDeviceID"></param>
        /// <param name="audioDeviceID"></param>
        /// <param name="frameRate"></param>
        /// <returns>Camera Status: 0 -- Successful, -1 -- Camera Exception, -2 -- Camera not Connected
        /// -3 -- Already initialized</returns>
        /// <exception cref="GoblinException"></exception>
        public int InitCamera(int videoDeviceID, int audioDeviceID, FrameRate frameRate)
        {
            if (!captureInitialized)
                throw new GoblinException("Initialize Video capture device first using InitVideoCapture() method");

            if (cameraInitialized)
                return -3;

            this.frameRate = frameRate;
            deviceID = videoDeviceID;

            if (libType == GoblinEnums.CameraLibraryType.DirectShow)
            {
                Filters filters = null;
                Filter videoDevice, audioDevice;
                try
                {
                    filters = new Filters();
                }
                catch (Exception exp)
                {
                    throw new GoblinException("No video capturing devices are found");
                }

                try
                {
                    videoDevice = (videoDeviceID >= 0) ? filters.VideoInputDevices[videoDeviceID] : null;
                }
                catch (Exception exp)
                {
                    throw new GoblinException("VideoDeviceID " + videoDeviceID + " is out of the range");
                }

                try
                {
                    audioDevice = (audioDeviceID >= 0) ? filters.AudioInputDevices[audioDeviceID] : null;
                }
                catch (Exception exp)
                {
                    throw new GoblinException("AudioDeviceID " + audioDeviceID + " is out of the range");
                }
                
                capture = new DCapture(videoDevice, audioDevice);

                double frame_rate = 0;
                switch (frameRate)
                {
                    case FrameRate._15Hz: frame_rate = 15; break;
                    case FrameRate._30Hz: frame_rate = 29.99; break;
                    case FrameRate._50Hz: frame_rate = 50; break;
                    case FrameRate._60Hz: frame_rate = 60; break;
                    case FrameRate._120Hz: frame_rate = 120; break;
                    case FrameRate._240Hz: frame_rate = 240; break;
                }

                if (videoDevice != null)
                {
                    // Using MPEG compressor
                    capture.VideoCompressor = filters.VideoCompressors[2]; 
                    capture.FrameRate = frame_rate;
                    try
                    {
                        capture.FrameSize = new Size(cameraWidth, cameraHeight);
                    }
                    catch(Exception exp)
                    {
                        throw new GoblinException("Resolution._" + cameraWidth + "x" + cameraHeight +
                            " is not supported. Maximum resolution supported is " + 
                            capture.VideoCaps.MaxFrameSize);
                    }
                }
                if (audioDevice != null)
                {
                    capture.AudioCompressor = filters.AudioCompressors[1];
                    capture.AudioSamplingRate = 44100;
                    capture.AudioSampleSize = 16;
                }
                tmpPanel = new Panel();
                tmpPanel.Size = new Size(Width, Height);
                try
                {
                    capture.PreviewWindow = tmpPanel;
                }
                catch (Exception exp)
                {
                    throw new GoblinException("Video capture device " + videoDeviceID + " is used by " +
                        "other application, and can not be accessed");
                }
                capture.FrameEvent2 += new DCapture.HeFrame(CaptureDone);
                capture.GrapImg();
            }
            else if (libType == GoblinEnums.CameraLibraryType.PGRFly)
            {
                flyCapture = new PGRFlyCapture();

                PGRFlyModule.FlyCaptureFrameRate flyFrameRate = 
                    PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_ANY;
                switch (frameRate)
                {
                    case FrameRate._15Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_15;
                        break;
                    case FrameRate._30Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_30;
                        break;
                    case FrameRate._50Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_50;
                        break;
                    case FrameRate._60Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_60;
                        break;
                    case FrameRate._120Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_120;
                        break;
                    case FrameRate._240Hz:
                        flyFrameRate = PGRFlyModule.FlyCaptureFrameRate.FLYCAPTURE_FRAMERATE_240;
                        break;
                }

                PGRFlyModule.FlyCaptureVideoMode flyVideoMode =
                    PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_ANY;
                switch (resolution)
                {
                    case Resolution._160x120:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_160x120YUV444;
                        break;
                    case Resolution._320x240:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_320x240YUV422;
                        break;
                    case Resolution._640x480:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_640x480Y8;
                        break;
                    case Resolution._800x600:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_800x600Y8;
                        break;
                    case Resolution._1024x768:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_1024x768Y8;
                        break;
                    case Resolution._1280x960:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_1280x960Y8;
                        break;
                    case Resolution._1600x1200:
                        flyVideoMode = PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_1600x1200Y8;
                        break;
                }

                if (!this.flyVideoMode.Equals(PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_ANY))
                    flyVideoMode = this.flyVideoMode;

                flyCapture.Initialize(videoDeviceID, flyFrameRate, flyVideoMode, grayscale);
            }

            cameraInitialized = true;
            return 0;
        }

        /// <summary>
        /// Starts video recording, and saves the recorded video in a given file
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <param name="filename"></param>
        /// <returns></returns>
        internal bool StartVideoCapturing(String filename)
        {
            if (libType != GoblinEnums.CameraLibraryType.DirectShow)
                return false;

            if (!capture.Stopped)
                return false;

            capture.Filename = filename;

            if (!capture.Cued)
                capture.Cue();

            capture.Start();

            return true;
        }

        /// <summary>
        /// Stops the video capturing.
        /// </summary>
        internal void StopVideoCapturing()
        {
            if (libType != GoblinEnums.CameraLibraryType.DirectShow)
                return;

            if (!capture.Stopped)
                capture.Stop();
        }

        /// <summary>
        /// Gets a Bitmap image object returned by the DirectShow library.
        /// </summary>
        /// <remarks>
        /// Use this method only if the DirectShow library is used.
        /// </remarks>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Bitmap GetBitmapImage()
        {
            if (libType != GoblinEnums.CameraLibraryType.DirectShow)
                throw new ArgumentException("You should call this method only if you're using DirectShow");

            if (capture == null)
                return null;
            
            return tmpBitmap;
        }

        /// <summary>
        /// Gets a FlyCaptureImage object returned by the PGRFly library.
        /// </summary>
        /// <remarks>
        /// Use this method only if the PGRFly (Point Grey FlyCapture) library is used.
        /// </remarks>
        /// <param name="camImage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public PGRFlyModule.FlyCaptureImage GetPGRFlyImage(IntPtr camImage)
        {
            if (libType != GoblinEnums.CameraLibraryType.PGRFly)
                throw new ArgumentException("You can't call this method if you're not using firefly/" +
                    "dragonfly cameras");

            return flyCapture.GrabRGBImage(camImage);
        }

        /// <summary>
        /// Gets an array of video image pixels in Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgr32 
        /// format. The size is CameraWidth * CameraHeight.
        /// </summary>
        /// <returns></returns>
        public int[] GetImageTexture(bool copyToImagePtr)
        {
            if (libType == GoblinEnums.CameraLibraryType.PGRFly)
            {
                PGRFlyModule.FlyCaptureImage flyImage = GetPGRFlyImage(cameraImage);

                int B = 0, G = 1, R = 2;
                if (PGRCameraModel == PGRFlyModule.FlyCaptureCameraModel.FLYCAPTURE_DRAGONFLY2)
                {
                    B = 2;
                    R = 0;
                }

                unsafe
                {
                    if (flyImage.pData != null)
                    {
                        failureCount = 0;
                        if (copyToImagePtr)
                            cameraImage = (IntPtr)flyImage.pData;

                        int index = 0;
                        for (int i = 0; i < flyImage.iRows; i++)
                        {
                            for (int j = 0; j < flyImage.iRowInc; j += 3)
                            {
                                imageData[(flyImage.iRows - i - 1) * flyImage.iCols + j / 3] =
                                    (*(flyImage.pData + index + j + R) << 16) |
                                    (*(flyImage.pData + index + j + G) << 8) |
                                    *(flyImage.pData + index + j + B);
                            }
                            index += flyImage.iRowInc;
                        }
                    }
                    else
                    {
                        Log.Write("Failed to capture image", Log.LogLevel.Log);
                        failureCount++;

                        if (failureCount > FAILURE_THRESHOLD)
                        {
                            throw new GoblinException("Video capture device id:" + deviceID +
                                " is used by other application, and can not be accessed");
                        }
                    }
                }
            }
            else if (libType == GoblinEnums.CameraLibraryType.DirectShow)
            {
                Bitmap image = GetBitmapImage();

                if (image != null)
                {
                    failureCount = 0;
                    BitmapData data = image.LockBits(
                        new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.ReadOnly, image.PixelFormat);

                    // convert the Bitmap pixel format, that is right to left and
                    // bottom to top, to artag pixel format, that is right to left and
                    // top to bottom
                    if (copyToImagePtr)
                        ReadBmpData(data, cameraImage, imageData, image.Width, image.Height);
                    else
                        ReadBmpData(data, imageData, image.Width, image.Height);

                    image.UnlockBits(data);
                }
                else
                {
                    failureCount++;

                    if (failureCount > FAILURE_THRESHOLD)
                    {
                        throw new GoblinException("Video capture device id:" + deviceID + " is used by " +
                            "other application, and can not be accessed");
                    }
                }
            }

            return imageData;
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

                        imageData[i * width + j / 3] = (*(dst + j + 2) << 16) |
                            (*(dst + j + 1) << 8) | *(dst + j);
                    }
                    src -= (width * 3);
                    dst += (width * 3);
                }
            }
        }

        private void ReadBmpData(BitmapData bmpDataSource, int[] imageData, int width,
            int height)
        {
            unsafe
            {
                byte* src = (byte*)bmpDataSource.Scan0;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width * 3; j += 3)
                    {
                        imageData[i * width + j / 3] = (*(src + j + 2) << 16) |
                            (*(src + j + 1) << 8) | *(src + j);
                    }
                    src -= (width * 3);
                }
            }
        }

        /// <summary>
        /// Assigns the video image returned by the DirectShow library to a temporary Bitmap holder.
        /// </summary>
        /// <param name="e"></param>
        private void CaptureDone(System.Drawing.Bitmap e)
        {
            this.tmpBitmap = e;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (libType == GoblinEnums.CameraLibraryType.DirectShow)
            {
                if (capture != null)
                    capture.Dispose();
            }
            else if (libType == GoblinEnums.CameraLibraryType.PGRFly)
                flyCapture.Dispose();

            cameraInitialized = false;
        }

        #endregion
    }
}
