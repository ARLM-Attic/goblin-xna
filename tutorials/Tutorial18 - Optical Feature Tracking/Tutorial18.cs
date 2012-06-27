/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Feature;
using GoblinXNA.Helpers;

namespace Tutorial18___Optical_Feature_Tracking
{
    /// <summary>
    /// This tutorial demonstrates how to use ALVAR 2.0's feature-based tracking. Before you run this tutorial,
    /// make sure to generate the .dat file using ALVAR's SampleMarkerlessCreator program using an image of 
    /// your choice. Once it's generated, please copy it to GoblinXNAv4.1/bin/x86/ directory. Please read through 
    /// ALVAR's documentation regarding on the image specifications and how to generate the .dat file.
    /// </summary>
    public class Tutorial18 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        Scene scene;

        public Tutorial18()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
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
            scene = new Scene();

            // For some reason, it sometimes causes memory conflict when it attempts to update the
            // marker transformation in the multi-threaded code, so if you see weird exceptions 
            // thrown in Shaders, then you should not enable the marker tracking thread
            State.ThreadOption = (ushort)ThreadOptions.MarkerTracking;

            // Set up optical marker tracking
            // Note that we don't create our own camera when we use optical marker
            // tracking. It'll be created automatically
            SetupFeatureTracking();

            // Set up the lights used in the scene
            CreateLights();

            // Create 3D objects
            CreateObjects();

            // Show Frames-Per-Second on the screen for debugging
            State.ShowFPS = true;
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(1, -1, -1);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = new Vector4(0.6f, 0.6f, 0.6f, 1);

            // Create a light node to hold the light source
            LightNode lightNode = new LightNode();
            lightNode.AmbientLightColor = new Vector4(0.2f, 0.2f, 0.2f, 1);
            lightNode.LightSource = lightSource;

            scene.RootNode.AddChild(lightNode);
        }

        private void SetupFeatureTracking()
        {
            // Create our video capture device that uses DirectShow library. Note that 
            // the combinations of resolution and frame rate that are allowed depend on 
            // the particular video capture device. Thus, setting incorrect resolution 
            // and frame rate values may cause exceptions or simply be ignored, depending 
            // on the device driver.  The values set here will work for a Microsoft VX 6000, 
            // and many other webcams.
            DirectShowCapture captureDevice = new DirectShowCapture();
            captureDevice.InitVideoCapture(0, FrameRate._60Hz, Resolution._640x480,
                ImageFormat.R8G8B8_24, false);

            // Add this video capture device to the scene so that it can be used for
            // the marker tracker
            scene.AddVideoCaptureDevice(captureDevice);

            // Create a feature tracker that uses the ALVAR library
            ALVARFeatureTracker tracker = new ALVARFeatureTracker();
            tracker.InitTracker(captureDevice.Width, captureDevice.Height, "default_calib.xml");

            // Set the feature tracker to use for our scene
            scene.MarkerTracker = tracker;

            // Display the camera image in the background. Note that this parameter should
            // be set after adding at least one video capture device to the Scene class.
            scene.ShowCameraImage = true;
        }

        private void CreateObjects()
        {
            float size = 4.0f;

            // Create a marker node to track a feature-based image.
            // NOTE: If you get an exception here, that means you haven't generated the .dat file yet
            // Please see this 
            MarkerNode groundMarkerNode = new MarkerNode(scene.MarkerTracker, "markerless.png.dat");

            // Now add the above nodes to the scene graph in the appropriate order.
            // Note that only the nodes added below the marker node are affected by 
            // the marker transformation.
            scene.RootNode.AddChild(groundMarkerNode);

            // Create a geometry node with a model of a box that will be overlaid on top of the image
            GeometryNode boxNode = new GeometryNode("Box")
            {
                Model = new Box(size),
                Material = new Material()
                {
                    Diffuse = Color.Red.ToVector4(),
                    Specular = Color.White.ToVector4(),
                    SpecularPower = 10
                }
            };

            TransformNode boxTrans = new TransformNode()
            {
                Translation = new Vector3(0, 0, -size / 2)
            };

            groundMarkerNode.AddChild(boxTrans);
            boxTrans.AddChild(boxNode);
        }

        protected override void Dispose(bool disposing)
        {
            scene.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            scene.Update(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly, this.IsActive);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            scene.Draw(gameTime.ElapsedGameTime, gameTime.IsRunningSlowly);
        }
    }
}
