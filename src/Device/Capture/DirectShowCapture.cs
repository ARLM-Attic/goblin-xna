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
 *          Mike Sorvillo
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace GoblinXNA.Device.Capture
{
    /// <summary>
    /// A video capture class that uses the DirectShow library.
    /// </summary>
    public class DirectShowCapture : IVideoCapture
    {
        #region Member Fields

        /// <summary>
        /// Video capture class for DirectShow
        /// </summary>
        private DCapture capture;

        private PointF focalPoint;

        private int videoDeviceID;
        private int audioDeviceID;

        private int cameraWidth;
        private int cameraHeight;
        private int imageSize;
        private IntPtr cameraImage;
        private bool grayscale;
        private bool cameraInitialized;
        private Resolution resolution;
        private FrameRate frameRate;
        private int[] imageData;

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

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a video capture using the DirectShow library.
        /// </summary>
        public DirectShowCapture()
        {
            cameraInitialized = false;
            videoDeviceID = -1;
            audioDeviceID = -1;
            focalPoint = new PointF(0, 0);

            cameraWidth = 0;
            cameraHeight = 0;
            cameraImage = IntPtr.Zero;
            grayscale = false;

            failureCount = 0;
        }

        #endregion

        #region Properties

        public int Width
        {
            get { return cameraWidth; }
        }

        public int Height
        {
            get { return cameraHeight; }
        }

        public PointF FocalPoint
        {
            get { return focalPoint; }
            set { focalPoint = value; }
        }

        public int VideoDeviceID
        {
            get { return videoDeviceID; }
        }

        public int AudioDeviceID
        {
            get { return audioDeviceID; }
        }

        public bool GrayScale
        {
            get { return grayscale; }
        }

        public bool Initialized
        {
            get { return cameraInitialized; }
        }

        public IntPtr ImagePtr
        {
            get { return cameraImage; }
        }

        #endregion

        #region Public Methods

        public void InitVideoCapture(int videoDeviceID, int audioDeviceID, FrameRate framerate,
            Resolution resolution, bool grayscale)
        {
            if (cameraInitialized)
                return;

            this.resolution = resolution;
            this.grayscale = grayscale;
            this.frameRate = framerate;
            this.videoDeviceID = videoDeviceID;
            this.audioDeviceID = audioDeviceID;

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
 
            imageData = new int[cameraWidth * cameraHeight];
            imageSize = cameraWidth * cameraHeight * ((grayscale) ? 1 : 3);
            cameraImage = Marshal.AllocHGlobal(imageSize);

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

            cameraInitialized = true;
        }

        public int[] GetImageTexture(bool returnImage, bool copyToImagePtr)
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
                if (copyToImagePtr && returnImage)
                    ReadBmpData(data, cameraImage, imageData, image.Width, image.Height);
                else if(copyToImagePtr)
                    ReadBmpData(data, cameraImage, image.Width, image.Height);
                else if(returnImage)
                    ReadBmpData(data, imageData, image.Width, image.Height);

                image.UnlockBits(data);
            }
            else
            {
                failureCount++;

                if (failureCount > FAILURE_THRESHOLD)
                {
                    throw new GoblinException("Video capture device id:" + videoDeviceID + " is used by " +
                        "other application, and can not be accessed");
                }
            }

            return imageData;
        }

        /// <summary>
        /// Displays video capture device property information on a Windows Form object.
        /// </summary>
        /// <param name="frmOwner">The owner to hold this property form</param>
        public void ShowVideoCaptureDeviceProperties(System.Windows.Forms.Form frmOwner)
        {
            capture.PropertyPages[0].Show(frmOwner);
        }

        /// <summary>
        /// Displayes video capture pin information on a Windows Form object.
        /// </summary>
        /// <param name="frmOwner"></param>
        public void ShowVideoCapturePin(System.Windows.Forms.Form frmOwner)
        {
            capture.PropertyPages[1].Show(frmOwner);
        }

        /// <summary>
        /// Starts video recording, and saves the recorded video in a given file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>If previous video capturing has not been stopped, then false is returned. Otherwise, true.</returns>
        public bool StartVideoCapturing(String filename)
        {
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
        public void StopVideoCapturing()
        {
            if (!capture.Stopped)
                capture.Stop();
        }

        /// <summary>
        /// Gets a Bitmap image object returned by the DirectShow library.
        /// </summary>
        /// <returns></returns>
        public Bitmap GetBitmapImage()
        {
            if (capture == null)
                return null;

            return tmpBitmap;
        }

        public void Dispose()
        {
            if (capture != null)
                capture.Dispose();
        }

        #endregion

        #region Private Methods

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

                    src -= (width * 3);
                    dst += (width * 3);
                }
            }
        }

        private void ReadBmpData(BitmapData bmpDataSource, int[] imageData, int width, int height)
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

        private void ReadBmpData(BitmapData bmpDataSource, IntPtr cam_image, int width, int height)
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
                    }

                    src -= (width * 3);
                    dst += (width * 3);
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

        #endregion
    }
}
