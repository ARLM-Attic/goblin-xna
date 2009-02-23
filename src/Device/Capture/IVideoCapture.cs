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
using System.Drawing;

using GoblinXNA;

namespace GoblinXNA.Device.Capture
{
    #region Enums
    /// <summary>
    /// The resolution of the camera. In the format of _[width]x[height].
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

    /// <summary>
    /// A video capture interface for accessing cameras. Any video decoding class should implement this interface.
    /// </summary>
    public interface IVideoCapture
    {
        /// <summary>
        /// Gets the camera width in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the camera height in pixels.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the focal point of this camera.
        /// </summary>
        PointF FocalPoint { get; set; }

        /// <summary>
        /// Gets the video device ID.
        /// </summary>
        int VideoDeviceID { get; }

        /// <summary>
        /// Gets the audio device ID.
        /// </summary>
        int AudioDeviceID { get; }

        /// <summary>
        /// Gets whether to use grayscale.
        /// </summary>
        bool GrayScale { get; }

        /// <summary>
        /// Gets whether the device is initialized.
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// Gets the image pointer to the camera image. 
        /// </summary>
        /// <remarks>
        /// This pointer is valid only if 'copyToImagePtr' parameter is set to true when
        /// GetImageTexture(..) function is called. 
        /// </remarks>
        /// <see cref="GetImageTexture"/>
        IntPtr ImagePtr { get; }

        /// <summary>
        /// Initializes the video capture device with the specific video and audio device ID,
        /// desired frame rate and image resolution, and whether to use grayscaled image rather 
        /// than color image. 
        /// </summary>
        /// <remarks>
        /// If the camera supports either only color or grayscale, then the grayscale parameter 
        /// does not affect the output
        /// </remarks>
        /// <param name="videoDeviceID">The actual video device ID assigned by the OS. It's usually 
        /// determined in the order of time that they were plugged in to the computer. For example, 
        /// the first video capture device plugged into the computer is assigned ID of 0, and the next 
        /// one is assigned ID of 1. If you're using the cameras embedded on a laptop or other mobile PC, 
        /// usually the front camera is assigned ID of 0, and the back camera is assigned ID of 1.</param>
        /// <param name="audioDeviceID">The device ID of the audio if audio is available. Set this
        /// to 0 if you don't need the audio input.</param>
        /// <param name="frameRate">The desired framerate to use</param>
        /// <param name="resolution">The resolution of the live video image to use. Some resolution is
        /// not supported by certain cameras, and an exception will be thrown in that case</param>
        /// <param name="grayscale">Indicates whether to use grayscale mode. If the camera only supports 
        /// black & white, then this must be set to false. Otherwise, an exception will be thrown</param>
        void InitVideoCapture(int videoDeviceID, int audioDeviceID, FrameRate frameRate, 
            Resolution resolution, bool grayscale);

        /// <summary>
        /// Gets an array of video image pixels in Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgr32 
        /// format. The size is CameraWidth * CameraHeight.
        /// </summary>
        /// <param name="copyToImagePtr">Whether to copy the video image to the ImagePtr as well so that the
        /// marker tracker library can use it to process the image and detect marker transformations</param>
        /// <param name="returnImage">Whether to return the image in int[] format.</param>
        /// <remarks>
        /// Both 'returnImage' and 'copyToImagePtr' parameters are used for optimization. For example,
        /// if there is no need to return the texture image for drawing background, then there is no
        /// need to perform the conversion from the original format to int[] format (this can be 
        /// computationally expensive). Same for the image pointer copy.
        /// </remarks>
        /// <returns></returns>
        int[] GetImageTexture(bool returnImage, bool copyToImagePtr);

        /// <summary>
        /// Disposes the video capture device.
        /// </summary>
        void Dispose();
    }
}
