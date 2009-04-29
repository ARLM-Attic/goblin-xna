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
using GoblinXNA.SceneGraph;
using GoblinXNA.Graphics;
using GoblinXNA.Device.Generic;
using GoblinXNA.Graphics.Geometry;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Physics;

namespace Tutorial5___Simple_Physics
{
    /// <summary>
    /// This tutorial demonstrates how to perform physical simulation using the wrapped Netwon
    /// physics library with our Geometry nodes. 
    /// </summary>
    public class Tutorial5 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        Scene scene;
        int shooterID = 0;
        Material shooterMat;
        Model boxModel;

        public Tutorial5()
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

            // We will use the Newton physics engine (http://www.newtondynamics.com)
            // for processing the physical simulation
            scene.PhysicsEngine = new NewtonPhysics();

            // Set up the lights used in the scene
            CreateLights();

            // Set up the camera which defines the eye location and viewing frustum
            CreateCamera();

            // Create 3D objects
            CreateObjects();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            scene.PreferPerPixelLighting = true;

            // Add a mouse click handler for shooting a box model from the mouse location 
            MouseInput.Instance.MouseClickEvent += new HandleMouseClick(MouseClickHandler);

            // Show some debug information
            State.ShowFPS = true;
            State.ShowTriangleCount = true;

            base.Initialize();
        }

        private void MouseClickHandler(int button, Point mouseLocation)
        {
            // Shoot a box if left mouse button is clicked
            if (button == MouseInput.LeftButton)
            {
                Vector3 nearSource = new Vector3(mouseLocation.X, mouseLocation.Y, 0);
                Vector3 farSource = new Vector3(mouseLocation.X, mouseLocation.Y, 1);

                Vector3 nearPoint = graphics.GraphicsDevice.Viewport.Unproject(nearSource,
                    State.ProjectionMatrix, State.ViewMatrix, Matrix.Identity);
                Vector3 farPoint = graphics.GraphicsDevice.Viewport.Unproject(farSource,
                    State.ProjectionMatrix, State.ViewMatrix, Matrix.Identity);

                ShootBox(nearPoint, farPoint);
            }
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
            lightNode.LightSources.Add(lightSource);

            // Add this light node to the root node
            scene.RootNode.AddChild(lightNode);
        }

        private void CreateCamera()
        {
            // Set up the camera of the scene graph
            Camera camera = new Camera();
            // Put the camera at (0, 0, 0)
            camera.Translation = new Vector3(0, 0, 0);
            // Rotate the camera -20 degrees along the X axis
            camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX,
                MathHelper.ToRadians(-20));
            // Set the vertical field of view to be 45 degrees
            camera.FieldOfViewY = MathHelper.ToRadians(45);
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

        private void CreateObjects()
        {
            // Create a model of box and sphere
            boxModel = new Box(Vector3.One);
            Model sphereModel = new Sphere(1f, 20, 20);

            // Create our ground plane
            GeometryNode groundNode = new GeometryNode("Ground");
            groundNode.Model = boxModel;
            // Make this ground plane collidable, so other collidable objects can collide
            // with this ground
            groundNode.Physics.Collidable = true;
            groundNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            groundNode.AddToPhysicsEngine = true;

            // Create a material for the ground
            Material groundMat = new Material();
            groundMat.Diffuse = Color.LightGreen.ToVector4();
            groundMat.Specular = Color.White.ToVector4();
            groundMat.SpecularPower = 20;

            groundNode.Material = groundMat;

            // Create a parent transformation for both the ground and the sphere models
            TransformNode parentTransNode = new TransformNode();
            parentTransNode.Translation = new Vector3(0, -10, -20);

            // Create a scale transformation for the ground to make it bigger
            TransformNode groundScaleNode = new TransformNode();
            groundScaleNode.Scale = new Vector3(100, 1, 100);

            // Add this ground model to the scene
            scene.RootNode.AddChild(parentTransNode);
            parentTransNode.AddChild(groundScaleNode);
            groundScaleNode.AddChild(groundNode);

            // Create a material that will be applied to all of the sphere models
            Material sphereMaterial = new Material();
            sphereMaterial.Diffuse = Color.Cyan.ToVector4();
            sphereMaterial.Specular = Color.White.ToVector4();
            sphereMaterial.SpecularPower = 10;

            Random rand = new Random();

            // Create bunch of sphere models and pile them up
            for (int i = 0; i <= 4; i++)
            {
                for (int j = -3; j <= 3; j++)
                {
                    TransformNode pileTrans = new TransformNode();
                    pileTrans.Translation = new Vector3(2 * j + (float)rand.NextDouble()/5, 2*i + 5f + (i + 1) * 0.05f,
                        0 + 0.01f * i + (float)rand.NextDouble()/5);

                    GeometryNode gNode = new GeometryNode("Sphere" + (10 * i + j));
                    gNode.Model = sphereModel;
                    gNode.Material = sphereMaterial;
                    // Make the sphere models interactable, which means that they
                    // participate in the physical simulation
                    gNode.Physics.Interactable = true;
                    gNode.Physics.Collidable = true;
                    gNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;
                    gNode.Physics.Mass = 30f;
                    gNode.AddToPhysicsEngine = true;

                    parentTransNode.AddChild(pileTrans);
                    pileTrans.AddChild(gNode);
                }
            }

            // Create a material for shooting box models
            shooterMat = new Material();
            shooterMat.Diffuse = Color.Pink.ToVector4();
            shooterMat.Specular = Color.Yellow.ToVector4();
            shooterMat.SpecularPower = 10;
        }

        /// <summary>
        /// Shoot a box from the clicked mouse location
        /// </summary>
        /// <param name="near"></param>
        /// <param name="far"></param>
        private void ShootBox(Vector3 near, Vector3 far)
        {
            GeometryNode shootBox = new GeometryNode("ShooterBox" + shooterID++);
            shootBox.Model = boxModel;
            shootBox.Material = shooterMat;
            shootBox.Physics.Interactable = true;
            shootBox.Physics.Collidable = true;
            shootBox.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            shootBox.Physics.Mass = 60f;
            shootBox.AddToPhysicsEngine = true;

            // Calculate the direction to shoot the box based on the near and far point
            Vector3 linVel = far - near;
            linVel.Normalize();
            // Multiply the direction with the velocity of 20
            linVel *= 20f;

            // Assign the initial velocity to this shooting box
            shootBox.Physics.InitialLinearVelocity = linVel;
            shootBox.Physics.InitialWorldTransform = Matrix.CreateTranslation(near);

            scene.RootNode.AddChild(shootBox);
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
        }
    }
}
