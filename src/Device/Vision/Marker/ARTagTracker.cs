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
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

// Reference for the Tao OpenGL Library for C#
// http://www.taoframework.com/Home
using Tao.OpenGl;
using Tao.FreeGlut;

using Microsoft.Xna.Framework;

using GoblinXNA.Helpers;
using GoblinXNA.Device.Capture;

namespace GoblinXNA.Device.Vision.Marker
{
    /// <summary>
    /// A marker tracker implementation using the ARTag (http://www.artag.net/) library developed 
    /// by Mark Fiala.
    /// </summary>
    public class ARTagTracker : IMarkerTracker
    {
        #region Member Fields

        /// <summary>
        /// The object ID that we are working with
        /// </summary>
        private List<int> artag_object_ids;
        private int img_width;
        private int img_height;
        private float camera_fx;
        private float camera_fy;

        /// <summary>
        /// rgb_greybar =1 for RGB images, =0 for greyscale
        /// </summary>
        private char rgb_greybar;
        private int[] imageData;
        private IntPtr cam_image;

        private bool initialized;

        private String configFilename;
        private String imageFilename;

        private int glWindowID;
        private bool glInitialized;

        private float zNearPlane;
        private float zFarPlane;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an ARTag marker tracker.
        /// </summary>
        public ARTagTracker()
        {
            img_width = 0;
            img_height = 0;
            camera_fx = 0;
            camera_fy = 0;
            configFilename = "";
            imageFilename = "";

            cam_image = IntPtr.Zero;
            imageData = null;
            initialized = false;
            glInitialized = false;
            artag_object_ids = new List<int>();

            zNearPlane = 1.0f;
            zFarPlane = 1025;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a list of ARTag object IDs that are returned by the ARTag library.
        /// ARTag associates an ID for every marker array.
        /// </summary>
        public List<int> ARTagObjectIDs
        {
            get { return artag_object_ids; }
        }

        public float CameraFx
        {
            get { return camera_fx; }
        }

        public float CameraFy
        {
            get { return camera_fy; }
        }

        public int ImageWidth
        {
            get { return img_width; }
        }

        public int ImageHeight
        {
            get { return img_height; }
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
                        throw new MarkerException("MarkerTracker is not initialized. Call InitTracker(...)");

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
                        InitTracker(camera_fx, camera_fy, w, h, (rgb_greybar == (char)0), configFilename);
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
                float camera_dRight = (float)img_width / (float)(2.0 * camera_fx);
                float camera_dLeft = -camera_dRight;
                float camera_dTop = (float)img_height / (float)(2.0 * camera_fy);
                float camera_dBottom = -camera_dTop;

                return Matrix.CreatePerspectiveOffCenter(camera_dLeft, camera_dRight, camera_dBottom,
                    camera_dTop, zNearPlane, zFarPlane);
            }
        }

        /// <summary>
        /// Gets or sets the near clipping plane used to compute CameraProjection.
        /// The default value is 1.0f.
        /// </summary>
        public float ZNearPlane
        {
            get { return zNearPlane; }
            set { zNearPlane = value; }
        }

