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
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Capture;
using GoblinXNA.Device.Vision;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Util;
using GoblinXNA.Physics;
using GoblinXNA.Helpers;
using GoblinXNA.Device.iWear;
using GoblinXNA.Device;
using GoblinXNA.Device.Generic;

using GoblinXNA.UI.UI2D;

namespace Tutorial13___iWear_VR920
{
    /// <summary>
    /// An enum that indicates which marker tracking library to use.
    /// </summary>
    enum MarkerLibrary
    {
        /// <summary>
        /// ARTag library developed by Mark Fiala
        /// </summary>
        ARTag,
        /// <summary>
        /// ALVAR library developed by VTT
        /// </summary>
        ALVAR
    }

    /// <summary>
    /// This tutorial demonstrates the stereoscropic rendering using Vuzix's iWear VR920.
    /// NOTE: Some resources included in this project are shared between Tutorial 8, so 
    /// please make sure that Tutorial 8 runs before running this tutorial.
    /// </summary>
    public class Tutorial13 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteFont sampleFont;

        Scene scene;
        MarkerNode groundMarkerNode;

        bool stereoMode = true;

        iWearTracker iTracker;

        MarkerLibrary markerLibrary = MarkerLibrary.ALVAR;

        public Tutorial13()
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
            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene(this);

            // If stereo mode is true, then setup stereo camera. If not stereo, we don't need to
            // setup a camera since it's automatically setup by Scene when marker tracker is
            // used. This stereo camera needs to be setup before setting up the marker tracker so
            // the stereo camera will have correct projection matrix computed by the marker tracker.
            if (stereoMode)
                SetupStereoCamera();

            // Set up optical marker tracking
            SetupMarkerTracking();

            // Set up the VUZIX's iWear VR920 for both stereo and orientation tracking
            SetupIWear();

            // Set up the lights used in the scene
            CreateLights();

            // Create 3D objects
            CreateObjects();

            // Create the ground that represents the physical ground marker array
            CreateGround();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            scene.PreferPerPixelLighting = true;

            // Enable shadow mapping
            // NOTE: In order to use shadow mapping, you will need to add 'PostScreenShadowBlur.fx'
            // and 'ShadowMap.fx' shader files as well as 'ShadowDistanceFadeoutMap.dds' texture file
            // to your 'Content' directory
            scene.EnableShadowMapping = true;

            // Show Frames-Per-Second on the screen for debugging
            State.ShowFPS = true;

            KeyboardInput.Instance.KeyPressEvent += new HandleKeyPress(HandleKeyPressEvent);

