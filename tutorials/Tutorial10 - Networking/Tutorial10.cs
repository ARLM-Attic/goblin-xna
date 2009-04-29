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
using GoblinXNA.Helpers;
using GoblinXNA.Graphics;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Device.Generic;
using GoblinXNA.Device;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Network;
using GoblinXNA.Physics;

namespace Tutorial10___Networking
{
    /// <summary>
    /// This tutorial demonstrates how to use Goblin XNA's networking capabilities with
    /// server-client model. In order to run both server and client on the same machine
    /// you need to copy the generated .exe file to other folder, and set one of them
    /// to be the server, and the other to be the client (isServer = false). If you're
    /// running the server and client on different machines, then you can simply run the
    /// project (of course, you will need to set one of them to be client).
    /// </summary>
    public class Tutorial10 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        // A Goblin XNA scene graph
        Scene scene;

        // A material for the shooting boxes
        Material shootMat;
        int shooterID = 0;

        // A network object which transmits mouse press information
        MouseNetworkObject mouseNetworkObj;

        // Indicates whether this is a server
        bool isServer;

        public Tutorial10(bool isServer)
        {
            this.isServer = isServer;
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
            this.IsMouseVisible = true;

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene(this);

            State.EnableNetworking = true;
            State.IsServer = isServer;

            // Set up the lights used in the scene
            CreateLights();

            // Set up the camera, which defines the eye location and viewing frustum
            CreateCamera();

            // Create 3D objects
            CreateObject();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            scene.PreferPerPixelLighting = true;

            // Create a network object that contains mouse press information to be
            // transmitted over network
            mouseNetworkObj = new MouseNetworkObject();

            // When a mouse press event is sent from the other side, then call "ShootBox"
            // function
            mouseNetworkObj.CallbackFunc = ShootBox;

            scene.PhysicsEngine = new NewtonPhysics();

            if (State.IsServer)
                scene.NetworkServer = new LidgrenServer("Tutorial10", 14242);
            else
            {
                // Create a client that connects to the local machine assuming that both
                // the server and client will be running on the same machine. In order to 
                // connect to a remote machine, you need to either pass the host name or
                // the IP address of the remote machine in the 3rd parameter. 
                LidgrenClient client = new LidgrenClient("Tutorial10", 14242, "Localhost");

                // If the server is not running when client is started, then wait for the
                // server to start up.
                client.WaitForServer = true;

                scene.NetworkClient = client;
            }

            // Add the mouse network object to the scene graph, so it'll be sent over network
            // whenever ReadyToSend is set to true.
            scene.AddNetworkObject(mouseNetworkObj);

            MouseInput.Instance.MousePressEvent += new HandleMousePress(MouseInput_MousePressEvent);

            base.Initialize();
        }

        private void CreateLights()
        {
            // Create a directional light source
            LightSource lightSource = new LightSource();
            lightSource.Direction = new Vector3(-1, -1, 0);
            lightSource.Diffuse = Color.White.ToVector4();
            lightSource.Specular = Color.White.ToVector4();

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

            if (State.IsServer)
                camera.Translation = new Vector3(0, 0, 10);
            else
            {
                camera.Translation = new Vector3(0, 0, -30);
                camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.Pi);
            }
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
            GeometryNode sphereNode = new GeometryNode("Sphere");
            sphereNode.Model = new Sphere(3, 20, 20);
            sphereNode.Model.ShowBoundingBox = true;

            Material sphereMaterial = new Material();
            sphereMaterial.Diffuse = Color.Red.ToVector4();
            sphereMaterial.Ambient = Color.Blue.ToVector4();
            sphereMaterial.Emissive = Color.Green.ToVector4();

            sphereNode.Material = sphereMaterial;

            TransformNode transNode = new TransformNode();

            GeometryNode cylinderNode = new GeometryNode("Cylinder");
            cylinderNode.Model = new Cylinder(3, 3, 8, 20);
            cylinderNode.Model.ShowBoundingBox = true;

            Material cylinderMat = new Material();
            cylinderMat.Diffuse = Color.Cyan.ToVector4();
            cylinderMat.Specular = Color.Yellow.ToVector4();
            cylinderMat.SpecularPower = 5;

            cylinderNode.Material = cylinderMat;

            TransformNode parentTrans = new TransformNode();
            parentTrans.Translation = new Vector3(0, -2, -10);

            cylinderNode.Physics.Collidable = true;
            cylinderNode.Physics.Interactable = true;
            cylinderNode.AddToPhysicsEngine = true;
            cylinderNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Cylinder;
            cylinderNode.Physics.Mass = 200;

            sphereNode.Physics.Collidable = true;
            sphereNode.Physics.Interactable = true;
            sphereNode.AddToPhysicsEngine = true;
            sphereNode.Physics.Shape = GoblinXNA.Physics.ShapeType.Sphere;
            sphereNode.Physics.Mass = 0;

            transNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), -MathHelper.PiOver2);
            transNode.Translation = new Vector3(0, 12, 0);

            scene.RootNode.AddChild(parentTrans);
            parentTrans.AddChild(sphereNode);
            parentTrans.AddChild(transNode);
            transNode.AddChild(cylinderNode);

            shootMat = new Material();
            shootMat.Diffuse = Color.Pink.ToVector4();
            shootMat.Specular = Color.Yellow.ToVector4();
            shootMat.SpecularPower = 10;
        }

        private void MouseInput_MousePressEvent(int button, Point mouseLocation)
        {
            if (button == MouseInput.LeftButton)
            {
                Vector3 nearSource = new Vector3(mouseLocation.X, mouseLocation.Y, 0);
                Vector3 farSource = new Vector3(mouseLocation.X, mouseLocation.Y, 1);

                Vector3 nearPoint = graphics.GraphicsDevice.Viewport.Unproject(nearSource,
                        State.ProjectionMatrix, State.ViewMatrix, Matrix.Identity);
                Vector3 farPoint = graphics.GraphicsDevice.Viewport.Unproject(farSource,
                    State.ProjectionMatrix, State.ViewMatrix, Matrix.Identity);

                // Shoot the box model
                ShootBox(nearPoint, farPoint);

                // Set ReadyToSend to true so that the scene graph will handle the transfer
                // NOTE: Once it is sent, ReadyToSend will be set to false automatically
                mouseNetworkObj.ReadyToSend = true;

                // Pass the necessary information to be sent
                mouseNetworkObj.PressedButton = button;
                mouseNetworkObj.NearPoint = nearPoint;
                mouseNetworkObj.FarPoint = farPoint;
            }
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        protected override void LoadContent()
        {
            base.LoadContent();
        }

        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        protected override void  UnloadContent()
        {
            Content.Unload();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <summary>
        /// Shoot a box from the clicked mouse location
        /// </summary>
        /// <param name="near"></param>
        /// <param name="far"></param>
        private void ShootBox(Vector3 near, Vector3 far)
        {
            Vector3 camPos = scene.CameraNode.Camera.Translation;

            GeometryNode shootBox = new GeometryNode("ShooterBox" + shooterID++);
            shootBox.Model = new Box(1);
            shootBox.Material = shootMat;
            shootBox.Physics.Interactable = true;
            shootBox.Physics.Collidable = true;
            shootBox.Physics.Shape = GoblinXNA.Physics.ShapeType.Box;
            shootBox.Physics.Mass = 20f;
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
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
