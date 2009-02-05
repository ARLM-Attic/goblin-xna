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
using GoblinXNA.Graphics.ParticleEffects;
using Model = GoblinXNA.Graphics.Model;

namespace Tutorial6___Simple_Particle_Systems
{
    /// <summary>
    /// This tutorial demonstrates how to use our easily-extendable particle systems.
    /// </summary>
    public class Tutorial6 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        Scene scene;
        TransformNode shipTransParentNode;

        Random random = new Random();

        double shipAngle = 0;

        public Tutorial6()
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
            //this.IsMouseVisible = true;

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            // Initialize the scene graph
            scene = new Scene(this);

            // Set the background color to CornflowerBlue color. 
            // GraphicsDevice.Clear(...) is called by Scene object with this color. 
            scene.BackgroundColor = Color.CornflowerBlue;

            // Set up the lights used in the scene
            CreateLights();

            // Set up the camera which defines the eye location and viewing frustum
            CreateCamera();

            // Create 3D objects
            CreateObject();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            //scene.PreferPerPixelLighting = true;

            // Show Frames-Per-Second on the screen for debugging
            State.ShowFPS = true;

            base.Initialize();
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
            camera.Translation = new Vector3(0, 50, 120);
            // Rotate the camera -45 degrees along the X axis
            camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX,
                MathHelper.ToRadians(-30));
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

        private void CreateObject()
        {
            // Loads a textured model of a ship
            ModelLoader loader = new ModelLoader();
            Model shipModel = (Model)loader.Load("", "p1_wedge");

            // Create a geometry node of a loaded ship model
            GeometryNode shipNode = new GeometryNode("Ship");
            shipNode.Model = shipModel;
            shipNode.Model.UseInternalMaterials = true;

            // Create a transform node to define the transformation for the ship
            TransformNode shipTransNode = new TransformNode();
            shipTransNode.Translation = new Vector3(0, 0, -50);
            shipTransNode.Scale = new Vector3(0.01f, 0.01f, 0.01f); // It's huge!
            shipTransNode.Rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0),
                MathHelper.ToRadians(90));

            shipTransParentNode = new TransformNode();
            shipTransParentNode.Translation = Vector3.Zero;

            // Create a geometry node with model of a torus
            GeometryNode torusNode = new GeometryNode("Torus");
            torusNode.Model = new Torus(12f, 15.5f, 20, 20);

            TransformNode torusTransNode = new TransformNode();
            torusTransNode.Translation = new Vector3(-50, 0, 0);
            torusTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX,
                MathHelper.ToRadians(90));

            // Create a material node for this torus model
            Material torusMaterial = new Material();
            torusMaterial.Diffuse = Color.DarkGoldenrod.ToVector4();
            torusMaterial.Specular = Color.White.ToVector4();
            torusMaterial.SpecularPower = 5;

            torusNode.Material = torusMaterial;

            // Now add the above nodes to the scene graph in appropriate order
            scene.RootNode.AddChild(shipTransParentNode);
            shipTransParentNode.AddChild(shipTransNode);
            shipTransNode.AddChild(shipNode);

            scene.RootNode.AddChild(torusTransNode);
            torusTransNode.AddChild(torusNode);

            // Now create couple of particle effects to attach to the models

            // Create a smoke particle effect and fire particle effect to simulate a
            // ring of fire around the torus model
            SmokePlumeParticleEffect smokeParticles = new SmokePlumeParticleEffect();
            FireParticleEffect fireParticles = new FireParticleEffect();
            // The order defines which particle effect to render first. Since we want
            // to show the fire particles in front of the smoke particles, we make
            // the smoke particles to be rendered first, and then fire particles
            smokeParticles.DrawOrder = 200;
            fireParticles.DrawOrder = 300;

            // Create a particle node to hold these two particle effects
            ParticleNode fireRingEffectNode = new ParticleNode();
            fireRingEffectNode.ParticleEffects.Add(smokeParticles);
            fireRingEffectNode.ParticleEffects.Add(fireParticles);

            // Implement an update handler for each of the particle effects which will be called
            // every "Update" call 
            fireRingEffectNode.UpdateHandler += new ParticleUpdateHandler(UpdateRingOfFire);

            torusNode.AddChild(fireRingEffectNode);

            // Create another set of fire and smoke particle effects to simulate the fire
            // the ship catches when the ship passes the ring of fire
            FireParticleEffect shipFireEffect = new FireParticleEffect();
            SmokePlumeParticleEffect shipSmokeEffect = new SmokePlumeParticleEffect();
            shipSmokeEffect.DrawOrder = 400;
            shipFireEffect.DrawOrder = 500;

            ParticleNode shipFireNode = new ParticleNode();
            shipFireNode.ParticleEffects.Add(shipFireEffect);
            shipFireNode.ParticleEffects.Add(shipSmokeEffect);

            shipFireNode.UpdateHandler += new ParticleUpdateHandler(UpdateShipFire);

            shipNode.AddChild(shipFireNode);
        }

        /// <summary>
        /// Update the fire effect on the ship model when the ship goes through the torus model
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="particleEffects"></param>
        private void UpdateShipFire(Matrix worldTransform, List<ParticleEffect> particleEffects)
        {
            double diff = (shipAngle % MathHelper.ToRadians(360)) - MathHelper.ToRadians(70);
            // Add fire particles only at certain rotation angle
            if (diff > 0 && diff < MathHelper.ToRadians(180))
            {
                foreach (ParticleEffect particle in particleEffects)
                {
                    // Add different number of fire particles based on the position of the ship
                    // model. We want to add more particles when it goes through the torus model
                    // and decrease the number after that
                    int numParticles = 0;
                    if (particle is FireParticleEffect)
                        numParticles = 8 - (int)(diff * 2);
                    else
                        numParticles = 1;

                    for(int i = 0; i < numParticles; i++)
                        particle.AddParticle(worldTransform.Translation + worldTransform.Forward * 1000, 
                            Vector3.Zero);
                }
            }
        }

        /// <summary>
        /// Update the fire effect on the torus model
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="particleEffects"></param>
        private void UpdateRingOfFire(Matrix worldTransform, List<ParticleEffect> particleEffects)
        {
            foreach (ParticleEffect particle in particleEffects)
            {
                if (particle is FireParticleEffect)
                {
                    // Add 10 fire particles every frame
                    for (int k = 0; k < 10; k++)
                        particle.AddParticle(RandomPointOnCircle(worldTransform.Translation), Vector3.Zero);
                }
                else
                    // Add 1 smoke particle every frame
                    particle.AddParticle(RandomPointOnCircle(worldTransform.Translation), Vector3.Zero);
            }
        }

        /// <summary>
        /// Get a random point on a circle
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        private Vector3 RandomPointOnCircle(Vector3 pos)
        {
            const float radius = 12.5f;

            double angle = random.NextDouble() * Math.PI * 2;

            float x = (float)Math.Cos(angle);
            float y = (float)Math.Sin(angle);

            return new Vector3(x * radius + pos.X, y * radius + pos.Y, pos.Z);
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
            shipAngle += gameTime.ElapsedGameTime.TotalSeconds;
            // Rotate the ship model about the origin along Z axis
            shipTransParentNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY,
                (float)shipAngle);

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
