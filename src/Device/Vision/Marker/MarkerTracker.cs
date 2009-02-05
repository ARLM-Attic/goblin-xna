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
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing;

using GoblinXNA.Device.Capture;

// Reference for the Tao OpenGL Library for C#
// http://www.taoframework.com/Home
using Tao.OpenGl;
using Tao.FreeGlut;

using Microsoft.Xna.Framework;
using GoblinXNA.Helpers;

namespace GoblinXNA.Device.Vision.Marker
{
    /// <summary>
    /// This class handles optical marker tracking by using external libraries. Currently,
    /// we support only the ARTag library.
    /// </summary>
    public sealed class MarkerTracker : IDisposable
    {
        #region Member Variables
        /// <summary>
        /// Focal length approx 850-1150 for 640x480 Dragonflies
        /// </summary>
        private const int DEFAULT_FX = 1100;
        /// <summary>
        /// About 400 for 320x240 webcam
        /// </summary>
        private const int DEFAULT_FY = 1100;
        private const int DEFAULT_WIDTH = 320;
        private const int DEFAULT_HEIGHT = 240;
        
        /// <summary>
        /// The tracker library we will be using
        /// </summary>
        private TrackerLibrary m_eLibrary;
        /// <summary>
        /// The object ID that we are working with
        /// </summary>
        private List<int> artag_object_ids;
        private int cam_width;
        private int cam_height;
        private float camera_fx;
        private float camera_fy;
        /// <summary>
        /// Create a null int pointer to the cam image
        /// </summary>
        private IntPtr cam_image;

        private int[] imageData;
        /// <summary>
        /// rgb_greybar =1 for RGB images, =0 for greyscale
        /// </summary>
        private char rgb_greybar;            
        private bool m_bInitialized = false;

        private Matrix cameraRotMatrix;

        /// <summary>
        /// Indicates whether to apply orthonormalization 
        /// </summary>
        private bool orthonormalize;

        private bool staticImageProcessed;
        #endregion

        /// <summary>
        /// An enum that defines the tracker library to use
        /// </summary>
        public enum TrackerLibrary
        {
            ARTag
            //ARToolkit
        }