        /// <summary>
        /// Gets or sets the far clipping plane used to compute CameraProjection.
        /// The default value is 1025.
        /// </summary>
        public float ZFarPlane
        {
            get { return zFarPlane; }
            set { zFarPlane = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initilizes the ARTag marker tracker with the given camera focal point (x, y),
        /// image dimension (width, height), whether the image is grayscale, and the 
        /// configuration (.cf) file.
        /// </summary>
        /// <param name="configs">
        /// There are two ways to pass the parameters. One way is to pass in the order of
        /// (float cameraFx, float cameraFy, int width, int height, bool grayscale,
        /// String configFile), and the other way is (System.Drawing.PointF focalPoint, 
        /// System.Drawing.Size imageDimension, bool grayscale, String configFile).
        /// </param>
        /// <exception cref="MarkerException"></exception>
        public void InitTracker(params Object[] configs)
        {
            String config = "";
            int bpp = 0;
            bool need_reinitialize = false;
            bool need_reset = false;
            if (configs.Length == 4)
            {
                try
                {
                    float fx, fy;
                    int w, h;
                    PointF focalPoint = (PointF)configs[0];
                    Size imageDimensnion = (Size)configs[1];
                    if ((bool)configs[2])
                    {
                        if (bpp != 1)
                        {
                            bpp = 1;
                            rgb_greybar = (char)0;
                            need_reinitialize = true;
                        }
                    }
                    else
                    {
                        if (bpp != 3)
                        {
                            bpp = 3;
                            rgb_greybar = (char)1;
                            need_reinitialize = true;
                        }
                    }
                    config = (String)configs[3];

                    fx = focalPoint.X;
                    fy = focalPoint.Y;
                    if ((fx != camera_fx) || (fy != camera_fy))
                        need_reset = true;
                    w = imageDimensnion.Width;
                    h = imageDimensnion.Height;
                    if ((w != img_width) || (h != img_height))
                        need_reinitialize = true;

                    camera_fx = fx;
                    camera_fy = fy;
                    img_width = w;
                    img_height = h;
                }
                catch (Exception exp)
                {
                    throw new MarkerException(GetInitTrackerUsage());
                }
            }
            else if (configs.Length == 6)
            {
                try
                {
                    float fx, fy;
                    int w, h;
                    fx = (float)configs[0];
                    fy = (float)configs[1];
                    if ((fx != camera_fx) || (fy != camera_fy))
                        need_reset = true;
                    w = (int)configs[2];
                    h = (int)configs[3];
                    if ((w != img_width) || (h != img_height))
                        need_reinitialize = true;

                    camera_fx = fx;
                    camera_fy = fy;
                    img_width = w;
                    img_height = h;
                    if ((bool)configs[4])
                    {
                        if (bpp != 1)
                        {
                            bpp = 1;
                            rgb_greybar = (char)0;
                            need_reinitialize = true;
                        }
                    }
                    else
                    {
                        if (bpp != 3)
                        {
                            bpp = 3;
                            rgb_greybar = (char)1;
                            need_reinitialize = true;
                        }
                    }
                    config = (String)configs[5];
                }
                catch (Exception exp)
                {
                    throw new MarkerException(GetInitTrackerUsage());
                }
            }
            else
                throw new MarkerException(GetInitTrackerUsage());

            // try to initialize artag 
            if(need_reinitialize)
                if (ARTagDllBridge.init_artag_wrapped(img_width, img_height, bpp) == 1)
                    throw new MarkerException("ERROR: Can not initalize ARTag");

            // set camera params
            if(need_reinitialize || need_reset)
                ARTagDllBridge.artag_set_camera_params_wrapped(camera_fx, camera_fy, 
                    img_width / 2.0, img_height / 2.0);

            if (need_reinitialize || !config.Equals(configFilename))
            {
                configFilename = config;
                // load coordframe file - returns -1 if there is an error
                if (ARTagDllBridge.load_array_file_wrapped(configFilename) == -1)
                    throw new MarkerException("Error loading marker file sets: " + configFilename);
            }

            InitGL();

            initialized = true;
        }

        /// <summary>
        /// Associates a marker with an identifier so that the identifier can be used to find this
        /// marker after processing the image. 
        /// </summary>
        /// <param name="markerConfigs">
        /// If you want to associate a single marker, then pass the marker ID in integer format,
        /// otherwise if you want to associate an array of markers, then pass the array name
        /// in String format.
        /// </param>
        /// <exception cref="MarkerException"></exception>
        /// <returns>An integer identifier for this marker object</returns>
        public Object AssociateMarker(params Object[] markerConfigs)
        {
            // make sure we are initialized
            if (!initialized)
                throw new MarkerException("ARTagTracker is not initialized. Call InitTracker(...)");

            if (markerConfigs.Length != 1)
                throw new MarkerException(GetAssocMarkerUsage());

            int artagObjectID = -1;
            if (markerConfigs[0] is int)
                artagObjectID = ARTagDllBridge.artag_associate_marker_wrapped((int)markerConfigs[0]);
            else
                artagObjectID = ARTagDllBridge.artag_associate_array_wrapped((String)markerConfigs[0]);

            // make sure we can find that object, if not, we have an error
            if (artagObjectID == -1)
                throw new MarkerException("Error associating marker: " + markerConfigs[0]);

            artag_object_ids.Add(artagObjectID);

            return artagObjectID;
        }

        /// <summary>
        /// Processes a static image set in the StaticImageFile property.
        /// </summary>>
        /// <see cref="StaticImageFile"/>
        /// <exception cref="MarkerException"></exception>
        public void ProcessImage()
        {
            if (cam_image == IntPtr.Zero)
                throw new MarkerException("You either forgot to add your video capture " +
                    "device or didn't set the static image file");

            ARTagDllBridge.artag_find_objects_wrapped(cam_image, rgb_greybar);
        }

        /// <summary>
        /// Processes the video image captured from an initialized video capture device. 
        /// </summary>
        /// <param name="captureDevice">An initialized video capture device</param>
        /// <exception cref="MarkerException"></exception>
        public void ProcessImage(IVideoCapture captureDevice)
        {
            // make sure we are initialized
            if (!initialized)
                throw new MarkerException("MarkerTracker is not initialized. Call InitTracker(...)");

            if (captureDevice.Format != GoblinXNA.Device.Capture.ImageFormat.R8G8B8_24)
                throw new MarkerException("ARTagTracker only supports R8G8B8_24 format. The format: "
                    + captureDevice.Format + " is not supported by ARTagTracker");

            if (captureDevice == null)
                return;

            int w = captureDevice.Width;
            int h = captureDevice.Height;
            float fx = captureDevice.FocalPoint.X;
            float fy = captureDevice.FocalPoint.Y;

            // If the focal point or image dimension changed from the previous configuration
            // then we need to re-initialize the ARTag tracker
            if ((w != img_width) || (h != img_height) || ((fx != 0) && (fx != camera_fx)) || 
                ((fy != 0) && (fy != camera_fy)))
                InitTracker(fx, fy, w, h, captureDevice.GrayScale, configFilename);

            ARTagDllBridge.artag_find_objects_wrapped(captureDevice.ImagePtr, rgb_greybar);
        }

        /// <summary>
        /// Checks whether a marker identified by 'markerID' is found in the processed image
        /// after calling ProcessImage(...) method.
        /// </summary>
        /// <param name="markerID">An ID associated with a marker returned from AssociateMarker(...)
        /// method.</param>
        /// <exception cref="MarkerException"></exception>
        /// <returns>A boolean value representing whether a marker was found</returns>
        public bool FindMarker(Object markerID)
        {
            // make sure we are initialized
            if (!initialized)
                throw new MarkerException("MarkerTracker is not initialized. Call InitTracker(...)");

            if (!(markerID is int))
                throw new MarkerException("'markerID' has to be an integer");

            int id = (int)markerID;
            if (!artag_object_ids.Contains(id))
                throw new MarkerException(id + " is not associated yet. Call AssociateMarker(...) to " +
                    "associate the marker ID first.");

            // find out if we found the objects
            char foundResult = ARTagDllBridge.artag_is_object_found_wrapped(id);

            // if we have found a marker, return true
            if (foundResult == (char)1)
            {
                ARTagDllBridge.artag_set_object_opengl_matrix_wrapped(id, (char)0);
                return true;
            }

            // if we got down here, just return false
            return false;
        }

        /// <summary>
        /// Gets the pose of the found marker in an array of 16 float numbers after calling
        /// the FindMarker(int) method.
        /// </summary>
        /// <see cref="FindMarker(int)"/>
        /// <returns>An array of floats containing the marker transformation matrix</returns>
        public float[] GetMarkerMatrixFloat()
        {
            float[] kMatrix = new float[16];
            /*IntPtr matAddr = artag_get_object_matrix_wrapped();
            unsafe
            {
                float* f = (float*)matAddr.ToInt32();
                for (int i = 0; i < 16; i++, f++)
                    kMatrix[i] = *f;
            }*/
            Gl.glGetFloatv(Gl.GL_MODELVIEW_MATRIX, kMatrix);
            return kMatrix;
        }

        public Matrix GetMarkerTransform()
        {
            Matrix d3dmat;

            float[] kMatrix = GetMarkerMatrixFloat();

            d3dmat.M11 = kMatrix[0];
            d3dmat.M12 = kMatrix[1];
            d3dmat.M13 = kMatrix[2];
            d3dmat.M14 = kMatrix[3];
            d3dmat.M21 = kMatrix[4];
            d3dmat.M22 = kMatrix[5];
            d3dmat.M23 = kMatrix[6];
            d3dmat.M24 = kMatrix[7];
            d3dmat.M31 = kMatrix[8];
            d3dmat.M32 = kMatrix[9];
            d3dmat.M33 = kMatrix[10];
            d3dmat.M34 = kMatrix[11];
            d3dmat.M41 = kMatrix[12];
            d3dmat.M42 = kMatrix[13];
            d3dmat.M43 = kMatrix[14];
            d3dmat.M44 = kMatrix[15];

            return d3dmat;
        }

        /// <summary>
        /// Gets the rotation component of the pose matrix of the found marker after calling 
        /// the FindMarker(...) method.
        /// </summary>
        /// <returns>The rotation matrix of the marker pose</returns>
        public Matrix GetMarkerRotation()
        {
            Matrix d3dmat = GetMarkerTransform();
            d3dmat.M14 = d3dmat.M24 = d3dmat.M34 = 0;
            d3dmat.M41 = d3dmat.M42 = d3dmat.M43 = 0;
            d3dmat.M44 = 1;
            return d3dmat;
        }

        /// <summary>
        /// Gets the position component of the pose matrix of the found marker after calling 
        /// the FindMarker(...) method.
        /// </summary>
        /// <returns>The translation matrix of the marker pose</returns>
        public Matrix GetMarkerPosition()
        {
            float[] kMatrix = GetMarkerMatrixFloat();
            Matrix d3dmat = new Matrix();
            d3dmat.M11 = d3dmat.M22 = d3dmat.M33 = d3dmat.M44 = 1;
            d3dmat.M41 = kMatrix[12];
            d3dmat.M42 = kMatrix[13];
            d3dmat.M43 = kMatrix[14];
            return d3dmat;
        }

        public void Dispose()
        {
            if (initialized)
            {
                ARTagDllBridge.close_artag_wrapped();
                Glut.glutDestroyWindow(glWindowID);
            }
        }

        #endregion

        #region Private Methods

        private String GetInitTrackerUsage()
        {
            return "Usage: InitTracker(float cameraFx, float cameraFy, int width, int height, " +
                "bool grayscale, String configFile) or InitMarker(System.Drawing.PointF focalPoint, " +
                "System.Drawing.Size imageDimension, bool grayscale, String configFile)";
        }

        private String GetAssocMarkerUsage()
        {
            return "Usage: AssociateMarker(int markerID) or AssociateMarker(String markerArrayName)";
        }

        /// <summary>
        /// Initialize GL parameters for retrieving the transfromation matrix set by ARTag.
        /// </summary>
        private void InitGL()
        {
            if (glInitialized)
                return;

            // initialize glut
            Glut.glutInit();

            // create the window
            Glut.glutInitWindowSize(0, 0);
            // make sure the GLUT window is not visible
            Glut.glutInitWindowPosition(10000, 10000);
            glWindowID = Glut.glutCreateWindow("");

            double camera_opengl_dRight, camera_opengl_dLeft, camera_opengl_dTop, camera_opengl_dBottom;
            //set viewing frustrum to match camera FOV
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            camera_opengl_dRight = (double)img_width / (double)(2.0 * camera_fx);
            camera_opengl_dLeft = -camera_opengl_dRight;
            camera_opengl_dTop = (double)img_height / (double)(2.0 * camera_fy);
            camera_opengl_dBottom = -camera_opengl_dTop;
            Gl.glFrustum(camera_opengl_dLeft, camera_opengl_dRight, camera_opengl_dBottom, camera_opengl_dTop, 1.0, 102500.0);
            camera_opengl_dLeft *= 1024;
            camera_opengl_dRight *= 1024;
            camera_opengl_dBottom *= 1024;
            camera_opengl_dTop *= 1024;
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            Cursor.Hide();

            glInitialized = true;
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

        #endregion
    }
}
