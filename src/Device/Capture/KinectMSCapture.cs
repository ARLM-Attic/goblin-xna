/************************************************************************************ 
 * Copyright (c) 2008-2011, Columbia University
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
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using GoblinXNA.Device.Vision;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Research.Kinect.Nui;

namespace GoblinXNA.Device.Capture
{
    /// <summary>
    /// An implementation of IVideoCapture for Kinect depth camera. This implementation uses
    /// Microsoft Kinect SDK.
    /// </summary>
    public class KinectMSCapture : IVideoCapture
    {
        #region Member Fields

        private int videoDeviceID;

        private int cameraWidth;
        private int cameraHeight;
        private bool grayscale;
        private bool cameraInitialized;
        private Resolution resolution;
        private FrameRate frameRate;
        private ImageFormat format;
        private IResizer resizer;

        private ImageReadyCallback imageReadyCallback;

        private Runtime nui;
        private RuntimeOptions options;

        private int[] videoData;
        private byte[] rawVideo;

        private bool displayVideoInDepthSpace;
        private int[,] colorToDepthSpaceMap;
        private bool useHighDefVideo;
        private Rectangle videoInDepthSpaceBound;
        private bool isVideoClipped;

        private bool copyingRawVideo = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a video capture using the Kinect camera with the Microsoft SDK.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="displayVideoInDepthSpace">Indicates whether to display the video image in the depth space. You should
        /// set this to true if you are using the video image with the depth data</param>
        /// <param name="useHighDefVideo">Indicates whether to display the depth mapped video in high defintion.
        /// The regular depth mapped video will be 320x240, but if this is set to true, a 640x480 video will
        /// be produced using the depth mapped clipping bound</param>
        public KinectMSCapture(RuntimeOptions options, bool displayVideoInDepthSpace, bool useHighDefVideo)
        {
            cameraInitialized = false;
            videoDeviceID = -1;

            this.displayVideoInDepthSpace = displayVideoInDepthSpace;
            this.useHighDefVideo = useHighDefVideo;

            cameraWidth = 0;
            cameraHeight = 0;
            grayscale = false;

            imageReadyCallback = null;

            this.options = options | RuntimeOptions.UseColor;
        }

        public KinectMSCapture(RuntimeOptions options, bool displayVideoInDepthSpace)
            : this(options, displayVideoInDepthSpace, false)
        {
        }

        public KinectMSCapture()
            : this(RuntimeOptions.UseColor, false, false)
        {
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

        public int VideoDeviceID
        {
            get { return videoDeviceID; }
        }

        public bool GrayScale
        {
            get { return grayscale; }
        }

        public bool Initialized
        {
            get { return cameraInitialized; }
        }

        public ImageFormat Format
        {
            get { return format; }
        }

        public IResizer MarkerTrackingImageResizer
        {
            get { return resizer; }
            set { resizer = value; }
        }

        public SpriteEffects RenderFormat
        {
            get { return SpriteEffects.None; }
        }

        public ImageReadyCallback CaptureCallback
        {
            set { imageReadyCallback = value; }
        }

        public Runtime NuiRuntime
        {
            get { return nui; }
        }

        /// <summary>
        /// Gets or sets whether this Kinect capture is currently used for calibration purpose only.
        /// If you're performing calibration using CameraCalibration project, make sure to set this
        /// to true.
        /// </summary>
        public bool UsedForCalibration
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets whether to mirror the video image. Note that this will affect the image pointer
        /// which is used for vision tracking as well.
        /// </summary>
        public bool MirrorImage
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the bound of the video frame mapped to the depth space. This property is valid 
        /// only if 'displayVideoInDepthSpace' parameter in the constructor is passed as 'true'.
        /// </summary>
        public Rectangle VideoInDepthSpaceBound
        {
            get { return videoInDepthSpaceBound; }
        }

        /// <summary>
        /// Gets whether the depth space mapped video is clipped due to the video and IR camera mis-alignment.
        /// This property is is valid only if 'displayVideoInDepthSpace' parameter in the constructor is 
        /// passed as 'true'.
        /// </summary>
        public bool IsVideoClipped
        {
            get { return isVideoClipped; }
        }

        #endregion

        #region Public Methods

        public void InitVideoCapture(int videoDeviceID, FrameRate framerate, Resolution resolution,
            ImageFormat format, bool grayscale)
        {
            if (cameraInitialized)
                return;

            this.resolution = resolution;
            this.grayscale = grayscale;
            this.frameRate = framerate;
            this.videoDeviceID = videoDeviceID;
            this.format = format;

            ImageResolution res = ImageResolution.Invalid;

            switch (resolution)
            {
                case Resolution._160x120:
                    cameraWidth = 160;
                    cameraHeight = 120;
                    res = ImageResolution.Resolution80x60;
                    break;
                case Resolution._320x240:
                    cameraWidth = 320;
                    cameraHeight = 240;
                    res = ImageResolution.Resolution320x240;
                    break;
                case Resolution._640x480:
                    cameraWidth = 640;
                    cameraHeight = 480;
                    res = ImageResolution.Resolution640x480;
                    break;
                case Resolution._800x600:
                    cameraWidth = 800;
                    cameraHeight = 600;
                    break;
                case Resolution._1024x768:
                    cameraWidth = 1024;
                    cameraHeight = 768;
                    break;
                case Resolution._1280x1024:
                    cameraWidth = 1280;
                    cameraHeight = 1024;
                    res = ImageResolution.Resolution1280x1024;
                    break;
                case Resolution._1600x1200:
                    cameraWidth = 1600;
                    cameraHeight = 1200;
                    break;
            }

            if (res == ImageResolution.Invalid)
                throw new GoblinException(resolution.ToString() + " is not supported by Kinect video");

            if (displayVideoInDepthSpace)
            {
                res = ImageResolution.Resolution640x480;
                if (useHighDefVideo)
                {
                    
                }
                else
                {
                    cameraWidth = 320;
                    cameraHeight = 240;
                }
            }

            nui = new Runtime(videoDeviceID);

            nui.Initialize(options);
            
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(VideoImageReady);
            
            nui.VideoStream.Open(ImageStreamType.Video, 2, res, ImageType.Color);

            if (UsedForCalibration || displayVideoInDepthSpace)
                videoData = new int[cameraWidth * cameraHeight];

            if (displayVideoInDepthSpace)
            {
                ImageViewArea iv = new ImageViewArea();
                int colorX = 0, colorY = 0;
                colorToDepthSpaceMap = new int[240, 320];
                Point min = new Point(int.MaxValue, int.MaxValue);
                Point max = new Point(-1, -1);
                for (int i = 0; i < 240; ++i)
                {
                    for (int j = 0; j < 320; ++j)
                    {
                        nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(res,
                            iv, j, i, (short)0, out colorX, out colorY);

                        if (colorX < min.X)
                            min.X = colorX;
                        if (colorX > max.X)
                            max.X = colorX;
                        if (colorY < min.Y)
                            min.Y = colorY;
                        if (colorY > max.Y)
                            max.Y = colorY;

                        if (colorY >= nui.VideoStream.Height)
                        {
                            colorY = nui.VideoStream.Height - 1;
                            isVideoClipped = true;
                        }
                        if (colorX >= nui.VideoStream.Width)
                        {
                            colorX = nui.VideoStream.Width - 1;
                            isVideoClipped = true;
                        }
                        colorToDepthSpaceMap[i, j] = colorY * nui.VideoStream.Width + colorX;
                    }
                }

                videoInDepthSpaceBound = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            }

            cameraInitialized = true;
        }

        /// <summary>
        /// Recomputes the mapping between the video and the depth coordinate using the updated depth values.
        /// </summary>
        /// <param name="depthData"></param>
        public void ReComputeVideoInDepthMapping(float[] depthData)
        {
            ImageViewArea iv = new ImageViewArea();
            int colorX = 0, colorY = 0, index = 0;
            Point min = new Point(int.MaxValue, int.MaxValue);
            Point max = new Point(-1, -1);
            for (int i = 0; i < 240; ++i)
            {
                for (int j = 0; j < 320; ++j, ++index)
                {
                    nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(nui.VideoStream.Resolution,
                        iv, j, i, (short)((short)depthData[index] << 3), out colorX, out colorY);

                    if (colorX < min.X)
                        min.X = colorX;
                    if (colorX > max.X)
                        max.X = colorX;
                    if (colorY < min.Y)
                        min.Y = colorY;
                    if (colorY > max.Y)
                        max.Y = colorY;

                    if (colorY >= nui.VideoStream.Height)
                    {
                        colorY = nui.VideoStream.Height - 1;
                        isVideoClipped = true;
                    }
                    if (colorX >= nui.VideoStream.Width)
                    {
                        colorX = nui.VideoStream.Width - 1;
                        isVideoClipped = true;
                    }
                    colorToDepthSpaceMap[i, j] = colorY * nui.VideoStream.Width + colorX;
                }
            }

            videoInDepthSpaceBound = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
        }

        public void GetImageTexture(int[] returnImage, ref IntPtr imagePtr)
        {
            if (returnImage != null)
            {
                if (UsedForCalibration || displayVideoInDepthSpace)
                    Buffer.BlockCopy(videoData, 0, returnImage, 0, videoData.Length * sizeof(int));
                else
                    videoData = returnImage;
            }

            if (imagePtr != IntPtr.Zero)
            {
                copyingRawVideo = true;
                switch (format)
                {
                    case ImageFormat.B8G8R8A8_32:
                        Marshal.Copy(videoData, 0, imagePtr, videoData.Length);
                        break;
                    case ImageFormat.R8G8B8_24:
                        unsafe
                        {
                            byte* dst = (byte*)imagePtr;
                            int color = 0;
                            int dstIndex = 0;
                            for(int i = 0; i < videoData.Length; ++i, dstIndex += 3)
                            {
                                color = videoData[i];
                                *(dst + dstIndex) = (byte)(color >> 16);
                                *(dst + dstIndex + 1) = (byte)(color >> 8);
                                *(dst + dstIndex + 2) = (byte)(color);
                            }
                        }
                        break;
                }
                copyingRawVideo = false;
            }
        }

        public void Dispose()
        {
            if (nui != null)
                nui.Uninitialize();
        }

        #endregion

        #region Private Methods

        private void VideoImageReady(object sender, ImageFrameReadyEventArgs e)
        {
            if (!UsedForCalibration && videoData == null)
                return;

            ImageFrame videoFrame = e.ImageFrame;

            int length = videoFrame.Image.Height * videoFrame.Image.Width;

            while (copyingRawVideo) { }
            rawVideo = videoFrame.Image.Bits;

            int videoIndex = 0;
            int index = 0;

            if (displayVideoInDepthSpace)
            {
                if (useHighDefVideo)
                {
                    for (int i = 0; i < length; ++i, videoIndex += videoFrame.Image.BytesPerPixel)
                    {
                        videoData[i] = (int)(rawVideo[videoIndex] << 16 | rawVideo[videoIndex + 1] << 8 |
                            rawVideo[videoIndex + 2]);
                    }

                    
                }
                else
                {
                    if (MirrorImage)
                    {
                        for (int i = 0; i < cameraHeight; ++i)
                        {
                            for (int j = 0; j < cameraWidth; ++j, ++index)
                            {
                                videoIndex = colorToDepthSpaceMap[i, (cameraWidth - j - 1)] *
                                    videoFrame.Image.BytesPerPixel;
                                videoData[index] = (int)(rawVideo[videoIndex] << 16 |
                                    rawVideo[videoIndex + 1] << 8 | rawVideo[videoIndex + 2]);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < cameraHeight; ++i)
                        {
                            for (int j = 0; j < cameraWidth; ++j, ++index)
                            {
                                videoIndex = colorToDepthSpaceMap[i, j] * videoFrame.Image.BytesPerPixel;
                                videoData[index] = (int)(rawVideo[videoIndex] << 16 |
                                    rawVideo[videoIndex + 1] << 8 | rawVideo[videoIndex + 2]);
                            }
                        }
                    }
                }
            }
            else
            {
                if (MirrorImage)
                {
                    for (int i = 0; i < videoFrame.Image.Height; ++i)
                    {
                        index = i * videoFrame.Image.Width;
                        for (int j = videoFrame.Image.Width - 1; j >= 0; --j, 
                            videoIndex += videoFrame.Image.BytesPerPixel)
                            videoData[index + j] = (int)(rawVideo[videoIndex] << 16 | rawVideo[videoIndex + 1] << 8 |
                                rawVideo[videoIndex + 2]);
                    }
                }
                else
                {
                    for (int i = 0; i < length; ++i, videoIndex += videoFrame.Image.BytesPerPixel)
                    {
                        videoData[i] = (int)(rawVideo[videoIndex] << 16 | rawVideo[videoIndex + 1] << 8 |
                            rawVideo[videoIndex + 2]);
                    }
                }
            }

            if (imageReadyCallback != null)
                imageReadyCallback(IntPtr.Zero, videoData);
        }

        #endregion
    }
}