        /// <summary>
        /// Creates an optical marker tracker that can track unique markers/fiducials found
        /// in either a static image or live video images, and provides the pose estimation and
        /// an identifier (usually in integer) of the found marker.
        /// </summary>
        public MarkerTracker()
        {
            m_eLibrary = TrackerLibrary.ARTag;
            cam_image = IntPtr.Zero;
            cam_width = DEFAULT_WIDTH;
            cam_height = DEFAULT_HEIGHT;
            camera_fx = DEFAULT_FX;
            camera_fy = DEFAULT_FY;
            rgb_greybar = (char)1;
            m_bInitialized = false;
            artag_object_ids = new List<int>();
            cameraRotMatrix = new Matrix();
            orthonormalize = true;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the library to use for optical marker tracking.
        /// </summary>
        public TrackerLibrary Library
        {
            get { return m_eLibrary; }
            set { m_eLibrary = value; }
        }

        /// <summary>
        /// Gets a list of ARTag object IDs that are returned by the ARTag library.
        /// ARTag associates an ID for every marker array.
        /// </summary>
        public List<int> ARTagObjectIDs
        {
            get { return artag_object_ids; }
        }

        /// <summary>
        /// Gets or sets whether to apply orthonormalization to the pose matrix returned
        /// by the marker tracker.
        /// </summary>
        public bool Orthonormalize
        {
            get { return orthonormalize; }
            set { orthonormalize = value; }
        }

        /// <summary>
        /// Gets the x-coordinate of the camera focal point.
        /// </summary>
        public float CameraFx
        {
            get { return camera_fx; }
        }

        /// <summary>
        /// Gets the y-coordinate of the camera focal point.
        /// </summary>
        public float CameraFy
        {
            get { return camera_fy; }
        }

        /// <summary>
        /// Gets the camera width. If static image is used, then this is the width of the image.
        /// </summary>
        public int CameraWidth
        {
            get { return cam_width; }
        }

        /// <summary>
        /// Gets the camera height. If static image is used, then this is the height of the image.
        /// </summary>
        public int CameraHeight
        {
            get { return cam_height; }
        }
        #endregion

        /// <summary>
        /// Gets the string version of the tracker library.
        /// </summary>
        /// <returns>A string version of the tracker library</returns>
        public String GetTrackerLibraryString()
        {
            //find out that library we are using
            if (m_eLibrary == TrackerLibrary.ARTag)
                return "ARTag";
            //else if (m_eLibrary == TrackerLibrary.ARToolkit)
            //    return "ARToolkit";

            //return an error
            return "ERROR";
        }

        /// <summary>
        /// Initilizes the marker tracker with a static image and an ARTag configuration
        /// (.cf) file.
        /// </summary>
        /// <param name="staticImageFile">The file name of the static image. Currently,
        /// we only support .ppm (color) or .pgm (grayscale) formats.</param>
        /// <param name="sArrayFilename">The file name of the ARTag configuration file 
        /// (.cf file)</param>
        /// <exception cref="GoblinException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="MarkerException"></exception>
        public void InitTracker(String staticImageFile, string sArrayFilename)
        {
            String fileType = Path.GetExtension(staticImageFile);

            if (fileType.Equals(".ppm"))
                rgb_greybar = (char)1;
            else if (fileType.Equals(".pgm"))
                rgb_greybar = (char)0;
            else
                throw new GoblinException("We currently do not support reading images other " +
                    "than .ppm or .pgm format");

            int w = 0, h = 0;
            cam_image = ARTagDllBridge.read_ppm_wrapped(staticImageFile, ref w, ref h);
            if (cam_image == IntPtr.Zero)
                throw new ArgumentException(staticImageFile + " does not exist or can not be opened");

            cam_width = w;
            cam_height = h;

            //try to initialize artag 
            if (ARTagDllBridge.init_artag_wrapped(cam_width, cam_height, 3) == 1)
                throw new MarkerException("ERROR: Can not initalize ARTag");

            imageData = new int[cam_width * cam_height];

            //set camera params
            ARTagDllBridge.artag_set_camera_params_wrapped(camera_fx, camera_fy, cam_width / 2.0, 
                cam_height / 2.0);

            SetArrayFile(sArrayFilename);
            InitGL();

            //set our init flag
            m_bInitialized = true;

            unsafe
            {
                byte* data = (byte*)(new IntPtr(cam_image.ToInt32() + w * h * 3));
                for (int i = 0; i < cam_width * cam_height; i++, data -= 3)
                {
                    imageData[i] = (*(data) << 16) | (*(data + 1) << 8) | *(data + 2);
                }
            }

            staticImageProcessed = false;
        }

        /// <summary>
        /// Initializes the marker tracker with the camera focal point and an ARTag configuration
        /// file (.cf file). Use this method if you want to use live video images for tracking.
        /// </summary>
        /// <param name="camera_fx">The x-coordinate of the camera focal point</param>
        /// <param name="camera_fy">The y-coordinate of the camera focal point</param>
        /// <param name="sArrayFilename">The file name of the ARTag configuration file 
        /// (.cf file)</param>
        public void InitTracker(float camera_fx, float camera_fy, string sArrayFilename)
        {
            //lets initialize the tracker first...
            int iWidth = MarkerBase.Base.VideoCaptures[0].Width;
            int iHeight = MarkerBase.Base.VideoCaptures[0].Height;

            cam_width = iWidth;
            cam_height = iHeight;
            this.camera_fx = camera_fx;
            this.camera_fy = camera_fy;

            //try to initialize artag 
            if (ARTagDllBridge.init_artag_wrapped(cam_width, cam_height, 3) == 1)
                throw new MarkerException("ERROR: Can not initalize ARTag");

            //allocate some memory for the image
            int imageSize = cam_width * cam_height * 3;
            cam_image = Marshal.AllocHGlobal(imageSize);

            imageData = new int[cam_width * cam_height];

            //set camera params
            ARTagDllBridge.artag_set_camera_params_wrapped(camera_fx, camera_fy, iWidth / 2.0, iHeight / 2.0);

            SetArrayFile(sArrayFilename);
            InitGL();

            //set our init flag
            m_bInitialized = true;
        }

        /// <summary>
        /// Initializes the marker tracker with camera focal point of (1100, 1100) and an 
        /// ARTag configuration file (.cf file).
        /// </summary>
        /// <param name="sArrayFilename">The file name of the ARTag configuration file 
        /// (.cf file)</param>
        public void InitTracker(string sArrayFilename)
        {
            InitTracker(DEFAULT_FX, DEFAULT_FY, sArrayFilename);
        }

        /// <summary>
        /// Initialize GL parameters for retrieving the transfromation matrix set by ARTag.
        /// </summary>
        private void InitGL()
        {
            // initialize glut
            Glut.glutInit();

            // create the window
            Glut.glutInitWindowSize(0, 0);
            // make sure the GLUT window is not visible
            Glut.glutInitWindowPosition(10000, 10000);
            Glut.glutCreateWindow("");

            double camera_opengl_dRight, camera_opengl_dLeft, camera_opengl_dTop, camera_opengl_dBottom;
            //set viewing frustrum to match camera FOV
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            camera_opengl_dRight = (double)cam_width / (double)(2.0 * camera_fx);
            camera_opengl_dLeft = -camera_opengl_dRight;
            camera_opengl_dTop = (double)cam_height / (double)(2.0 * camera_fy);
            camera_opengl_dBottom = -camera_opengl_dTop;
            Gl.glFrustum(camera_opengl_dLeft, camera_opengl_dRight, camera_opengl_dBottom, camera_opengl_dTop, 1.0, 102500.0);
            camera_opengl_dLeft *= 1024;
            camera_opengl_dRight *= 1024;
            camera_opengl_dBottom *= 1024;
            camera_opengl_dTop *= 1024;
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            Cursor.Hide();
        }

        /// <summary>
        /// Loads a new ARTag configuration file (.cf file).
        /// </summary>
        /// <param name="sArrayFilename">The file name of the ARTag configuration file 
        /// (.cf file)</param>
        /// <exception cref="MarkerException"></exception>
        public void SetArrayFile(String sArrayFilename)
        {
            //load coordframe file - returns -1 if there is an error
            if (ARTagDllBridge.load_array_file_wrapped(sArrayFilename) == -1)
                throw new MarkerException("Error loading marker file sets: " + sArrayFilename);
        }

        /// <summary>
        /// Associates a marker array defined in the loaded ARTag configuration file with 
        /// an ID. The associated ID is then used for looking for this specific marker array
        /// in either a static image or live video image.
        /// </summary>
        /// <param name="sArrayName">The name of the marker array defined in the loaded
        /// ARTag configuration file</param>
        /// <exception cref="MarkerException"></exception>
        /// <returns>An ID associated with the marker array name</returns>
        public int SetMarkerArray(string sArrayName)
        {
            //associate object with array that the user wants
            int artag_object_id = ARTagDllBridge.artag_associate_array_wrapped(sArrayName);

            //make sure we can find that object, if not, we have an error
            if (artag_object_id == -1)
                throw new MarkerException("Error associating marker array: " + sArrayName);

            artag_object_ids.Add(artag_object_id);

            return artag_object_id;
        }

        /// <summary>
        /// Associates a single marker ID defined in the loaded ARTag configuration file with 
        /// an ID (the marker ID and this ID is different). The associated ID is then used for 
        /// looking for this specific marker in either a static image or live video image.
        /// </summary>
        /// <param name="iMarkerID">The ID of the marker defined in the loaded
        /// ARTag configuration file</param>
        /// <exception cref="MarkerException"></exception>
        /// <returns>An ID associated with the marker ID</returns>
        public int SetSingleMarker(int iMarkerID)
        {
            //make our object id to be a single marker instead
            int artag_object_id = ARTagDllBridge.artag_associate_marker_wrapped(iMarkerID);

            //make sure we can find that object, if not, we have an error
            if (artag_object_id == -1)
                throw new MarkerException("Error associating marker #" + iMarkerID);

            artag_object_ids.Add(artag_object_id);

            return artag_object_id;
        }

        /// <summary>
        /// Processes either a static image or the video image captured from the first initialized 
        /// video capture device in the MarkerBase.Base.VideoCaptures array.
        /// </summary>
        /// <exception cref="MarkerException"></exception>
        /// <returns>An array of either a static image or video image pixels in 
        /// Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgr32 format. 
        /// The size is CameraWidth * CameraHeight.</returns>
        public int[] ProcessImage()
        {
            return ProcessImage(0);
        }

        /// <summary>
        /// Processes either a static image or the video image captured from the initialized video 
        /// capture device in the MarkerBase.Base.VideoCaptures array at 'cameraIndex'. If you're 
        /// only using one video capture device, then this index should be '0'.
        /// </summary>
        /// <param name="cameraIndex">The index of the initialized video capture devices to use
        /// for capturing the image. Ignore this parameter if using a static image.</param>
        /// <exception cref="MarkerException"></exception>
        /// <exception cref="GoblinException"></exception>
        /// <returns>An array of either a static image or video image pixels in 
        /// Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgr32 format. 
        /// The size is CameraWidth * CameraHeight.</returns>
        public int[] ProcessImage(int cameraIndex)
        {
            //make sure we are initialized
            if (!m_bInitialized)
                throw new MarkerException("MarkerTracker is not initialized.  Call initTracker()");

            //check to make sure we know what we are supposed to be looking for
            if (artag_object_ids.Count == 0)
                throw new MarkerException("We don't know what marker to look for.  Please use SetMarkers");

            //if we are using the camera, here is where we will init camera is main input device
            //if not, make sure the cam_image isnt null
            if (MarkerBase.Base.UseCamera)
            {
                //get the image frame and set the camera image
                VideoCapture kVideo = MarkerBase.Base.VideoCaptures[cameraIndex];
                if (kVideo != null)
                {
                    imageData = kVideo.GetImageTexture(true);

                    ARTagDllBridge.artag_find_objects_wrapped(kVideo.ImagePtr, 
                        (kVideo.GrayScale ? (char)0 : (char)1));
                }
            }
            else
            {
                if (!staticImageProcessed)
                {
                    //find the objects from just an image
                    ARTagDllBridge.artag_find_objects_wrapped(cam_image, rgb_greybar);
                    staticImageProcessed = true;
                }
            }

            return imageData;
        }

        /// <summary>
        /// Checkes whether either a marker array or a single marker associated with the 
        /// 'artag_object_id' returned from SetMarkerArray(String) or SetSingleMarker(int)
        /// methods is found in the processed image after calling ProcessImage(..) method.
        /// </summary>
        /// <param name="artag_object_id">An ID associated with either a marker array
        /// of a single marker returned from SetMarkerArray(String) or SetSingleMarker(int).</param>
        /// <seealso cref="SetMarkerArray(String)"/>
        /// <seealso cref="SetSingleMarker(int)"/>
        /// <exception cref="MarkerException"></exception>
        /// <returns>A boolean value representing whether a marker was found</returns>
        public bool FindMarker(int artag_object_id)
        {
            //make sure we are initialized
            if (!m_bInitialized)
                throw new MarkerException("MarkerTracker is not initialized.  Call initTracker()");

            if (artag_object_id == -1)
                return false;

            //find out if we found the objects
            char foundResult = ARTagDllBridge.artag_is_object_found_wrapped(artag_object_id);

            //if we have found a marker, return true
            if (foundResult == (char)1)
            {
                ARTagDllBridge.artag_set_object_opengl_matrix_wrapped(artag_object_id, (char)0);
                return true;
            }

            //if we got down here, just return false
            return false;
        }

        internal void SetCameraRotationMatrix(Matrix mat)
        {
            cameraRotMatrix = mat;
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

        /// <summary>
        /// Gets the pose matrix of the found marker in right-handed system format after calling
        /// the FindMarker(int) method.
        /// </summary>
        /// <see cref="FindMarker(int)"/>
        /// <returns>The pose matrix in right-handed system</returns>
        public Matrix GetMarkerRHSMatrix()
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

            /*if (orthonormalize)
            {
                Quaternion rot;
                Vector3 trans;
                Vector3 scale;
                d3dmat.Decompose(out scale, out rot, out trans);
                rot.Normalize();

                d3dmat = Matrix.CreateFromQuaternion(rot);
                d3dmat.Translation = trans;
            }*/

            return d3dmat;
        }

        /// <summary>
        /// Gets the rotation component of the pose matrix of the found marker in right-handed
        /// system format after calling the FindMarker(int) method.
        /// </summary>
        /// <returns></returns>
        public Matrix GetMarkerRHSRotationMatrix()
        {
            Matrix d3dmat = GetMarkerRHSMatrix();
            d3dmat.M14 = d3dmat.M24 = d3dmat.M34 = 0;
            d3dmat.M41 = d3dmat.M42 = d3dmat.M43 = 0;
            d3dmat.M44 = 1;
            return d3dmat;
        }

        /// <summary>
        /// Gets the position component of the pose matrix of the found marker in right-handed
        /// system format after calling the FindMarker(int) method.
        /// </summary>
        /// <returns></returns>
        public Matrix GetMarkerRHSPositionMatrix()
        {
            float[] kMatrix = GetMarkerMatrixFloat();
            Matrix d3dmat = new Matrix();
            d3dmat.M11 = d3dmat.M22 = d3dmat.M33 = d3dmat.M44 = 1;
            d3dmat.M41 = kMatrix[12];
            d3dmat.M42 = kMatrix[13];
            d3dmat.M43 = kMatrix[14];
            return d3dmat;
        }

        /// <summary>
        /// Gets the pose matrix of the found marker in left-handed system after calling
        /// FindMarker(int) method.
        /// </summary>
        /// <see cref="FindMarker(int)"/>
        /// <returns>The pose matrix in left-handed system</returns>
        public Matrix GetMarkerLHSMatrix()
        {
            Matrix d3dmat;

            float[] kMatrix = GetMarkerMatrixFloat();

            d3dmat.M11 = kMatrix[0];
            d3dmat.M12 = kMatrix[1];
            d3dmat.M13 = -kMatrix[2];
            d3dmat.M14 = kMatrix[3];
            d3dmat.M21 = kMatrix[4];
            d3dmat.M22 = kMatrix[5];
            d3dmat.M23 = -kMatrix[6];
            d3dmat.M24 = kMatrix[7];
            d3dmat.M31 = -kMatrix[8];
            d3dmat.M32 = -kMatrix[9];
            d3dmat.M33 = kMatrix[10];
            d3dmat.M34 = kMatrix[11];
            d3dmat.M41 = kMatrix[12];
            d3dmat.M42 = kMatrix[13];
            d3dmat.M43 = -kMatrix[14];
            d3dmat.M44 = kMatrix[15];

            if (orthonormalize)
                d3dmat = OrthonormalizeMatrix(d3dmat);

            return d3dmat;
        }

        /// <summary>
        /// Gets the rotation component of the pose matrix of the found marker in left-handed
        /// system format after calling the FindMarker(int) method.
        /// </summary>
        /// <returns></returns>
        public Matrix GetMarkerLHSRotationMatrix()
        {
            Matrix d3dmat = GetMarkerLHSMatrix();
            d3dmat.M14 = d3dmat.M24 = d3dmat.M34 = 0;
            d3dmat.M41 = d3dmat.M42 = d3dmat.M43 = 0;
            d3dmat.M44 = 1;
            return d3dmat;
        }

        /// <summary>
        /// Gets the position component of the pose matrix of the found marker in left-handed
        /// system format after calling the FindMarker(int) method.
        /// </summary>
        /// <returns></returns>
        public Matrix GetMarkerLHSPositionMatrix()
        {
            float[] kMatrix = GetMarkerMatrixFloat();
            Matrix d3dmat = new Matrix();
            d3dmat.M11 = d3dmat.M22 = d3dmat.M33 = d3dmat.M44 = 1;
            d3dmat.M41 = kMatrix[12];
            d3dmat.M42 = kMatrix[13];
            d3dmat.M43 = -kMatrix[14];
            return d3dmat;
        }

        internal Matrix GetMarkerLHSMatrixCameraAttached()
        {
            Matrix markerMat = GetMarkerLHSMatrix();
            return Matrix.Multiply(cameraRotMatrix, markerMat);
        }

        /// <summary>
        /// Orthonormalizes a transformation matrix.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        private Matrix OrthonormalizeMatrix(Matrix mat)
        {
            Matrix result = new Matrix();
            Vector3 firstRow = new Vector3(),
                secondRow = new Vector3(),
                thirdRow = new Vector3(),
                normalized = new Vector3(),
                projected = new Vector3(),
                ortho = new Vector3(),
                orthonormalized = new Vector3();
            firstRow.X = mat.M11; 
            firstRow.Y = mat.M21; 
            firstRow.Z = mat.M31;
            secondRow.Z = mat.M12;
            secondRow.Y = mat.M22;
            secondRow.Z = mat.M32;
            // Normalize the vector created from the three components of the 1st row
            normalized = Vector3.Normalize(firstRow);

            // Project the normalized vector onto the vector created from the 2nd row
            float fact = Vector3.Dot(secondRow, normalized) / 
                Vector3.DistanceSquared(secondRow, new Vector3());
            projected = Vector3.Multiply(secondRow, fact);

            // Subtract the projection from the 2nd vector to obtain a vector that is 
            // orthogonal to the normalized vector
            ortho = Vector3.Subtract(secondRow, projected);

            // Normalize the orthogonal vector
            orthonormalized = Vector3.Normalize(ortho);

            // Take the cross product of the two normalized vectors to obtain the 3rd
            // vector that is orthogonal to both
            thirdRow = Vector3.Cross(normalized, orthonormalized);

            // Assemble the three vectors as teh rows of the new orthonomalized matrix
            result.M11 = normalized.X; 
            result.M21 = normalized.Y; 
            result.M31 = normalized.Z;
            result.M12 = orthonormalized.X;
            result.M22 = orthonormalized.Y;
            result.M32 = orthonormalized.Z;
            result.M13 = thirdRow.X;
            result.M23 = thirdRow.Y;
            result.M33 = thirdRow.Z;
            result.M41 = mat.M41;
            result.M42 = mat.M42;
            result.M43 = mat.M43;
            result.M44 = mat.M44;

            return result;
        }

        #region IDisposable Members

        public void Dispose()
        {
            ARTagDllBridge.close_artag_wrapped();
        }

        #endregion
        
    }
}
