//=============================================================================
// Copyright © 2006 Point Grey Research, Inc. All Rights Reserved.
// 
// This software is the confidential and proprietary information of Point
// Grey Research, Inc. ("Confidential Information").  You shall not
// disclose such Confidential Information and shall use it only in
// accordance with the terms of the license agreement you entered into
// with Point Grey Research, Inc. (PGR).
// 
// PGR MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF THE
// SOFTWARE, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE, OR NON-INFRINGEMENT. PGR SHALL NOT BE LIABLE FOR ANY DAMAGES
// SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
// THIS SOFTWARE OR ITS DERIVATIVES.
//=============================================================================
//=============================================================================
//
// Modified by: Ohan Oda (ohan@cs.columbia.edu)
//
//==========================================================================


using System;
using System.Runtime.InteropServices;

namespace GoblinXNA.Device.Capture
{
    /// <summary>
    /// A camera driver class for Point Grey Research firefly/dragonfly cameras.
    /// (This class is provided by Point Grey Research)
    /// </summary>
	internal unsafe class PGRFlyCapture : IDisposable
    {
        #region Structs
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        public struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public RGBQUAD bmiColors;
        }
        #endregion

        #region DLL Imports
        //
		// DLL Functions to import
		// 
		// Follow this format to import any DLL with a specific function.
		//

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureCreateContext(int* flycapcontext);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureStart(int flycapcontext, 
			PGRFlyModule.FlyCaptureVideoMode videoMode,
            PGRFlyModule.FlyCaptureFrameRate frameRate);

		[DllImport("pgrflycapture.dll")]
		public static extern string flycaptureErrorToString(int error);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureInitialize(int flycapContext, 
			int cameraIndex);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureGetCameraInformation(int flycapContext,
            ref PGRFlyModule.FlyCaptureInfo arInfo);

		[DllImport("pgrflycapture.dll")]
		unsafe public static extern int flycaptureGrabImage2(int flycapContext,
            ref PGRFlyModule.FlyCaptureImage image);

		[DllImport("pgrflycapture.dll")]
		unsafe public static extern int flycaptureSaveImage(int flycapContext,
            ref PGRFlyModule.FlyCaptureImage image, string filename, 
			PGRFlyModule.FlyCaptureImageFileFormat fileFormat);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureStop(int flycapContext);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureDestroyContext(int flycapContext);

		[DllImport("pgrflycapture.dll")]
		public static extern int flycaptureConvertImage(int flycapContext,
            ref PGRFlyModule.FlyCaptureImage image, ref PGRFlyModule.FlyCaptureImage imageConvert);
        #endregion

        #region Constants
        // Bitmap constant
		public const short DIB_RGB_COLORS = 0;

		// The maximum number of cameras on the bus.
		public const int _MAX_CAMS = 3;
        #endregion

        #region Variables
        private int flycapContext;
        private int cameraIndex;

        private PGRFlyModule.FlyCaptureInfo flycapInfo;
        private PGRFlyModule.FlyCaptureImage image;
        private PGRFlyModule.FlyCaptureImage flycapRGBImage;
        private PGRFlyModule.FlyCaptureCameraType cameraType;
        private PGRFlyModule.FlyCaptureCameraModel cameraModel;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a camera driver for Point Grey Research firefly/dragong fly cameras.
        /// </summary>
        public PGRFlyCapture()
        {
            flycapInfo = new PGRFlyModule.FlyCaptureInfo();
            image = new PGRFlyModule.FlyCaptureImage();
            flycapRGBImage = new PGRFlyModule.FlyCaptureImage();
        }
        #endregion

        #region Properties
        /// <summary>
        /// The model of the camera
        /// </summary>
        /// <seealso cref="GoblinXNA.Device.Capture.PGRFlyModule"/>
        public PGRFlyModule.FlyCaptureCameraModel CameraModel
        {
            get { return cameraModel; }
        }

