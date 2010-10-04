/************************************************************************************ 
 * Copyright (c) 2008-2010, Columbia University
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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Graphics;

namespace GoblinXNA.Device.Capture
{
    /// <summary>
    /// Creates a dummy capture device that streams a static image.
    /// </summary>
    public class NullCapture : IVideoCapture
    {
        #region Member Fields

        private PointF focalPoint;

        private int videoDeviceID;

        private string imageFilename;

        private int cameraWidth;
        private int cameraHeight;
        private bool grayscale;
        private bool cameraInitialized;

        private ImageFormat format;
        private IResizer resizer;

        private int[] imageData;
        private IntPtr imagePtr;

        private bool isImageAlreadyProcessed;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a video capture using a series of static images.
        /// </summary>
        public NullCapture()
        {
            cameraInitialized = false;
            focalPoint = new PointF(0, 0);
            videoDeviceID = -1;
            imageData = null;
            imagePtr = IntPtr.Zero;
            imageFilename = "";

            cameraWidth = 0;
            cameraHeight = 0;
            grayscale = false;
            isImageAlreadyProcessed = false;
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

        /// <summary>
        /// Gets or sets whether the static image is already processed by the marker tracker.
        /// </summary>
        public bool IsImageAlreadyProcessed
        {
            get { return isImageAlreadyProcessed; }
            set { isImageAlreadyProcessed = value; }
        }

        /// <summary>
        /// Gets or sets the static image used for tracking. JPEG, GIF, and BMP formats are
        /// supported.
        /// </summary>
        /// <remarks>
        /// You need to set this value if you want to perform marker tracking using a
        /// static image instead of a live video stream. 
        /// </remarks>
        /// <exception cref="GoblinException">If the image format is not supported.</exception>
        public String StaticImageFile
        {
            get { return imageFilename; }
            set
            {
                if (!cameraInitialized)
                    throw new GoblinException("You need to initialize the video capture device first");

                if (!value.Equals(imageFilename))
                {
                    imageFilename = value;

                    String fileType = Path.GetExtension(imageFilename);

                    int bpp = 1;
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

                        int imageSize = w * h;
                        imageData = new int[imageSize];
                        switch (format)
                        {
                            case ImageFormat.R5G6B5_16:
                                bpp = 2;
                                break;
                            case ImageFormat.B8G8R8_24:
                            case ImageFormat.R8G8B8_24:
                                bpp = 3;
                                break;
                            case ImageFormat.A8B8G8R8_32:
                            case ImageFormat.B8G8R8A8_32:
                            case ImageFormat.R8G8B8A8_32:
                                bpp = 4;
                                break;
                        }

                        if (resizer != null)
                            imageSize = (int)(imageSize * (resizer.ScalingFactor * resizer.ScalingFactor));

                        imagePtr = Marshal.AllocHGlobal(imageSize * bpp);

                        ReadBmpData(data, imagePtr, imageData, w, h, bpp);

                        image.UnlockBits(data);
                    }
                    else
                        throw new GoblinException("We currently do not support reading images other " +
                            "than .jpg, .jpeg, .gif, and .bmp format");

                    cameraWidth = w;
                    cameraHeight = h;

                    isImageAlreadyProcessed = false;
                }
            }
        }

        #endregion

        #region Public Methods

        public void InitVideoCapture(int videoDeviceID, FrameRate framerate, Resolution resolution, 
            ImageFormat format, bool grayscale)
        {
            if (cameraInitialized)
                return;

            this.videoDeviceID = videoDeviceID;
            this.format = format;
            this.grayscale = grayscale;

            cameraInitialized = true;
        }

        public void GetImageTexture(int[] returnImage, ref IntPtr imagePtr)
        {
            if (imageFilename.Length > 0)
            {
                Array.Copy(imageData, returnImage, returnImage.Length);
                imagePtr = this.imagePtr;
            }
        }

        public void Dispose()
        {
            // imagePtr is freed by Scene class
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
            int height, int bpp)
        {
            byte R, G, B, A;

            if (resizer != null)
            {
                resizer.ResizeImage(bmpDataSource.Scan0, new Vector2(width, height), ref cam_image, bpp);

                unsafe
                {
                    byte* src = (byte*)bmpDataSource.Scan0;
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0, k = 0; j < width * 3; j += 3, k += bpp)
                        {
                            imageData[i * width + j / 3] = (*(src + j + 2) << 16) |
                                (*(src + j + 1) << 8) | *(src + j);
                        }

                        src += bmpDataSource.Stride;
                    }
                }
            }
            else
            {
                unsafe
                {
                    byte* src = (byte*)bmpDataSource.Scan0;
                    byte* dst = (byte*)cam_image;

                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0, k = 0; j < width * 3; j += 3, k += bpp)
                        {
                            switch (format)
                            {
                                case ImageFormat.GRAYSCALE_8:
                                    *(dst + k) = (byte)(0.3 * (*(src + j)) + 0.59 * (*(src + j + 1)) +
                                        +0.11 * (*(src + j + 2)));
                                    break;
                                case ImageFormat.R5G6B5_16:
                                    *(dst + k) = (byte)((*(src + j) & 0xF8) | (*(src + j + 1) >> 5));
                                    *(dst + k + 1) = (byte)(((*(src + j + 1) & 0x1C) << 3) |
                                        ((*(src + j + 2) & 0xF8) >> 3));
                                    break;
                                case ImageFormat.B8G8R8_24:
                                case ImageFormat.R8G8B8_24:
                                    if (format == ImageFormat.R8G8B8_24)
                                    {
                                        R = 0; G = 1; B = 2;
                                    }
                                    else
                                    {
                                        R = 2; G = 1; B = 0;
                                    }

                                    *(dst + k + R) = *(src + j);
                                    *(dst + k + G) = *(src + j + 1);
                                    *(dst + k + B) = *(src + j + 2);
                                    break;
                                case ImageFormat.A8B8G8R8_32:
                                case ImageFormat.B8G8R8A8_32:
                                case ImageFormat.R8G8B8A8_32:
                                    if (format == ImageFormat.A8B8G8R8_32)
                                    {
                                        A = 0; B = 1; G = 2; R = 3;
                                    }
                                    else if (format == ImageFormat.B8G8R8A8_32)
                                    {
                                        B = 0; G = 1; R = 2; A = 3;
                                    }
                                    else
                                    {
                                        R = 0; G = 1; B = 2; A = 3;
                                    }

                                    *(dst + k + A) = (byte)255;
                                    *(dst + k + R) = *(src + j);
                                    *(dst + k + G) = *(src + j + 1);
                                    *(dst + k + B) = *(src + j + 2);
                                    break;
                            }

                            imageData[i * width + j / 3] = (*(src + j + 2) << 16) |
                                (*(src + j + 1) << 8) | *(src + j);
                        }

                        src += bmpDataSource.Stride;
                        dst += (width * bpp);
                    }
                }
            }
        }

        #endregion
    }
}
