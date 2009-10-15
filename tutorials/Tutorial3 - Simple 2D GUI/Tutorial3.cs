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
using GoblinXNA.UI;
using GoblinXNA.UI.UI2D;
using GoblinXNA.SceneGraph;
using GoblinXNA.Graphics;
using GoblinXNA.Graphics.Geometry;

namespace Tutorial3___Simple_2D_GUI
{
    /// <summary>
    /// This tutorial introduce GoblinXNA's 2D GUI facilities.
    /// </summary>
    public class Tutorial3 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;

        Scene scene;
        TransformNode cylinderTransNode;
        static string label;
        static G2DRadioButton sliderRadio;
        SpriteFont textFont;
        SpriteFont uiFont;
        SpriteFont sliderFont;
        static float rotationRate = 1;
        float rotationAngle = 0;

        public Tutorial3()
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
            // TODO: Add your initialization logic here

            // Display the mouse cursor
            this.IsMouseVisible = true;

            // Initialize the GoblinXNA framework
            State.InitGoblin(graphics, Content, "");

            scene = new Scene(this);

            // Set the background color to CornflowerBlue color. 
            // GraphicsDevice.Clear(...) is called by Scene object with this color. 
            scene.BackgroundColor = Color.CornflowerBlue;

            // Set up the lights used in the scene
            CreateLights();

            // Set up the camera, which defines the eye location and viewing frustum
            CreateCamera();

            // Create a 3D object
            CreateObject();

            // Create 2D GUI
            Create2DGUI();

            // Use per pixel lighting for better quality (If you using non NVidia graphics card,
            // setting this to true may reduce the performance significantly)
            scene.PreferPerPixelLighting = true;

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
            // Create a geometry node with a model of a cylinder 
            GeometryNode cylinderNode = new GeometryNode("Cylinder");
            cylinderNode.Model = new Cylinder(1, 1, 3, 20);

            // Create a transform node to define the transformation of this cylinder
            cylinderTransNode = new TransformNode();
            cylinderTransNode.Translation = new Vector3(0, 0, -5);

            // Create a material to apply to the cylinder model
            Material cylinderMaterial = new Material();
            cylinderMaterial.Diffuse = Color.Violet.ToVector4();
            cylinderMaterial.Specular = Color.White.ToVector4();
            cylinderMaterial.SpecularPower = 10;

            cylinderNode.Material = cylinderMaterial;

            scene.RootNode.AddChild(cylinderTransNode);
            cylinderTransNode.AddChild(cylinderNode);
        }

        private void Create2DGUI()
        {
            // Create the main panel which holds all other GUI components
            G2DPanel frame = new G2DPanel();
            frame.Bounds = new Rectangle(325, 370, 150, 170);
            frame.Border = GoblinEnums.BorderFactory.LineBorder;
            frame.Transparency = 0.7f;  // Ranges from 0 (fully transparent) to 1 (fully opaque)

            label = "User Interfaces";

            // Loads the fonts used for rendering UI labels and slider labels
            uiFont = Content.Load<SpriteFont>("UIFont");
            sliderFont = Content.Load<SpriteFont>("SliderFont");

            // Create four Radio Buttons
            G2DRadioButton radio1 = new G2DRadioButton("User Interfaces");
            radio1.TextFont = uiFont;
            radio1.Bounds = new Rectangle(10, 5, 80, 20);
            radio1.DoClick(); // make this radio button selected initially
            radio1.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            G2DRadioButton radio2 = new G2DRadioButton("Computer Graphics");
            radio2.TextFont = uiFont;
            radio2.Bounds = new Rectangle(10, 25, 80, 20);
            radio2.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            G2DRadioButton radio3 = new G2DRadioButton("Augmented Reality");
            radio3.TextFont = uiFont;
            radio3.Bounds = new Rectangle(10, 45, 80, 20);
            radio3.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            sliderRadio = new G2DRadioButton("Slider Control");
            sliderRadio.TextFont = uiFont;
            sliderRadio.Bounds = new Rectangle(10, 65, 80, 20);
            sliderRadio.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            // Create a slider
            G2DSlider slider = new G2DSlider();
            slider.TextFont = sliderFont;
            slider.Bounds = new Rectangle(5, 100, 140, 30);
            slider.Maximum = 40;
            slider.MajorTickSpacing = 20;
            slider.MinorTickSpacing = 5;
            slider.PaintTicks = true;
            slider.PaintLabels = true;
            slider.StateChangedEvent += new StateChanged(HandleStateChanged);

            // Create a ButtonGroup object which controls the radio 
            // button selections
            ButtonGroup group = new ButtonGroup();
            group.Add(radio1);
            group.Add(radio2);
            group.Add(radio3);
            group.Add(sliderRadio);

            // Create a Button
            G2DButton button = new G2DButton("I Love");
            button.TextFont = uiFont;
            button.Bounds = new Rectangle(50, 145, 50, 20);
            button.ActionPerformedEvent += new ActionPerformed(HandleActionPerformed);

            // Add all of the components to the main panel
            frame.AddChild(radio1);
            frame.AddChild(radio2);
            frame.AddChild(radio3);
            frame.AddChild(sliderRadio);
            frame.AddChild(button);
            frame.AddChild(slider);

            scene.UIRenderer.Add2DComponent(frame);
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
            rotationAngle += (float)gameTime.ElapsedGameTime.TotalSeconds * rotationRate;
            // TODO: Add your update logic here
            cylinderTransNode.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ,
                rotationAngle);

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
            UI2DRenderer.WriteText(Vector2.Zero, label, Color.Red,
                textFont, GoblinEnums.HorizontalAlignment.Center, GoblinEnums.VerticalAlignment.Top);
        }

        /// <summary>
        /// Handles action performed events.
        /// </summary>
        /// <param name="source"></param>
        private void HandleActionPerformed(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DButton)
                label = comp.Text + " Goblin XNA!!";
            else if (comp is G2DRadioButton)
                label = comp.Text;
        }

        /// <summary>
        /// Handles state changed events.
        /// </summary>
        /// <param name="source"></param>
        private void HandleStateChanged(object source)
        {
            G2DComponent comp = (G2DComponent)source;
            if (comp is G2DSlider)
            {
                sliderRadio.DoClick();
                label = "Slider value is " + ((G2DSlider)comp).Value;
                rotationRate = ((G2DSlider)comp).Value * 0.2f;
            }
        }
    }
}