        /// <summary>
        /// The type of the camera (e.g., b&w or color)
        /// </summary>
        /// <seealso cref="GoblinXNA.Device.Capture.PGRFlyModule"/>
        public PGRFlyModule.FlyCaptureCameraType CameraType
        {
            get { return cameraType; }
        }
        #endregion

        /// <summary>
        /// Initializes the camera driver
        /// </summary>
        /// <param name="cameraIndex">If only one Point Grey camera is connected, then use '0'. 
        /// If more than one Point Grey cameras connected, then use between '0' and 'number of 
        /// Point Grey cameras connected - 1'</param>
        /// <param name="frameRate">The frame rate you desire</param>
        /// <param name="videoMode"></param>
        /// <param name="grayscale"></param>
        unsafe public void Initialize(int cameraIndex, PGRFlyModule.FlyCaptureFrameRate frameRate, 
            PGRFlyModule.FlyCaptureVideoMode videoMode, bool grayscale)
        {
            this.cameraIndex = cameraIndex;

            int flycapContext;
            int ret;
            // Create the context.
            ret = flycaptureCreateContext(&flycapContext);
            if (ret != 0)
                ReportError(ret, "flycaptureCreateContext");

            // Initialize the camera.
            ret = flycaptureInitialize(flycapContext, cameraIndex);
            if (ret != 0)
                ReportError(ret, "flycaptureInitialize");

            // Get the info for this camera.
            ret = flycaptureGetCameraInformation(flycapContext, ref flycapInfo);
            if (ret != 0)
                ReportError(ret, "flycaptureGetCameraInformation");

            if (flycapInfo.CameraType ==
                PGRFlyModule.FlyCaptureCameraType.FLYCAPTURE_BLACK_AND_WHITE && !grayscale)
                throw new GoblinException("This Point Grey camera is B&W, so you need to initialize " +
                    "the video capture device with grayscale");

            cameraType = flycapInfo.CameraType;
            cameraModel = flycapInfo.CameraModel;

            // Start FlyCapture.
            if (cameraModel == PGRFlyModule.FlyCaptureCameraModel.FLYCAPTURE_DRAGONFLY2)
                ret = flycaptureStart(flycapContext, PGRFlyModule.FlyCaptureVideoMode.FLYCAPTURE_VIDEOMODE_640x480RGB, frameRate);
            else
                ret = flycaptureStart(flycapContext, videoMode, frameRate);
            
            if (ret != 0)
                ReportError(ret, "flycaptureStart");

            this.flycapContext = flycapContext;
        }

        /// <summary>
        /// Grabs
        /// </summary>
        /// <param name="camImage"></param>
        /// <returns></returns>
        public PGRFlyModule.FlyCaptureImage GrabRGBImage(IntPtr camImage)
        {
            int ret;
            ret = flycaptureGrabImage2(flycapContext, ref image);
            if (ret != 0)
                ReportError(ret, "flycaptureGrabImage2");

            if (cameraModel == PGRFlyModule.FlyCaptureCameraModel.FLYCAPTURE_DRAGONFLY2)
                return image;
            else
            {
                // Convert the image.
                flycapRGBImage.pData = (byte*)camImage;
                flycapRGBImage.pixelFormat = PGRFlyModule.FlyCapturePixelFormat.FLYCAPTURE_BGR;
                ret = flycaptureConvertImage(flycapContext, ref image, ref flycapRGBImage);
                if (ret != 0)
                    ReportError(ret, "flycaptureConvertImage");

                return flycapRGBImage;
            }
        }

		private void ReportError( int ret, string fname )
		{		
			throw new GoblinException(fname + " error: " + flycaptureErrorToString(ret));
		}

        #region IDisposable Members

        public void Dispose()
        {
            int ret;
            // Stop FlyCapture.
            ret = flycaptureStop(flycapContext);
            /*if (ret != 0)
                ReportError(ret, "flycaptureStop");*/

            // Destroy the context.
            ret = flycaptureDestroyContext(flycapContext);
            /*if(ret != 0)
                ReportError(ret, "flycaptureDestroyContext");*/
        }

        #endregion
    }
}



