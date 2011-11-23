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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using GoblinXNA;
using GoblinXNA.SceneGraph;
using GoblinXNA.Graphics;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.iWear;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.UI;
using GoblinXNA.Device.Generic;
using GoblinXNA.Device;
using GoblinXNA.Helpers;
using GoblinXNA.Device.Util;
using GoblinXNA.Network;

namespace StereoCameraCalibration
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Main : Microsoft.Xna.Framework.Game
    {
        const int CALIB_COUNT_MAX = 20;
        const float CAPTURE_INTERVAL = 1500; // in milliseconds = 1.5s
        const string calibrationFilename = "stereoCalib.xml";
        const float EXPECTED_GAP_MIN = 56;
        const float EXPECTED_GAP_MAX = 62;

        GraphicsDeviceManager graphics;

        Scene scene;

        ALVARMarkerTracker markerTracker;
        Object markerID;

        IVideoCapture leftCaptureDevice;
        IVideoCapture rightCaptureDevice;
        Texture2D leftTexture;
        Texture2D rightTexture;
        int[] leftVideoData;
        int[] rightVideoData;
        IntPtr leftImagePtr;
        IntPtr rightImagePtr;
        bool calibrating = false;

        iWearTracker iTracker;

        List<Matrix> relativeTransforms;

        Viewport leftViewport;
        Viewport rightViewport;

        Thread calibrationThread;

        CameraTransformStream camTransform;

        int captureCount = 0;
        bool finalized = false;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 480;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene();

            // Set up the VUZIX's iWear Wrap920AR for stereo
            SetupIWear();

            // Set up the stereo camera
            SetupStereoCamera();

            SetupViewports();

            // Set up the stereo camera calibration
            SetupCalibration();

            SetupNetwork();

            KeyboardInput.Instance.KeyPressEvent += new HandleKeyPress(HandleKeyPressEvent);
        }

        private void HandleKeyPressEvent(Keys key, KeyModifier modifier)
        {
            // This is somewhat necessary to exit from full screen mode
            if (key == Keys.Escape)
                this.Exit();

            if (key == Keys.Space)
            {
                if (!calibrating && !finalized)
                {
                    calibrationThread = new Thread(CalibrateStereo);
                    calibrationThread.Start();
                }

                //SaveCameraImages();
            }
        }

        private void SetupStereoCamera()
        {
            StereoCamera camera = new StereoCamera();
            camera.Translation = new Vector3(0, 0, 0);

            CameraNode cameraNode = new CameraNode(camera);

            scene.RootNode.AddChild(cameraNode);
            scene.CameraNode = cameraNode;
        }

        private void SetupViewports()
        {
            // Create a viewport for the left eye image
            leftViewport = new Viewport();
            leftViewport.X = 0;
            leftViewport.Y = 0;
            leftViewport.Width = 640;
            leftViewport.Height = 480;
            leftViewport.MinDepth = State.Device.Viewport.MinDepth;
            leftViewport.MaxDepth = State.Device.Viewport.MaxDepth;

            // Create a viewport for the right eye image
            rightViewport = new Viewport();
            rightViewport.X = 640;
            rightViewport.Y = 0;
            rightViewport.Width = 640;
            rightViewport.Height = 480;
            rightViewport.MinDepth = State.Device.Viewport.MinDepth;
            rightViewport.MaxDepth = State.Device.Viewport.MaxDepth;

            scene.BackgroundBound = leftViewport.Bounds;
        }

        private void SetupCalibration()
        {
            leftCaptureDevice = new DirectShowCapture2();
            leftCaptureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                ImageFormat.R8G8B8_24, false);

            // Add left video capture device to the scene for rendering left eye image
            scene.AddVideoCaptureDevice(leftCaptureDevice);

            rightCaptureDevice = new DirectShowCapture2();
            rightCaptureDevice.InitVideoCapture(1, FrameRate._30Hz, Resolution._640x480,
                ImageFormat.R8G8B8_24, false);

            // Add right video capture device to the scene for rendering right eye image
            scene.AddVideoCaptureDevice(rightCaptureDevice);

            // Create holders for retrieving the captured video images
            leftImagePtr = Marshal.AllocHGlobal(leftCaptureDevice.Width * leftCaptureDevice.Height * 3);
            rightImagePtr = Marshal.AllocHGlobal(rightCaptureDevice.Width * rightCaptureDevice.Height * 3);

            // Associate each video devices to each eye
            scene.LeftEyeVideoID = 0;
            scene.RightEyeVideoID = 1;

            scene.ShowCameraImage = true;

            float markerSize = 32.4f;

            // Initialize a marker tracker for tracking an marker array used for calibration
            markerTracker = new ALVARMarkerTracker();
            markerTracker.MaxMarkerError = 0.02f;
            markerTracker.ZNearPlane = 0.1f;
            markerTracker.ZFarPlane = 1000;
            markerTracker.InitTracker(leftCaptureDevice.Width, leftCaptureDevice.Height, "Wrap920_0_Left.xml", markerSize);
            ((StereoCamera)scene.CameraNode.Camera).LeftProjection = markerTracker.CameraProjection;

            // Add another marker detector for tracking right video capture device
            ALVARDllBridge.alvar_add_marker_detector(markerSize, 5, 2);

            ALVARDllBridge.alvar_add_camera("Wrap920_0_Right.xml", rightCaptureDevice.Width, rightCaptureDevice.Height);
            double[] projMat = new double[16];
            double cameraFovX = 0, cameraFovY = 0;
            ALVARDllBridge.alvar_get_camera_params(1, projMat, ref cameraFovX, ref cameraFovY, 1000, 0.1f);
            ((StereoCamera)scene.CameraNode.Camera).RightProjection = new Matrix(
                (float)projMat[0], (float)projMat[1], (float)projMat[2], (float)projMat[3],
                (float)projMat[4], (float)projMat[5], (float)projMat[6], (float)projMat[7],
                (float)projMat[8], (float)projMat[9], (float)projMat[10], (float)projMat[11],
                (float)projMat[12], (float)projMat[13], (float)projMat[14], (float)projMat[15]);

            // Add a marker array to be tracked
            markerID = markerTracker.AssociateMarker("ALVARGroundArray.xml");

            relativeTransforms = new List<Matrix>();
        }

        private void SetupIWear()
        {
            // Get an instance of iWearTracker
            iTracker = iWearTracker.Instance;
            // We need to initialize it before adding it to the InputMapper class
            iTracker.Initialize();

            iTracker.EnableStereo = true;
            // Add this iWearTracker to the InputMapper class for automatic update and disposal
            InputMapper.Instance.Add6DOFInputDevice(iTracker);
            // Re-enumerate all of the input devices so that the newly added device can be found
            InputMapper.Instance.Reenumerate();
        }

        private void CalibrateStereo()
        {
            calibrating = true;

            // get the left and right camera iamges
            leftCaptureDevice.GetImageTexture(null, ref leftImagePtr);
            rightCaptureDevice.GetImageTexture(null, ref rightImagePtr);

            markerTracker.DetectorID = 0;
            markerTracker.CameraID = 0;
            markerTracker.ProcessImage(leftCaptureDevice, leftImagePtr);

            bool markerFoundOnLeftVideo = markerTracker.FindMarker(markerID);

            if (markerFoundOnLeftVideo)
            {
                Matrix leftEyeTransform = markerTracker.GetMarkerTransform();

                markerTracker.DetectorID = 1;
                markerTracker.CameraID = 1;
                markerTracker.ProcessImage(rightCaptureDevice, rightImagePtr);

                bool markerFoundOnRightVideo = markerTracker.FindMarker(markerID);

                if (markerFoundOnRightVideo)
                {
                    Matrix rightEyeTransform = markerTracker.GetMarkerTransform();

                    leftEyeTransform = Matrix.Invert(leftEyeTransform);
                    rightEyeTransform = Matrix.Invert(rightEyeTransform);

                    Matrix relativeTransform = rightEyeTransform * Matrix.Invert(leftEyeTransform);
                    Vector3 rawScale, rawPos;
                    Quaternion rawRot;
                    relativeTransform.Decompose(out rawScale, out rawRot, out rawPos);

                    float xGap = Math.Abs(rawPos.X);
                    float yGap = Math.Abs(rawPos.Y);
                    float zGap = Math.Abs(rawPos.Z);

                    float xyRatio = yGap / xGap;
                    float xzRatio = zGap / xGap;

                    if (xyRatio < 0.05 && xzRatio < 0.2 && rawPos.Length() > EXPECTED_GAP_MIN && rawPos.Length() < EXPECTED_GAP_MAX)
                    {
                        camTransform.LeftCameraTransform = leftEyeTransform;
                        camTransform.RightCameraTransform = rightEyeTransform;
                        camTransform.ReadyToSend = true;

                        relativeTransforms.Add(relativeTransform);

                        Console.WriteLine("Completed calculation " + (captureCount + 1));

                        rawScale = Vector3Helper.QuaternionToEulerAngleVector3(rawRot);
                        rawScale = Vector3Helper.RadiansToDegrees(rawScale);
                        Console.WriteLine("Pos: " + rawPos.ToString() + ", Length: " + rawPos.Length() + ", Yaw: " 
                            + rawScale.X + ", Pitch: " + rawScale.Y + ": Roll, " + rawScale.Z);
                        Console.WriteLine();
                        captureCount++;
                    }
                    else
                    {
                        Console.WriteLine("Failed: Pos: " + rawPos.ToString() + ", Length: " + rawPos.Length());
                        Console.WriteLine();
                    }
                }
            }

            if (captureCount >= CALIB_COUNT_MAX)
            {
                SaveCalibration();

                Console.WriteLine("Finished calibration. Saved " + calibrationFilename);

                finalized = true;
            }

            calibrating = false;
        }

        private void SaveCameraImages()
        {
            if (leftVideoData == null)
            {
                leftVideoData = new int[leftCaptureDevice.Width * leftCaptureDevice.Height];
                rightVideoData = new int[rightCaptureDevice.Width * rightCaptureDevice.Height];

                leftTexture = new Texture2D(State.Device, leftCaptureDevice.Width, leftCaptureDevice.Height, false,
                    SurfaceFormat.Color);
                rightTexture = new Texture2D(State.Device, rightCaptureDevice.Width, rightCaptureDevice.Height, false,
                    SurfaceFormat.Color);

                if (!Directory.Exists("Images"))
                    Directory.CreateDirectory("Images");
            }

            IntPtr zeroPtr = IntPtr.Zero;
            leftCaptureDevice.GetImageTexture(leftVideoData, ref zeroPtr);
            rightCaptureDevice.GetImageTexture(rightVideoData, ref zeroPtr);

            int alpha = (int)(255 << 24);
            for (int i = 0; i < leftVideoData.Length; ++i)
            {
                leftVideoData[i] |= alpha;
                rightVideoData[i] |= alpha;
            }

            leftTexture.SetData<int>(leftVideoData);
            rightTexture.SetData<int>(rightVideoData);

            captureCount++;
            leftTexture.SaveAsPng(new FileStream("Images/left" + captureCount.ToString("00") + ".png", FileMode.Create, FileAccess.Write),
                leftTexture.Width, leftTexture.Height);
            rightTexture.SaveAsPng(new FileStream("Images/right" + captureCount.ToString("00") + ".png", FileMode.Create, FileAccess.Write),
                rightTexture.Width, rightTexture.Height);

            Console.WriteLine("Completed calculation " + (captureCount));

            if (captureCount > CALIB_COUNT_MAX)
            {
                Console.WriteLine("Finished calibration. Saved " + calibrationFilename);

                finalized = true;
            }
        }

        private void SaveCalibration()
        {
            Vector3 rawScale, rawPos;
            Quaternion rawRot;

            Vector3 posSum = Vector3.Zero;
            Quaternion rotSum = Quaternion.Identity;

            int count = relativeTransforms.Count;
            for (int i = 0; i < count; i++)
            {
                relativeTransforms[i].Decompose(out rawScale, out rawRot, out rawPos);

                posSum += rawPos;
                rotSum += rawRot;
            }

            Vector3 avgPos = posSum / count;
            rotSum.Normalize();
            rawScale = Vector3Helper.RadiansToDegrees(Vector3Helper.QuaternionToEulerAngleVector3(rotSum));

            Console.WriteLine("Relative camera position & orientation:");
            Console.WriteLine("Pos: " + avgPos.ToString() + ", Length: " + avgPos.Length() + ", Yaw: " 
                + rawScale.X + ", Pitch: " + rawScale.Y + ", Roll: " + rawScale.Z);

            Matrix avgTransform = Matrix.CreateFromQuaternion(rotSum);
            avgTransform.Translation = avgPos;

            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlElement xmlNode = xmlDoc.CreateElement("StereoCalibration");

            xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);
            xmlDoc.AppendChild(xmlNode);

            MatrixHelper.SaveMatrixToXML(xmlNode, avgTransform, xmlDoc);

            try
            {
                xmlDoc.Save(calibrationFilename);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Failed to save the file: " + calibrationFilename);
            }
        }

        private void SetupNetwork()
        {
            State.EnableNetworking = true;
            State.IsServer = true;

            LidgrenServer server = new LidgrenServer("StereoCameraCalibration", 14242);

            NetworkHandler networkHandler = new NetworkHandler();
            networkHandler.NetworkServer = server;

            scene.NetworkHandler = networkHandler;

            camTransform = new CameraTransformStream();
            scene.NetworkHandler.AddNetworkObject(camTransform);
        }

        protected override void Dispose(bool disposing)
        {
            if (calibrationThread != null && calibrationThread.IsAlive)
                calibrationThread.Abort();

            scene.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            scene.Update(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly, this.IsActive);
        }

        protected override void Draw(GameTime gameTime)
        {
            State.Device.Viewport = leftViewport;
            scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);

            State.Device.Viewport = rightViewport;
            scene.RenderScene();
        }
    }
}