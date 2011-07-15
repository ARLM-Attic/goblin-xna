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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using GoblinXNA;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Capture;
using GoblinXNA.SceneGraph;
using GoblinXNA.UI;

namespace CameraCalibration
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Main : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        Scene scene;

        IVideoCapture captureDevice;
        IntPtr imagePtr;

        const int ETALON_ROWS = 6;
        const int ETALON_COLUMNS = 8;
        const int CALIB_COUNT_MAX = 50;
        const float CAPTURE_INTERVAL = 1500; // in milliseconds = 1.5s

        int captureCount = 0;
        float timer = 0;
        bool finalized = false;
        bool useImageSequence = false;

        string calibrationFilename = "calib.xml";

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene(this);

            // Set up the camera, which defines the eye location and viewing frustum
            CreateCamera();

            // Set up the camera calibration
            SetupCalibration();

            State.ShowNotifications = true;
        }

        private void CreateCamera()
        {
            // Create a camera 
            Camera camera = new Camera();

            // Set the vertical field of view to be 60 degrees
            camera.FieldOfViewY = MathHelper.ToRadians(60);
            //camera.InterpupillaryDistance = 0.5f;
            // Set the near clipping plane to be 0.1f unit away from the camera
            camera.ZNearPlane = 0.1f;
            // Set the far clipping plane to be 1000 units away from the camera
            camera.ZFarPlane = 1000;

            // Now assign this camera to a camera node, and add this camera node to our scene graph
            CameraNode cameraNode = new CameraNode(camera);
            scene.RootNode.AddChild(cameraNode);

            // Assign the camera node to be our scene graph's current camera node
            scene.CameraNode = cameraNode;
        }

        private void SetupCalibration()
        {
            if (useImageSequence)
            {
                captureDevice = new NullCapture();
                // Use whatever the resolution of the still image you're using instead of 640x480
                captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                    ImageFormat.R8G8B8_24, false);
                ((NullCapture)captureDevice).StaticImageFile = "image" + captureCount + ".jpg";
            }
            else
            {
                captureDevice = new DirectShowCapture();
                captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                    ImageFormat.R8G8B8_24, false);
            }

            // Add this video capture device to the scene so that it can be used for
            // the marker tracker
            scene.AddVideoCaptureDevice(captureDevice);

            imagePtr = Marshal.AllocHGlobal(captureDevice.Width * captureDevice.Height * 3);

            scene.ShowCameraImage = true;

            // Initializes ALVAR camera
            ALVARDllBridge.alvar_init_camera(null, captureDevice.Width, captureDevice.Height);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // If we are still collecting calibration data
            // - For every 1.5s add calibration data from detected 7*9 chessboard
            if (captureCount < CALIB_COUNT_MAX)
            {
                if (timer >= CAPTURE_INTERVAL)
                {
                    string channelSeq = "RGB";
                    int nChannles = 3;
                    
                    if(useImageSequence)
                        ((NullCapture)captureDevice).StaticImageFile = "image" + captureCount + ".jpg";

                    captureDevice.GetImageTexture(null, ref imagePtr);

                    if (ALVARDllBridge.alvar_calibrate_camera(nChannles, channelSeq, channelSeq, imagePtr,
                        2.8, ETALON_ROWS, ETALON_COLUMNS))
                    {
                        Notifier.AddMessage("Captured Image " + (captureCount + 1));
                        captureCount++;
                    }

                    timer = 0;
                }
            }
            else
            {
                if (!finalized)
                {
                    Notifier.AddMessage("Calibrating...");
                    ALVARDllBridge.alvar_finalize_calibration(calibrationFilename);

                    Notifier.FadeOutTime = -1;
                    Notifier.AddMessage("Finished calibration. Saved " + calibrationFilename);

                    finalized = true;
                }
            }

            base.Draw(gameTime);
        }
    }
}
