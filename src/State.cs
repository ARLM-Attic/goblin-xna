/************************************************************************************ 
 * Copyright (c) 2008-2010, Columbia University
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
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.UI;
using GoblinXNA.Helpers;
using GoblinXNA.Shaders;
using GoblinXNA.Graphics;

namespace GoblinXNA
{
    /// <summary>
    /// Options for enabling threading for certain processes.
    /// </summary>
    public enum ThreadOptions
    {
        /// <summary>
        /// Thread the marker tracking process. This includes the video capturing process
        /// as well.
        /// </summary>
        MarkerTracking = (ushort)0x0001,
        /// <summary>
        /// Thread the physics simulation.
        /// </summary>
        PhysicsSimulation = (ushort)0x0002,
    }

    /// <summary>
    /// The main class of the Goblin XNA framework. 
    /// </summary>
    public sealed class State
    {
        #region Member Fields
        private static GraphicsDeviceManager _graphics;
        private static GraphicsDevice _device;
        private static ContentManager _content;
        private static bool initialized;
        private static Texture2D blankTexture;
        private static Matrix viewMatrix;
        private static Matrix projMatrix;
        private static Matrix viewProjMatrix;
        private static Matrix viewInverse;
        private static Matrix cameraTransform;
        private static bool canUsePS20;
        private static bool canUsePS30;

        private static Hashtable settings = new Hashtable();

        private static Log.LogLevel printLevel;
        private static int nextNodeID;

        private static bool enableNetworking;
        private static bool isServer;
        private static int numWaitForClients;

        private static ShadowMapShader shadowShader;
        private static Color boundingBoxColor;

        private static LineManager3D lineManager3D;

        private static bool showFPS;
        private static bool showTriangleCount;
        private static bool showNotifications;
        private static Color debugTextColor;

        private static ushort threadOption;

        // Remember scene render target. This is very important because for our post screen shaders we have to render our whole scene
        // to this render target. But in the process we will use many other shaders and they might set their own render targets and then
        // reset it, but we need to have this scene still to be set. Don't reset to the back buffer (with SetRenderTarget(0, null), this
        // would stop rendering to our scene render target and the post screen shader will not be able to process our screen.		
        private static RenderTarget2D remSceneRenderTarget = null;

        // Remember the last render target we set, this way we can check if the rendertarget was set before calling resolve!		
        private static RenderTarget2D lastSetRenderTarget = null;

        // Remember render to texture instances to allow recreating them all when DeviceReset is called.		
        private static List<RenderToTexture> remRenderToTextures = new List<RenderToTexture>();
        #endregion

        #region Constructors
        static State()
        {
            initialized = false;
        }
        #endregion

        private static void graphics_DeviceReset(object sender, EventArgs e)
        {
            // Re-Set device
            _device = _graphics.GraphicsDevice;

            // Restore z buffer state
            _device.RenderState.DepthBufferEnable = true;
            _device.RenderState.DepthBufferWriteEnable = true;
            // Set u/v addressing back to wrap
            _device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            _device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

            // Recreate all render-targets
            foreach (RenderToTexture renderToTexture in remRenderToTextures)
                renderToTexture.HandleDeviceReset();
        }

        #region Properties
        internal static GraphicsDeviceManager Graphics
        {
            get { return _graphics; }
        }

        public static GraphicsDevice Device
        {
            get { return _device; }
        }

        public static ContentManager Content
        {
            get { return _content; }
        }

        /// <summary>
        /// Gets the width dimension of the current screen in pixels
        /// </summary>
        public static int Width
        {
            get { return _device.Viewport.Width; }
        }

        /// <summary>
        /// Gets the height dimension of the current screen in pixels
        /// </summary>
        public static int Height
        {
            get { return _device.Viewport.Height; }
        }

        /// <summary>
        /// Gets whether Goblin framework has been initialized through the InitGoblin method call
        /// </summary>
        public static bool Initialized
        {
            get { return initialized; }
        }

        /// <summary>
        /// Gets a blank texture
        /// </summary>
        internal static Texture2D BlankTexture
        {
            get { return blankTexture; }
        }

        public static Matrix ViewMatrix
        {
            get { return viewMatrix; }
            internal set 
            { 
                viewMatrix = value;
                viewInverse = Matrix.Invert(viewMatrix);
            }
        }

        public static Matrix ProjectionMatrix
        {
            get { return projMatrix; }
            internal set { projMatrix = value; }
        }

        public static Matrix ViewProjectionMatrix
        {
            get { return viewProjMatrix; }
            internal set { viewProjMatrix = value; }
        }

        public static Matrix ViewInverseMatrix
        {
            get { return viewInverse; }
        }

        internal static Matrix CameraTransform
        {
            get { return cameraTransform; }
            set { cameraTransform = value; }
        }

        /// <summary>
        /// Gets or sets whether depth buffer is enabled.
        /// </summary>
        internal static bool DepthBufferEnabled
        {
            get { return _device.RenderState.DepthBufferEnable; }
            set { _device.RenderState.DepthBufferEnable = value; }
        }

        internal static bool DepthBufferWriteEnabled
        {
            get { return _device.RenderState.DepthBufferWriteEnable; }
            set { _device.RenderState.DepthBufferWriteEnable = value; }
        }

        /// <summary>
        /// Gets or sets whether to enable alpha-blended transparency
        /// </summary>
        internal static bool AlphaBlendingEnabled
        {
            get { return _device.RenderState.AlphaBlendEnable; }
            set
            {
                if (value)
                {
                    _device.RenderState.AlphaBlendEnable = true;
                    _device.RenderState.SourceBlend = Blend.SourceAlpha;
                    _device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                }
                else
                    _device.RenderState.AlphaBlendEnable = false;
            }
        }

        /// <summary>
        /// Gets whether pixel shader 2.0 is supported for the local graphics card
        /// </summary>
        public static bool CanUsePS20
        {
            get { return canUsePS20; }
        }

        /// <summary>
        /// Gets whether pixel shader 3.0 is supported for the local graphics card
        /// </summary>
        public static bool CanUsePS30
        {
            get { return canUsePS30; }
        }

        /// <summary>
        /// Gets or sets the threading options by oring the ThreadOptions enum. This property is
        /// used to control what operations to be threaded. If your CPU is single-core, then there is
        /// no point to thread any operations.
        /// </summary>
        public static ushort ThreadOption
        {
            get { return threadOption; }
            set { threadOption = value; }
        }

        /// <summary>
        /// Gets the shader used to perform shadow mapping
        /// </summary>
        public static ShadowMapShader ShadowShader
        {
            get { return shadowShader; }
            set { shadowShader = value; }
        }

        /// <summary>
        /// Gets or sets the color used to draw the bounding box of each model for debugging.
        /// </summary>
        public static Color BoundingBoxColor
        {
            get { return boundingBoxColor; }
            set { boundingBoxColor = value; }
        }

        public static LineManager3D LineManager
        {
            get { return lineManager3D; }
            set { lineManager3D = value; }
        }

        /// <summary>
        /// Gets or sets whether to enable networking. The default value is false.
        /// </summary>
        public static bool EnableNetworking
        {
            get { return enableNetworking; }
            set { enableNetworking = value; }
        }

        /// <summary>
        /// Gets or sets whether this machine acts as a server when networking is enabled
        /// </summary>
        public static bool IsServer
        {
            get { return isServer; }
            set { isServer = value; }
        }

        /// <summary>
        /// Number of clients to wait for connections before starting physics simulation.
        /// The default value is 0.
        /// </summary>
        public static int NumberOfClientsToWait
        {
            get { return numWaitForClients; }
            set { numWaitForClients = value; }
        }

        /// <summary>
        /// Gets or sets the log levels that will be printed out. Default value is LogLevel.Error.
        /// </summary>
        /// <example>
        /// LogLevel.Log means prints out all log levels including Warning and Error messages.
        /// LogLevel.Warning means prints out only Warning and Error messages.
        /// LogLevel.Error means prints out only Error messages.
        /// </example>
        public static Log.LogLevel LogPrintLevel
        {
            get { return printLevel; }
            set { printLevel = value; }
        }

        /// <summary>
        /// Gets or sets whether to display the Frames-Per-Secound count on the screen.
        /// </summary>
        /// <remarks>
        /// The color of the text can be changed by modifying DebutTextColor property. 
        /// </remarks>
        /// <see cref="DebugTextColor"/>
        public static bool ShowFPS
        {
            get { return showFPS; }
            set { showFPS = value; }
        }

        /// <summary>
        /// Gets or sets whether to display any notification messages on the screen.
        /// </summary>
        public static bool ShowNotifications
        {
            get { return showNotifications; }
            set { showNotifications = value; }
        }

        /// <summary>
        /// Gets or sets whether to display the triangle count of all of the rendered 
        /// models in the scene.
        /// </summary>
        /// <remarks>
        /// The color of the text can be changed by modifying DebutTextColor property. 
        /// </remarks>
        /// <see cref="DebugTextColor"/>
        public static bool ShowTriangleCount
        {
            get { return showTriangleCount; }
            set { showTriangleCount = value; }
        }

        /// <summary>
        /// Gets or sets the color of the FPS or triangle count text. The default color 
        /// is Color.White.
        /// </summary>
        public static Color DebugTextColor
        {
            get { return debugTextColor; }
            set { debugTextColor = value; }
        }

        #endregion

        #region Public Static Methods
        /// <summary>
        /// This is the very first method that needs to be called before using any of the Goblin 
        /// XNA framework.
        /// </summary>
        /// <param name="graphics">GraphicsDeviceManager object from the main Game class</param>
        /// <param name="content">ContentManager object from the main Game class</param>
        /// <param name="settingFile">
        /// The full path of the setting file in XML format. Setting file is used, for example, 
        /// to specify where the model files are stored if not directly under "Content" directory.
        /// You can also add your own setting variable with certain value, and retrieve the value
        /// using GetSettingVariable method.
        /// 
        /// Can be an empty string, in which case, a template setting file (template_setting.xml) 
        /// that contains all of the setting variables used in Goblin XNA will be generated. 
        /// If you don't specify the setting file, then all of the resource files (e.g., models, 
        /// textures, spritefonts, etc) should be directly stored under the "Content" directory,
        /// so Goblin XNA can figure out where to load those resources. 
        /// </param>
        /// <see cref="GetSettingVariable"/>
        /// <exception cref="GoblinException"></exception>
        public static void InitGoblin(GraphicsDeviceManager graphics, ContentManager content,
            String settingFile)
        {
            if (graphics == null || content == null)
                throw new GoblinException("graphics or content can not be null");

            _graphics = graphics;
            _device = graphics.GraphicsDevice;
            _graphics.DeviceReset += new EventHandler(graphics_DeviceReset);
            _content = content;
            initialized = true;
            // creates a blank texture for 2D primitive drawing when texture is not needed
            blankTexture = new Texture2D(_device, 1, 1, 1, TextureUsage.AutoGenerateMipMap,
                SurfaceFormat.Bgra5551);

            // puts a white pixel in this blank texture to make a 1x1 blank texture
            ushort[] texData = new ushort[1];
            texData[0] = (ushort)0xFFFF;
            blankTexture.SetData(texData);
            viewMatrix = Matrix.Identity;
            projMatrix = Matrix.Identity;

            canUsePS20 = _device.GraphicsDeviceCapabilities.PixelShaderVersion.Major >= 2;
            canUsePS30 = _device.GraphicsDeviceCapabilities.PixelShaderVersion.Major >= 3;

            // bounding box color for drawing 3D models' bounding box
            boundingBoxColor = Color.Red;
            
            lineManager3D = new LineManager3D();

            if (settingFile.Length != 0)
                LoadSettings(settingFile);
            else
                WriteSettingTemplate();

            printLevel = Log.LogLevel.Error;
            nextNodeID = 0;

            showFPS = false;
            showNotifications = false;
            showTriangleCount = false;
            debugTextColor = Color.White;

            enableNetworking = false;
            isServer = false;
            numWaitForClients = 0;

            cameraTransform = Matrix.Identity;

            threadOption = 0;
        }

        /// <summary>
        /// Gets the setting variables loaded at the initialization time.
        /// </summary>
        /// <param name="name">The name of the setting variable</param>
        /// <returns></returns>
        public static String GetSettingVariable(String name)
        {
            if (!settings.ContainsKey(name))
                return "";
            else
                return (String)settings[name];
        }
        #endregion

        /// <summary>
        /// Loads all of the added setting variables.
        /// </summary>
        /// <param name="filename">the filename where the setting variables are stored</param>
        private static void LoadSettings(String filename)
        {
            XmlTextReader reader = new XmlTextReader(filename);

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name.Equals("var"))
                            if (reader.AttributeCount == 2)
                                settings.Add(reader.GetAttribute("name"), reader.GetAttribute("value"));
                        break;
                }
            }
        }

        /// <summary>
        /// Writes out a template setting file that contains all of the setting variables
        /// used in Goblin XNA.
        /// </summary>
        private static void WriteSettingTemplate()
        {
            XmlTextWriter writer = new XmlTextWriter("template_setting.xml", null);
            writer.WriteStartDocument();

            writer.WriteStartElement("GoblinXNASettings");

            writer.WriteComment("Specifies the file path in which to write out the log file");
            writer.WriteComment("If not specified, the default path is where the executable is with " +
                "file name 'log.txt'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "LogFileName");
            writer.WriteAttributeString("value", "GoblinXNALog.txt");
            writer.WriteEndElement();

            writer.WriteComment("Specifies where the texture resources are stored under 'Content' directory");
            writer.WriteComment("You don't need to specify this if directly stored under 'Content'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "TextureDirectory");
            writer.WriteAttributeString("value", "Textures");
            writer.WriteEndElement();

            writer.WriteComment("Specifies where the shader (.fx) resources are stored under 'Content' directory");
            writer.WriteComment("You don't need to specify this if directly stored under 'Content'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "ShaderDirectory");
            writer.WriteAttributeString("value", "Shaders");
            writer.WriteEndElement();

            writer.WriteComment("Specifies where the 3D model resources are stored under 'Content' directory");
            writer.WriteComment("You don't need to specify this if directly stored under 'Content'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "ModelDirectory");
            writer.WriteAttributeString("value", "Models");
            writer.WriteEndElement();

            writer.WriteComment("Specifies where the font resources are stored under 'Content' directory");
            writer.WriteComment("You don't need to specify this if directly stored under 'Content'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "FontDirectory");
            writer.WriteAttributeString("value", "Fonts");
            writer.WriteEndElement();

            writer.WriteComment("Specifies where the audio resources are stored under 'Content' directory");
            writer.WriteComment("You don't need to specify this if directly stored under 'Content'");
            writer.WriteStartElement("var");
            writer.WriteAttributeString("name", "AudioDirectory");
            writer.WriteAttributeString("value", "Audio");
            writer.WriteEndElement();

            writer.WriteEndElement(); // "Goblin XNA Settings" element

            writer.WriteEndDocument();
            writer.Close();
        }

        /// <summary>
        /// A helper function to get the next unique node ID.
        /// </summary>
        /// <returns></returns>
        internal static int GetNextNodeID()
        {
            nextNodeID++;
            return nextNodeID;
        }

        internal static void Restore3DSettings()
        {
            State.Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            State.Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
        }

        #region Render targets
        // Add render to texture instance to allow recreating them all when DeviceReset is called with help of the remRenderToTextures list. 
        internal static void AddRemRenderToTexture(RenderToTexture renderToTexture)
        {
            remRenderToTextures.Add(renderToTexture);
        }

        // Current render target we have set, null if it is just the back buffer.		
        internal static RenderTarget2D CurrentRenderTarget
        {
            get { return lastSetRenderTarget; }
        }

        // Set render target		
        internal static void SetRenderTarget(RenderTarget2D renderTarget, bool isSceneRenderTarget)
        {
            _device.SetRenderTarget(0, renderTarget);
            if (isSceneRenderTarget)
                remSceneRenderTarget = renderTarget;
            lastSetRenderTarget = renderTarget;
        }

        // Reset render target		
        internal static void ResetRenderTarget(bool fullResetToBackBuffer)
        {
            if (remSceneRenderTarget == null || fullResetToBackBuffer)
            {
                remSceneRenderTarget = null;
                lastSetRenderTarget = null;
                _device.SetRenderTarget(0, null);
            }
            else
            {
                _device.SetRenderTarget(0, remSceneRenderTarget);
                lastSetRenderTarget = remSceneRenderTarget;
            }
        }
        #endregion Render targets
    }
}