            base.Initialize();
        }

        private void HandleKeyPressEvent(Keys key, KeyModifier modifier)
        {
            // This is somewhat necessary to exit from full screen mode
            if (key == Keys.Escape)
                this.Exit();
        }

        private void SetupStereoCamera()
        {
            StereoCamera camera = new StereoCamera();
            camera.Translation = new Vector3(0, 0, 0);
            camera.FieldOfViewY = (float)Math.PI / 4;
            // For stereo camera, you need to setup the interpupillary distance which is the distance
            // between the left and right eyes
            camera.InterpupillaryDistance = 1.0f;
            CameraNode cameraNode = new CameraNode(camera);

            scene.RootNode.AddChild(cameraNode);
            scene.CameraNode = cameraNode;

            // The stereo mode only works correctly when it's in full screen mode when iWear VR920
            // is used
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(1, -1, -1);
            lightSource.Diffuse = new Vector4(0.8f, 0.8f, 0.8f, 1);
            lightSource.Specular = new Vector4(0.6f, 0.6f, 0.6f, 1);

            // Create a light node to hold the light source
            LightNode lightNode = new LightNode();
            // Add an ambient component
            lightNode.AmbientLightColor = new Vector4(0.3f, 0.3f, 0.3f, 1);
            lightNode.LightSources.Add(lightSource);

            // Add this light node to the root node
            groundMarkerNode.AddChild(lightNode);
        }

        private void SetupMarkerTracking()
        {
            DirectShowCapture captureDevice = new DirectShowCapture();
            captureDevice.InitVideoCapture(0, FrameRate._30Hz, Resolution._640x480,
                ImageFormat.R8G8B8_24, false);

            // Add this video capture device to the scene so that it can be used for
            // the marker tracker
            scene.AddVideoCaptureDevice(captureDevice);

            IMarkerTracker tracker = null;
            if (markerLibrary == MarkerLibrary.ALVAR)
            {
                // Create an optical marker tracker that uses ALVAR library
                tracker = new ALVARMarkerTracker();
                ((ALVARMarkerTracker)tracker).MaxMarkerError = 0.02f;
                tracker.InitTracker(captureDevice.Width, captureDevice.Height, "calib.xml", 9.0);
            }
            else
            {
                // Create an optical marker tracker that uses ARTag library
                tracker = new ARTagTracker();
                // Set the configuration file to look for the marker specifications
                tracker.InitTracker(638.052f, 633.673f, captureDevice.Width,
                    captureDevice.Height, false, "ARTag.cf");
            }

            scene.MarkerTracker = tracker;

            // Create a marker node to track a ground marker array. 
            if (markerLibrary == MarkerLibrary.ALVAR)
            {
                // Create an array to hold a list of marker IDs that are used in the marker
                // array configuration (even though these are already specified in the configuration
                // file, ALVAR still requires this array)
                int[] ids = new int[28];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = i;

                groundMarkerNode = new MarkerNode(scene.MarkerTracker, "ALVARGroundArray.txt", ids);

                // Add a transform node to tranlate the objects to be centered around the
                // marker board.
                TransformNode transNode = new TransformNode();
                transNode.Translation = new Vector3(-42, -33, 0);

                scene.RootNode.AddChild(transNode);
                transNode.AddChild(groundMarkerNode);
            }
            else
            {
                groundMarkerNode = new MarkerNode(scene.MarkerTracker, "ground");

                scene.RootNode.AddChild(groundMarkerNode);
            }

            scene.ShowCameraImage = true;
        }

        private void SetupIWear()
        {
            // Get an instance of iWearTracker
            iTracker = iWearTracker.Instance;
            // We need to initialize it before adding it to the InputMapper class
            iTracker.Initialize();
            // If not stereo, then we need to set the iWear VR920 to mono mode (by default, it's
            // stereo mode if stereo is available)
            if (!stereoMode)
                iTracker.EnableStereo = false;
            // Add this iWearTracker to the InputMapper class for automatic update and disposal
            InputMapper.Instance.Add6DOFInputDevice(iTracker);
            // Re-enumerate all of the input devices so that the newly added device can be found
            InputMapper.Instance.Reenumerate();
        }

        private void CreateGround()
        {
            GeometryNode groundNode = new GeometryNode("Ground");
            if (markerLibrary == MarkerLibrary.ALVAR)
                groundNode.Model = new Box(95, 59, 0.1f);
            else
                groundNode.Model = new Box(85, 66, 0.1f);

            // Set this ground model to act as an occluder so that it appears transparent
            groundNode.IsOccluder = true;

            // Make the ground model to receive shadow casted by other objects with
            // CastShadows set to true
            groundNode.Model.ReceiveShadows = true;

            Material groundMaterial = new Material();
            groundMaterial.Diffuse = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
            groundMaterial.Specular = Color.White.ToVector4();
            groundMaterial.SpecularPower = 20;

            groundNode.Material = groundMaterial;

            TransformNode groundTransNode = new TransformNode();
            groundTransNode.Translation = new Vector3(42, 32, 0);

            groundMarkerNode.AddChild(groundTransNode);
            groundTransNode.AddChild(groundNode);
        }

        private void CreateObjects()
        {
            // Create a sphere geometry
            {
                GeometryNode sphereNode = new GeometryNode("Sphere");
                sphereNode.Model = new Sphere(3.5f, 20, 20);
                sphereNode.Model.CastShadows = true;
                sphereNode.Model.ReceiveShadows = true;

                Material sphereMat = new Material();
                sphereMat.Diffuse = Color.Red.ToVector4();
                sphereMat.Specular = Color.White.ToVector4();
                sphereMat.SpecularPower = 20;

                sphereNode.Material = sphereMat;

                TransformNode sphereTrans = new TransformNode();
                sphereTrans.Translation = new Vector3(42, 32, 5);

                groundMarkerNode.AddChild(sphereTrans);
                sphereTrans.AddChild(sphereNode);
            }

            // Create a box geometry
            {
                GeometryNode boxNode = new GeometryNode("Box");
                boxNode.Model = new Box(6);
                boxNode.Model.CastShadows = true;
                boxNode.Model.ReceiveShadows = true;

                Material boxMat = new Material();
                boxMat.Diffuse = Color.Blue.ToVector4();
                boxMat.Specular = Color.White.ToVector4();
                boxMat.SpecularPower = 20;

                boxNode.Material = boxMat;

                TransformNode boxTrans = new TransformNode();
                boxTrans.Translation = new Vector3(12, 10, 8);

                groundMarkerNode.AddChild(boxTrans);
                boxTrans.AddChild(boxNode);
            }

            // Create a cylinder geometry
            {
                GeometryNode cylinderNode = new GeometryNode("Cylinder");
                cylinderNode.Model = new Cylinder(3.5f, 3.5f, 10, 20);
                cylinderNode.Model.CastShadows = true;
                cylinderNode.Model.ReceiveShadows = true;

                Material cylinderMat = new Material();
                cylinderMat.Diffuse = Color.Green.ToVector4();
                cylinderMat.Specular = Color.White.ToVector4();
                cylinderMat.SpecularPower = 20;

                cylinderNode.Material = cylinderMat;

                TransformNode cylinderTrans = new TransformNode();
                cylinderTrans.Translation = new Vector3(76, 10, 8);

                groundMarkerNode.AddChild(cylinderTrans);
                cylinderTrans.AddChild(cylinderNode);
            }

            // Create a torus geometry
            {
                GeometryNode torusNode = new GeometryNode("Torus");
                torusNode.Model = new Torus(2.5f, 6.0f, 20, 20);
                torusNode.Model.CastShadows = true;
                torusNode.Model.ReceiveShadows = true;

                Material torusMat = new Material();
                torusMat.Diffuse = Color.Yellow.ToVector4();
                torusMat.Specular = Color.White.ToVector4();
                torusMat.SpecularPower = 20;

                torusNode.Material = torusMat;

                TransformNode torusTrans = new TransformNode();
                torusTrans.Translation = new Vector3(12, 60, 8);

                groundMarkerNode.AddChild(torusTrans);
                torusTrans.AddChild(torusNode);
            }

            // Create a capsule geometry
            {
                GeometryNode capsuleNode = new GeometryNode("Capsule");
                capsuleNode.Model = new Capsule(3, 12, 20);
                capsuleNode.Model.CastShadows = true;
                capsuleNode.Model.ReceiveShadows = true;

                Material capsuleMat = new Material();
                capsuleMat.Diffuse = Color.Cyan.ToVector4();
                capsuleMat.Specular = Color.White.ToVector4();
                capsuleMat.SpecularPower = 20;

                capsuleNode.Material = capsuleMat;

                TransformNode capsuleTrans = new TransformNode();
                capsuleTrans.Translation = new Vector3(76, 60, 8);

                groundMarkerNode.AddChild(capsuleTrans);
                capsuleTrans.AddChild(capsuleNode);
            }
        }

        protected override void LoadContent()
        {
            sampleFont = Content.Load<SpriteFont>("Sample");

            base.LoadContent();
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
            // If it's in stereo mode, and iWear VR920 supports stereo
            if (stereoMode && iTracker.IsStereoAvailable)
            {
                ///////////// First, render the scene for the left eye view /////////////////

                iTracker.UpdateBottomLine(this);
                // Begin GPU query for rendering the scene
                iTracker.BeginGPUQuery();
                // Render the scene for left eye. Note that since base.Draw(..) will update the
                // physics simulation and scene graph as well. 
                base.Draw(gameTime);
                // Renders our own 2D UI for left eye view
                RenderUI();
                // Wait for the GPU to finish rendering
                iTracker.EndGPUQuery();

                // Signal the iWear VR920 that the image for the left eye is ready
                iTracker.SynchronizeEye(iWearDllBridge.Eyes.LEFT_EYE);

                ///////////// Then, render the scene for the right eye view /////////////////

                // Begin GPU query for rendering the scene
                iTracker.BeginGPUQuery();
                // Render the scene for right eye. Note that we called scene.RenderScene(...) instead of
                // base.Draw(...). This is because we do not want to update the scene graph or physics
                // simulation since we want to keep both the left and right eye view in the same time
                // frame. Also, RenderScene(...) is much more light-weighted compared to base.Draw(...).
                // The parameter forces the scene to also render the UI in the right eye view. If this is
                // set to false, then you would only see the UI displayed on the left eye view.
                scene.RenderScene(true);
                // Renders our own 2D UI for right eye view
                RenderUI();
                // Wait for the GPU to finish rendering
                iTracker.EndGPUQuery();

                // Signal the iWear VR920 that the image for the right eye is ready
                iTracker.SynchronizeEye(iWearDllBridge.Eyes.RIGHT_EYE);
            }
            else
            {
                base.Draw(gameTime);
                RenderUI();
            }
        }

        /// <summary>
        /// Renders your 2D UI.
        /// </summary>
        /// <param name="trackerAvailable"></param>
        private void RenderUI()
        {
            UI2DRenderer.WriteText(Vector2.Zero, "Stereoscopic UI", Color.Red,
                sampleFont, Vector2.One, GoblinEnums.HorizontalAlignment.Center,
                GoblinEnums.VerticalAlignment.Top);
        }
    }
}
