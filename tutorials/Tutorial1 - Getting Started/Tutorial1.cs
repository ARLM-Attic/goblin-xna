using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Generic;
using GoblinXNA.UI.UI2D;

// A GoblinXNA project is created by selecting the project type "XNA Game Studio 2.0"
// under "Visual C#", and selecting the template "Windows Game(2.0)".

namespace Tutorial1___Getting_Started
{
    public class Tutorial1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        // A GoblinXNA scene graph
        Scene scene;
        // A sprite font to draw a 2D text string
        SpriteFont textFont;

        public Tutorial1()
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
            // Display the mouse cursor
            this.IsMouseVisible = true;
            
            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene(this);

            // Set the background color to CornflowerBlue color. 
            // GraphicsDevice.Clear(...) is called by Scene object with this color. 
            scene.BackgroundColor = Color.CornflowerBlue;

            // Set up the lights used in the scene
            CreateLights();

            // Set up the camera, which defines the eye location and viewing frustum
            CreateCamera();

            // Create 3D objects
            CreateObject();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            scene.PreferPerPixelLighting = true;

            // Show Frames-Per-Second on the screen for debugging
            State.ShowFPS = true;

            base.Initialize();
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(-1, -1, -1);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = new Vector4(0.6f, 0.6f, 0.6f, 1);

            // Create a light node to hold the light source
            LightNode lightNode = new LightNode();
            lightNode.LightSources.Add(lightSource);

            // Add this light node to the root node
            scene.RootNode.AddChild(lightNode);
        }

        private void CreateCamera()
        {
            // Create a camera 
            Camera camera = new Camera();
            // Put the camera at the origin
            camera.Translation = new Vector3(0, 0, 0);
            // Set the vertical field of view to be 60 degrees
            camera.FieldOfViewY = MathHelper.ToRadians(60);
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

        private void CreateObject()
        {
            // Create a geometry node with a model of a sphere 
            // NOTE: We strongly recommend you give each geometry node a unique name for use with
            // the physics engine and networking; if you leave out the name, it will be automatically 
            // generated
            GeometryNode sphereNode = new GeometryNode("Sphere");
            sphereNode.Model = new Sphere(3, 60, 60);

            // Create a transform node to define the transformation of this sphere
            // (Transformation includes translation, rotation, and scaling)
            TransformNode sphereTransNode = new TransformNode();
            // We want to scale the sphere by half in all three dimensions and
            // translate the sphere 5 units back along the Z axis 
            sphereTransNode.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            sphereTransNode.Translation = new Vector3(0, 0, -5);

            // Create a material which we want to apply to the sphere model
            Material sphereMaterial = new Material();
            sphereMaterial.Diffuse = new Vector4(0.5f, 0, 0, 1);
            sphereMaterial.Specular = Color.White.ToVector4();
            sphereMaterial.SpecularPower = 10;

            // Apply this material to the sphere model
            sphereNode.Material = sphereMaterial;

            // Now add the above nodes to the scene graph in the appropriate order.
            // Child nodes are affected by parent nodes. In this case, we want to make 
            // the sphere node have the transformation, so we add the transform node to 
            // the root node, and then add the sphere node to the transform node.
            scene.RootNode.AddChild(sphereTransNode);
            sphereTransNode.AddChild(sphereNode);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            textFont = Content.Load<SpriteFont>("Sample");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            Content.Unload();
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
            base.Draw(gameTime);

            // Draw a 2D text string at the center of the screen
            // NOTE: Since scene.Draw(..) (which is called by base.Draw(..)) will clear the 
            // background this WriteText function should be called after base.Draw(..). 
            UI2DRenderer.WriteText(Vector2.Zero, "Hello World!!", Color.GreenYellow, textFont,
                GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Center);
        }
    }
}
