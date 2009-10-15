/************************************************************************************ 
 * Copyright (c) 2008-2009, Columbia University
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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;
using GoblinXNA.Graphics.ParticleEffects;
using GoblinXNA.Shaders;
using GoblinXNA.Physics;
using GoblinXNA.Network;
using GoblinXNA.Helpers;
using GoblinXNA.UI;
using GoblinXNA.Device;
using GoblinXNA.Device.Vision.Marker;
using GoblinXNA.Device.Capture;
using GoblinXNA.Sounds;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// The most important class in Goblin XNA that handles 3D scene processing and rendering.
    /// </summary>
    public class Scene : DrawableGameComponent
    {
        #region Member Fields
        /// <summary>
        /// The root node of this scene graph
        /// </summary>
        protected BranchNode rootNode;
        /// <summary>
        /// A hash table that stores Node objects with their names as the keys
        /// </summary>
        protected Dictionary<String, Node> nodeTable;
        /// <summary>
        /// A list of geometry nodes that need to be rendered organized in its groupID
        /// </summary>
        protected Dictionary<int, List<GeometryNode>> nodeRenderGroups;
        /// <summary>
        /// A list of geometry nodes that are opaque (same as nodeRenderGroups but in
        /// a 1dimensional list for easy searching)
        /// </summary>
        protected List<GeometryNode> opaqueGroups;
        /// <summary>
        /// A list of boolean values indicating whether nodes with certain groupID should
        /// be rendered
        /// </summary>
        protected Dictionary<int, bool> renderGroups;
        /// <summary>
        /// A list of geometry nodes that are transparent
        /// </summary>
        protected List<GeometryNode> transparentGroup;
        /// <summary>
        /// A comparer for sorting the drawing order of transparent geometries
        /// </summary>
        protected IComparer<GeometryNode> transparencySortOrder;
        /// <summary>
        /// Indicates whether the transparent nodes should be re-sorted
        /// </summary>
        protected bool needTransparencySort;
        /// <summary>
        /// A list of geometry nodes that function as occluders
        /// </summary>
        protected List<GeometryNode> occluderGroup;
        /// <summary>
        /// A list of particle effects that need to be rendered
        /// </summary>
        protected List<ParticleNode> renderedEffects;
        /// <summary>
        /// A list of network objects that can be transferred over the network
        /// </summary>
        protected Dictionary<String, NetObj> networkObjects;
        /// <summary>
        /// The network server implementation used in this scene graph
        /// </summary>
        protected IServer networkServer;
        /// <summary>
        /// The network client implementation used in this scene graph
        /// </summary>
        protected IClient networkClient;
        /// <summary>
        /// The current camera node associated with this scene graph
        /// </summary>
        protected CameraNode cameraNode;
        protected Vector3 prevCameraTrans;
        /// <summary>
        /// The physics engine implementation used in this scene graph
        /// </summary>
        protected IPhysics physicsEngine;
        /// <summary>
        /// A list of global light sources in this scene graph
        /// </summary>
        protected List<LightNode> globalLights;
        protected Stack<LightNode> localLights;

        protected List<LODNode> lodNodes;

        protected GoblinXNA.Graphics.Environment environment;

        protected SpriteBatch spriteBatch;

        protected IMarkerTracker tracker;
        protected bool trackMarkers;
        protected bool markerModuleInited;
        protected List<MarkerNode> markerUpdateList;

        protected IShader aabbShader;
        protected Color aabbColor;
        protected IShader cmeshShader;
        protected Color cmeshColor;
        protected bool enableShadowMapping;
        protected bool enableLighting;
        protected bool preferPerPixelLighting;

        protected bool enableFrustumCulling;

        protected UIRenderer uiRenderer;
        private Matrix prevMatrix;

        protected int triangleCount;
        /// <summary>
        /// Used for physics engine debugging
        /// </summary>
        protected bool renderAabb;
        protected bool renderCollisionMesh;

        protected Texture2D backgroundTexture;
        protected Color backgroundColor;

        protected List<LightSource> globalLightSources;

        #region For Networking
        protected List<byte[]> networkMessages;
        protected List<byte> reliableInOrderMsgs;
        protected List<byte> unreliableInOrderMsgs;
        protected List<byte> reliableUnOrderMsgs;
        protected List<byte> unreliableUnOrderMsgs;
        #endregion

        /// <summary>
        /// For stereo rendering
        /// </summary>
        protected bool renderLeftView;

        protected Thread updateThread;
        protected bool readyToUpdateTracker;
        protected int[] videoImage;
        protected int[] tmpImage;

        // Indicates whether the scene graph is currently being processed. This is used
        // to avoid adding a node while processing (removing while processing is acceptable)
        protected bool processing;

        #region For Augmented Reality Scene
        protected bool showCameraImage;
        protected int overlayVideoID;
        protected int trackerVideoID;
        protected int actualOverlayVideoID;
        protected int actualTrackerVideoID;
        protected bool overlayAndTrackerUseSameID;
        protected Dictionary<int, int> videoIDs;
        protected bool freezeVideo;

        // These variables are used to avoid updating the tracker while changing the video overlay
        // ID or tracker ID, or visa versa
        protected bool waitForVideoIDChange;
        protected bool waitForTrackerUpdate;

        protected float prevTrackerTime;

        #region For Stereo Augmented Reality
        protected int leftEyeVideoID;
        protected int rightEyeVideoID;
        protected bool singleVideoStereo;
        protected int actualLeftEyeVideoID;
        protected int actualRightEyeVideoID;
        protected int leftEyeVideoImageShift;
        protected int rightEyeVideoImageShift;
        protected Rectangle videoVisibleArea;
        #endregion

        #endregion

        #region Temporary Variables for Optimized Calculation

        protected Matrix tmpMat1;
        protected Matrix tmpMat2;
        protected Matrix tmpMat3;
        protected Quaternion tmpQuat1;
        protected Vector3 tmpVec1;
        protected Vector3 tmpVec2;
        protected Vector3 tmpVec3;

        protected Material emptyMaterial;
        protected List<LightNode> emptyLightList; // for reducing redundant List creation everytime passing zero lights

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a 3D scene.
        /// </summary>
        /// <param name="mainGame">The main Game class</param>
        public Scene(Game mainGame) : base(mainGame)
        {
            uiRenderer = new UIRenderer(mainGame);
            mainGame.Components.Add(this);
            this.DrawOrder = 101;

            spriteBatch = new SpriteBatch(State.Device);

            rootNode = new BranchNode("Root");
            rootNode.SceneGraph = this;
            nodeTable = new Dictionary<string, Node>();
            
            nodeRenderGroups = new Dictionary<int, List<GeometryNode>>();
            renderGroups = new Dictionary<int, bool>();
            transparentGroup = new List<GeometryNode>();
            opaqueGroups = new List<GeometryNode>();
            needTransparencySort = false;
            renderedEffects = new List<ParticleNode>();
            occluderGroup = new List<GeometryNode>();

            transparencySortOrder = new DefaultTransparencyComparer();

            networkObjects = new Dictionary<String, NetObj>();
            cameraNode = null;
            prevCameraTrans = new Vector3();

            globalLights = new List<LightNode>();
            localLights = new Stack<LightNode>();
            globalLightSources = new List<LightSource>();

            emptyLightList = new List<LightNode>();
            emptyMaterial = new Material();

            lodNodes = new List<LODNode>();

            environment = null;

            enableShadowMapping = false;
            enableLighting = true;
            preferPerPixelLighting = false;

            enableFrustumCulling = true;

            aabbColor = Color.YellowGreen;
            aabbShader = new SimpleEffectShader();
            Material aabbMat = new Material();
            aabbMat.Ambient = aabbColor.ToVector4();
            aabbMat.Diffuse = aabbColor.ToVector4();
            aabbMat.Emissive = aabbColor.ToVector4();
            aabbMat.Specular = aabbColor.ToVector4();
            aabbShader.SetParameters(aabbMat);

            cmeshColor = Color.Purple;
            cmeshShader = new SimpleEffectShader();
            Material cmeshMat = new Material();
            cmeshMat.Ambient = cmeshColor.ToVector4();
            cmeshMat.Diffuse = cmeshColor.ToVector4();
            cmeshMat.Emissive = cmeshColor.ToVector4();
            cmeshMat.Specular = cmeshColor.ToVector4();
            cmeshShader.SetParameters(cmeshMat);

            markerModuleInited = false;

            triangleCount = 0;
            renderAabb = false;
            renderCollisionMesh = false;

            renderLeftView = true;

            backgroundTexture = null;
            backgroundColor = Color.CornflowerBlue;

            showCameraImage = false;
            actualOverlayVideoID = -1;
            trackerVideoID = 0;
            actualTrackerVideoID = 0;
            overlayAndTrackerUseSameID = true;
            videoIDs = new Dictionary<int, int>();
            readyToUpdateTracker = false;
            freezeVideo = false;
            waitForVideoIDChange = false;
            waitForTrackerUpdate = false;

            markerUpdateList = new List<MarkerNode>();
            prevTrackerTime = 0;

            leftEyeVideoID = -1;
            rightEyeVideoID = -1;
            actualLeftEyeVideoID = -1;
            actualRightEyeVideoID = -1;
            leftEyeVideoImageShift = 0;
            rightEyeVideoImageShift = 0;
            singleVideoStereo = true;
            videoVisibleArea = new Rectangle(0, 0, State.Width, State.Height);

            networkMessages = new List<byte[]>();
            reliableInOrderMsgs = new List<byte>();
            unreliableInOrderMsgs = new List<byte>();
            reliableUnOrderMsgs = new List<byte>();
            unreliableUnOrderMsgs = new List<byte>();

            processing = false;

            if (State.IsMultiCore)
            {
                updateThread = new Thread(UpdateTracker);
                updateThread.Start();
            }
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the root node of this scene graph
        /// </summary>
        /// <exception cref="GoblinException">If assigned to null</exception>
        public BranchNode RootNode
        {
            get { return rootNode; }
            set 
            {
                if (value == null)
                    throw new GoblinException("Root node can not be assigned to null");
                rootNode = value; 

                // Clears all of the saved information related to scene graph
                nodeTable.Clear();
                nodeRenderGroups.Clear();
                renderGroups.Clear();
                transparentGroup.Clear();
                opaqueGroups.Clear();
                renderedEffects.Clear();
                occluderGroup.Clear();
            }
        }

        /// <summary>
        /// Gets or sets the camera node of this scene graph. (There can be multiple camera nodes
        /// in one scene graph, but only one camera node should be active at a time.)
        /// </summary>
        public CameraNode CameraNode
        {
            get { return cameraNode; }
            set { cameraNode = value; }
        }

        /// <summary>
        /// Gets or sets the environment effect simulated in this scene including fog,
        /// rain, and sunflare.
        /// </summary>
        public GoblinXNA.Graphics.Environment Environment
        {
            get { return environment; }
            set { environment = value; }
        }

        /// <summary>
        /// Gets or sets the background texture. If ShowCameraImage is set, this texture
        /// will be ignored. Also, if this texture is set, the BackgroundColor will be
        /// ignored.
        /// </summary>
        /// <see cref="ShowCameraImage"/>
        /// <see cref="BackgroundColor"/>
        public Texture2D BackgroundTexture
        {
            get { return backgroundTexture; }
            set { backgroundTexture = value; }
        }

        /// <summary>
        /// Gets or sets the background color. If either ShowCameraImage or BackgroundTexture
        /// is set, this will be ignored. Default color is Color.CornflowerBlue.
        /// </summary>
        /// <see cref="ShowCameraImage"/>
        /// <see cref="BackgroundTexture"/>
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; }
        }

        /// <summary>
        /// Gets or sets the comparer for sorting the drawing order of transparent geometries.
        /// If not set, then a default transparency comparer which compares the distance between 
        /// center of the bounding volume of the geometry and the currently active camera location.
        /// </summary>
        public IComparer<GeometryNode> TransparencyDrawOrderComparer
        {
            get { return transparencySortOrder; }
            set { transparencySortOrder = value; }
        }

        /// <summary>
        /// Gets or sets the marker tracking system.
        /// </summary>
        /// <remarks>
        /// If you already assigned the CameraNode, then its projection matrix will be modified
        /// to match the projection matrix of the marker tracker.
        /// </remarks>
        public IMarkerTracker MarkerTracker
        {
            get { return tracker; }
            set
            {
                if (value == null)
                    return;

                if (!value.Initialized)
                    throw new GoblinException("You have to initialize the tracker before you assign " +
                        "to Scene.MarkerTracker");

                tracker = value;
                MarkerBase.Instance.Tracker = tracker;

                if (cameraNode == null)
                {
                    MarkerBase.Instance.InitCameraNode();
                    RootNode.AddChild(MarkerBase.Instance.CameraNode);
                    CameraNode = (CameraNode)MarkerBase.Instance.CameraNode;
                }
                else
                {
                    cameraNode.Camera.Projection = tracker.CameraProjection;
                }
            }
        }

        /// <summary>
        /// Gets or sets the specific physics engine implementation used for this scene graph.
        /// </summary>
        public IPhysics PhysicsEngine
        {
            get { return physicsEngine; }
            set 
            {
                if (physicsEngine != null)
                    physicsEngine.Dispose();

                physicsEngine = value;
                physicsEngine.InitializePhysics();
            }
        }

        /// <summary>
        /// Gets or sets whether to render the axis-aligned bounding box generated by the physics
        /// engine of your choice. Note that the IPhysics function GetAxisAlignedBoundingBox
        /// must be implemented correctly in order to render correctly.
        /// </summary>
        public bool RenderAxisAlignedBoundingBox
        {
            get { return renderAabb; }
            set { renderAabb = value; }
        }

        /// <summary>
        /// Gets or sets whether to render the actual mesh used for collision detection by
        /// the physics engine of your choice. Note that the IPhysics function GetCollisionMesh
        /// must be implemented correctly in order to render correctly.
        /// </summary>
        public bool RenderCollisionMesh
        {
            get { return renderCollisionMesh; }
            set { renderCollisionMesh = value; }
        }

        /// <summary>
        /// Gets or sets the specific network server implementation used for this scene graph.
        /// By default, LidgrenServer is used.
        /// </summary>
        public IServer NetworkServer
        {
            get { return networkServer; }
            set
            {
                if (networkServer != null)
                    networkServer.Shutdown();

                networkServer = value;
                networkServer.Initialize();
            }
        }
        /// <summary>
        /// Gets or sets the specific network client implementation used for this scene graph.
        /// By default, LidgrenClient is used.
        /// </summary>
        public IClient NetworkClient
        {
            get { return networkClient; }
            set
            {
                if (networkClient != null)
                    networkClient.Shutdown();

                networkClient = value;
                networkClient.Connect();
            }
        }

        /// <summary>
        /// Gets the UI renderer used in this scene. Use this property to add any UI components to
        /// be rendered in the scene.
        /// </summary>
        public UIRenderer UIRenderer
        {
            get { return uiRenderer; }
        }
        
        /// <summary>
        /// Gets or sets whether shadow mapping should be enabled. The default value is false.
        /// </summary>
        public bool EnableShadowMapping
        {
            get { return enableShadowMapping; }
            set 
            { 
                enableShadowMapping = value;
                if (enableShadowMapping)
                    State.ShadowShader = new ShadowMapShader();
            }
        }

        /// <summary>
        /// Gets or sets whether to enable the lighting in the scene. The default value is true.
        /// </summary>
        public bool EnableLighting
        {
            get { return enableLighting; }
            set { enableLighting = value; }
        }

        /// <summary>
        /// Gets or sets whether to use per pixel lighting in the scene. The default value is false.
        /// </summary>
        /// <remarks>
        /// This value only affects SimpleEffectShader. If you're using a model which do not
        /// use SimpleEffectShader, then this value is ignored.
        /// </remarks>
        public bool PreferPerPixelLighting
        {
            get { return preferPerPixelLighting; }
            set { preferPerPixelLighting = value; }
        }

        /// <summary>
        /// Gets or sets whether to enable culling on each geometry node based on whether the node
        /// is inside of the current camera frustum. The default value is true.
        /// </summary>
        /// <remarks>
        /// If your application is CPU bound, then setting this to false can improve the performance. However,
        /// if your application is GPU bound, then leaving this value to true would have better performance.
        /// </remarks>
        public bool EnableFrustumCulling
        {
            get { return enableFrustumCulling; }
            set { enableFrustumCulling = value; }
        }

        /// <summary>
        /// Gets or sets the shader used to draw the axis-aligned bounding box generated in the physics
        /// engine for debugging purposes. Default shader is SimpleEffectShader.
        /// </summary>
        public IShader AabbShader
        {
            get { return aabbShader; }
            set { aabbShader = value; }
        }

        /// <summary>
        /// Gets or sets the color used to draw the axis-aligned bounding box of each model for 
        /// debugging. Default color is Color.YellowGreen.
        /// </summary>
        public Color AabbColor
        {
            get { return aabbColor; }
            set
            {
                aabbColor = value;
                Material tmpMat = new Material();
                tmpMat.Specular = value.ToVector4();
                tmpMat.Ambient = value.ToVector4();
                tmpMat.Diffuse = value.ToVector4();
                tmpMat.Emissive = value.ToVector4();
                aabbShader.SetParameters(tmpMat);
            }
        }

        /// <summary>
        /// Gets or sets the shader used to draw the actual mesh used for collision detection 
        /// by the physics engine for debugging purposes. Default shader is SimpleEffectShader.
        /// </summary>
        public IShader CollisionMeshShader
        {
            get { return cmeshShader; }
            set { cmeshShader = value; }
        }

        /// <summary>
        /// Gets or sets the color used to draw the actual mesh used for collision detection of 
        /// each model for debugging. Default color is Color.Purple.
        /// </summary>
        public Color CollisionMeshColor
        {
            get { return cmeshColor; }
            set
            {
                cmeshColor = value;
                Material tmpMat = new Material();
                tmpMat.Specular = value.ToVector4();
                tmpMat.Ambient = value.ToVector4();
                tmpMat.Diffuse = value.ToVector4();
                tmpMat.Emissive = value.ToVector4();
                cmeshShader.SetParameters(tmpMat);
            }
        }

        /// <summary>
        /// Gets the current Frames Per Second count
        /// </summary>
        public int FPS
        {
            get { return uiRenderer.FPS; }
        }

        /// <summary>
        /// Gets the current triangle count
        /// </summary>
        public int TriangleCount
        {
            get { return triangleCount; }
        }

        /// <summary>
        /// Gets a list of added video capture devices.
        /// </summary>
        public List<IVideoCapture> VideoCapture
        {
            get { return MarkerBase.Instance.VideoCaptures; }
        }

        /// <summary>
        /// Gets or sets whether to show camera captured physical image in the background.
        /// (By default, this is false. If showing camera image, then needs to be set to true 
        /// before initializing the marker tracker using InitMarkerTracker method)
        /// </summary>
        /// <remarks>
        /// If you want to show a static image used by the tracker, you need to set the 
        /// Scene.OverlayVideoID to -1. Use VideoVisibleArea property to define the visible
        /// video rendered on the background if you prefer to render only a part of the video
        /// image instead of the entire image on the background.
        /// </remarks>
        /// <see cref="OverlayVideoID"/>
        /// <seealso cref="VideoVisibleArea"/>
        public bool ShowCameraImage
        {
            get { return showCameraImage; }
            set 
            {
                //if (value && (MarkerBase.Instance.VideoCaptures.Count == 0))
                //    throw new GoblinException("You need to add at least one video capture device " +
                //        "before you can show the camera image");

                showCameraImage = value;
                if (showCameraImage && !MarkerBase.Instance.RenderInitialized)
                    MarkerBase.Instance.InitRendering();
            }
        }

        /// <summary>
        /// Gets or sets the video capture device ID used to provide the overlaid physical image.
        /// This ID should correspond to the videoDeviceID given to the initialized video device
        /// using InitVideoCapture method. If you want to show a static image used by the tracker,
        /// you should set this to -1.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public int OverlayVideoID
        {
            get { return overlayVideoID; }
            set 
            {
                // Wait for the tracker update to end before modifying the ID
                while (waitForTrackerUpdate) { }
                waitForVideoIDChange = true;
                if (value < 0)
                {
                    MarkerBase.Instance.ActiveCaptureDevice = -1;
                    actualOverlayVideoID = -1;
                    overlayVideoID = -1;
                    overlayAndTrackerUseSameID = (overlayVideoID == trackerVideoID);
                    waitForVideoIDChange = false;
                    return;
                }

                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("OverlayVideoID " + value + " does not exist");

                actualOverlayVideoID = videoIDs[value];
                overlayVideoID = value;
                MarkerBase.Instance.ActiveCaptureDevice = actualOverlayVideoID;

                overlayAndTrackerUseSameID = (overlayVideoID == trackerVideoID);
                waitForVideoIDChange = false;
            }
        }

        /// <summary>
        /// Gets or sets the video capture device ID used to perform marker tracking (if available).
        /// This ID should correspond to the videoDeviceID given to the initialized video device
        /// using InitVideoCapture method. If you want to process a static image used by the tracker,
        /// you should set this to -1.
        /// </summary>
        public int TrackerVideoID
        {
            get { return trackerVideoID; }
            set
            {
                // Wait for the tracker update to end before modifying the ID
                while (waitForTrackerUpdate) { }
                waitForVideoIDChange = true;
                if (value < 0)
                {
                    actualTrackerVideoID = -1;
                    trackerVideoID = -1;
                    overlayAndTrackerUseSameID = (overlayVideoID == trackerVideoID);
                    waitForVideoIDChange = false;
                    return;
                }

                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("TrackerVideoID " + value + " does not exist");

                actualTrackerVideoID = videoIDs[value];
                trackerVideoID = value;

                overlayAndTrackerUseSameID = (overlayVideoID == trackerVideoID);
                waitForVideoIDChange = false;
            }
        }

        /// <summary>
        /// Gets or sets the video ID for left eye to use for stereo augmented reality. If you use 
        /// single camera for stereo, then you should set both LeftEyeVideoID and RightEyeVideoID to 
        /// the same video ID. 
        /// </summary>
        /// <see cref="RightEyeVideoID"/>
        public int LeftEyeVideoID
        {
            get { return leftEyeVideoID; }
            set 
            {
                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("VideoID " + value + " does not exist");

                actualLeftEyeVideoID = videoIDs[value];
                leftEyeVideoID = value;

                singleVideoStereo = (leftEyeVideoID == rightEyeVideoID);
            }
        }

        /// <summary>
        /// Gets or sets the video ID for right eye to use for stereo augmented reality. If you use 
        /// single camera for stereo, then you should set both LeftEyeVideoID and RightEyeVideoID to 
        /// the same video ID.
        /// </summary>
        /// <see cref="LeftEyeVideoID"/>
        public int RightEyeVideoID
        {
            get { return rightEyeVideoID; }
            set
            {
                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("VideoID " + value + " does not exist");

                actualRightEyeVideoID = videoIDs[value];
                rightEyeVideoID = value;

                singleVideoStereo = (leftEyeVideoID == rightEyeVideoID);
            }
        }

        /// <summary>
        /// Gets or sets the shift amount of the overlaid video image in pixels for left eye.
        /// </summary>
        /// <remarks>
        /// Sets this property only if you want to do stereo video overlay using single camera.
        /// </remarks>
        public int LeftEyeVideoImageShift
        {
            get { return leftEyeVideoImageShift; }
            set { leftEyeVideoImageShift = value; }
        }

        /// <summary>
        /// Gets or sets the shift amount of the overlaid video image in pixels for right eye.
        /// </summary>
        /// <remarks>
        /// Sets this property only if you want to do stereo video overlay using single camera.
        /// </remarks>
        public int RightEyeVideoImageShift
        {
            get { return rightEyeVideoImageShift; }
            set { rightEyeVideoImageShift = value; }
        }

        /// <summary>
        /// Gets or sets the visible area (in rectangle) of the video overlay image rendered on the background. 
        /// The default is (0, 0, State.Width, State.Height) which fills the entire background with
        /// the video image when ShowCameraImage is set to true. 
        /// </summary>
        /// <remarks>
        /// If you modify the back buffer width and height after initializing Scene, make sure to 
        /// modify this information as well so that the overlaid image fills the entire background.
        /// This property can be used to render only a part of the video image instead of full image.
        /// </remarks>
        public Rectangle VideoVisibleArea
        {
            get { return videoVisibleArea; }
            set { videoVisibleArea = value; }
        }

        /// <summary>
        /// Gets or sets whether to freeze currently active video streaming. 
        /// </summary>
        /// <remarks>
        /// This will also affect the vision tracking if tracking is used.
        /// </remarks>
        public bool FreezeVideo
        {
            get { return freezeVideo; }
            set { freezeVideo = value; }
        }

        /// <summary>
        /// Indicates whether the scene graph is currently being traversed.
        /// </summary>
        internal bool Processing
        {
            get { return processing; }
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Updates the particle effects added in the scene graph.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void UpdateParticleEffects(GameTime gameTime)
        {
            foreach (ParticleNode node in renderedEffects)
            {
                foreach (ParticleEffect particalEffect in node.ParticleEffects)
                    particalEffect.Update(gameTime);
            }
        }

        /// <summary>
        /// Prepares the scene for rendering by traversing the entire scene graph using pre-order traversal.
        /// </summary>
        protected virtual void PrepareSceneForRendering()
        {
            localLights.Clear();
            globalLights.Clear();

            trackMarkers = false;
            processing = true;
            Matrix rootWorldTransform = Matrix.Identity;
            Matrix rootMarkerTransform = Matrix.Identity;
            RecursivePrepareForRendering(rootNode, ref rootWorldTransform, ref rootMarkerTransform, false);
            processing = false;
        }

        /// <summary>
        /// Tests whether a bounding sphere is within the viewing frustum of the current active camera
        /// </summary>
        /// <param name="boundingVolume"></param>
        /// <returns></returns>
        protected virtual bool IsWithinViewFrustum(BoundingSphere boundingVolume)
        {
            if (!enableFrustumCulling)
                return true;

            if (cameraNode.Stereo)
                return cameraNode.RightBoundingFrustum.Intersects(boundingVolume) ||
                    cameraNode.LeftBoundingFrustum.Intersects(boundingVolume);
            else
                return cameraNode.BoundingFrustum.Intersects(boundingVolume);
        }

        /// <summary>
        /// Recursively traverses the scene graph using pre-order traversal.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="parentWorldTransformation"></param>
        /// <param name="markerTransform"></param>
        /// <param name="calculateAll"></param>
        /// <param name="elapsedTime"></param>
        protected virtual void RecursivePrepareForRendering(Node node,
            ref Matrix parentWorldTransformation, ref Matrix markerTransform, bool calculateAll)
        {
            Matrix parentTransformation = parentWorldTransformation;
            Matrix parentMarkerTransform = markerTransform;
            bool isWorldTransformationDirty = false;
            int switchPass = -1;
            bool pruneForMarkerNode = false; 

            if (!nodeTable.ContainsKey(node.Name))
                nodeTable.Add(node.Name, node);

            if (!node.Enabled)
                return;

            // TODO: Prune this node if bounding sphere of this node does not intersect
            // with the bounding frustum

            if (node is TransformNode)
            {
                TransformNode tNode = (TransformNode) node;
                isWorldTransformationDirty = tNode.IsWorldTransformationDirty;

                Matrix nodeWorldTransformation = Matrix.Identity;
                if (tNode.UseUserDefinedTransform)
                {
                    nodeWorldTransformation = tNode.WorldTransformation;
                    isWorldTransformationDirty = tNode.UserDefinedTransformChanged;
                    tNode.UserDefinedTransformChanged = false;
                }
                else
                {
                    if (tNode.IsWorldTransformationDirty)
                    {
                        tmpQuat1 = tNode.Rotation;
                        tmpVec1 = tNode.Scale;

                        Matrix.CreateFromQuaternion(ref tmpQuat1, out tmpMat1);
                        Matrix.CreateScale(ref tmpVec1, out tmpMat2);
                        Matrix.Multiply(ref tmpMat1, ref tmpMat2, out nodeWorldTransformation);

                        nodeWorldTransformation.Translation = tNode.Translation;
                        tNode.ComposedTransform = nodeWorldTransformation;
                        tNode.IsWorldTransformationDirty = false;
                    }
                    else
                        nodeWorldTransformation = tNode.ComposedTransform;
                }

                if (!parentWorldTransformation.Equals(Matrix.Identity))
                {
                    Matrix.Multiply(ref nodeWorldTransformation, ref parentWorldTransformation, 
                        out nodeWorldTransformation);
                }
                parentTransformation = nodeWorldTransformation;
            }
            else if (node is CameraNode)
            {
                CameraNode cNode = (CameraNode) node;
                Matrix.Multiply(ref parentTransformation, ref markerTransform, out tmpMat1);
                Matrix.Invert(ref tmpMat1, out tmpMat1);
                if (cNode.Stereo)
                {
                    tmpMat2 = ((StereoCamera)cNode.Camera).LeftView;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                    cNode.LeftCompoundViewMatrix = tmpMat3;

                    tmpMat2 = ((StereoCamera)cNode.Camera).RightView;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                    cNode.RightCompoundViewMatrix = tmpMat3;
                }
                else
                {
                    tmpMat2 = cNode.Camera.View;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                    cNode.CompoundViewMatrix = tmpMat3;
                }

                MatrixHelper.GetRotationMatrix(ref parentTransformation, out tmpMat1);
                Matrix.Multiply(ref tmpMat1, ref markerTransform, out tmpMat2);
                tmpMat3 = cNode.Camera.CameraTransformation;
                Matrix.Multiply(ref tmpMat2, ref tmpMat3, out tmpMat1);

                tmpVec1 = cNode.Camera.CameraTransformation.Translation;
                tmpVec2 = parentTransformation.Translation;

                Vector3.Add(ref tmpVec1, ref tmpVec2, out tmpVec3);

                tmpMat1.Translation = tmpVec3;
                cNode.WorldTransformation = tmpMat1;
            }
            else if (node is GeometryNode)
            {
                GeometryNode gNode = (GeometryNode) node;

                if (State.EnableNetworking && State.IsServer)
                    prevMatrix = gNode.WorldTransformation;

                if (!(State.EnableNetworking && !State.IsServer))
                {
                    if (physicsEngine != null && gNode.AddToPhysicsEngine)
                    {
                        if (gNode.Physics.Modified || calculateAll)
                        {
                            // Calculate the initial tranformation to pass to the physics engine
                            tmpMat1 = gNode.Physics.InitialWorldTransform;
                            Matrix.Multiply(ref parentTransformation, ref tmpMat1, out tmpMat2);
                            gNode.Physics.CompoundInitialWorldTransform = tmpMat2;
                            gNode.WorldTransformation = tmpMat2;
                            physicsEngine.ModifyPhysicsObject(gNode.Physics, tmpMat2);
                            gNode.Physics.Modified = false;
                        }
                        else
                        {
                            gNode.WorldTransformation = gNode.Physics.PhysicsWorldTransform;
                        }
                    }
                    else
                        gNode.WorldTransformation = parentTransformation;

                    if (gNode.Model.OffsetToOrigin)
                    {
                        Vector3 offset = gNode.Model.OffsetTransform.Translation;

                        Matrix.CreateTranslation(ref offset, out tmpMat1);
                        tmpMat2 = gNode.WorldTransformation;
                        MatrixHelper.GetRotationMatrix(ref tmpMat2, out tmpMat3);
                        Matrix.Multiply(ref tmpMat1, ref tmpMat3, out tmpMat2);

                        tmpVec1 = tmpMat2.Translation;
                        Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                        tmpMat2 = gNode.WorldTransformation;
                        Matrix.Multiply(ref tmpMat2, ref tmpMat1, out tmpMat3);

                        gNode.WorldTransformation = tmpMat3;
                    }
                }

                if(gNode.PhysicsStateChanged && physicsEngine != null)
                {
                    if(gNode.AddToPhysicsEngine)
                        physicsEngine.AddPhysicsObject(gNode.Physics);
                    else
                        physicsEngine.RemovePhysicsObject(gNode.Physics);

                    gNode.PhysicsStateChanged = false;
                }

                gNode.MarkerTransform = markerTransform;

                parentTransformation = gNode.WorldTransformation;

                if (State.EnableNetworking && State.IsServer)
                {
                    if (MatrixHelper.HasMovedSignificantly(prevMatrix, gNode.WorldTransformation))
                        gNode.Network.ReadyToSend = true;
                }

                if (gNode.IsOccluder)
                {
                    AddToOccluderGroup(gNode);
                }
                else
                {
                    if (gNode.Material.Diffuse.W < 1.0f)
                        AddToTransparencyGroup(gNode);
                    else
                        AddToRenderedGroups(gNode);
                }

                if (gNode is LODNode)
                {
                    LODNode lodNode = (LODNode)gNode;
                    if (!lodNodes.Contains(lodNode))
                        lodNodes.Add(lodNode);
                }

                BoundingSphere boundingVol = gNode.BoundingVolume;
                tmpMat1 = gNode.WorldTransformation;

                tmpVec1.X = (float)Math.Sqrt(tmpMat1.M11 * tmpMat1.M11 + tmpMat1.M12 * tmpMat1.M12 + tmpMat1.M13 * tmpMat1.M13);
                tmpVec1.Y = (float)Math.Sqrt(tmpMat1.M21 * tmpMat1.M21 + tmpMat1.M22 * tmpMat1.M22 + tmpMat1.M23 * tmpMat1.M23);
                tmpVec1.Z = (float)Math.Sqrt(tmpMat1.M31 * tmpMat1.M31 + tmpMat1.M32 * tmpMat1.M32 + tmpMat1.M33 * tmpMat1.M33);

                boundingVol.Radius = gNode.Model.MinimumBoundingSphere.Radius * Math.Max(tmpVec1.X,
                    Math.Max(tmpVec1.Y, tmpVec1.Z));

                if (!gNode.Model.OffsetToOrigin)
                {
                    tmpVec1 = gNode.Model.MinimumBoundingBox.Min;
                    tmpVec2 = gNode.Model.MinimumBoundingBox.Max;
                    Vector3.Add(ref tmpVec1, ref tmpVec2, out tmpVec3);
                    Vector3.Divide(ref tmpVec3, 2, out tmpVec1);

                    Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                    tmpMat2 = gNode.WorldTransformation;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                }

                if (markerTransform.M44 == 0)
                {
                    boundingVol.Center.X = float.MaxValue;
                    boundingVol.Center.Y = float.MaxValue;
                    boundingVol.Center.Z = float.MaxValue;
                }
                else
                {
                    Matrix.Multiply(ref tmpMat3, ref markerTransform, out tmpMat1);
                    boundingVol.Center = tmpMat1.Translation;
                }

                gNode.BoundingVolume = boundingVol;

                // Set the local lights that will affect this geometry model
                gNode.LocalLights.Clear();
                gNode.LocalLights.AddRange(localLights);

                gNode.ShouldRender = true;
            }
            else if (node is MarkerNode)
            {
                trackMarkers = true;
                MarkerNode markerNode = (MarkerNode)node;
                tmpMat1 = markerNode.WorldTransformation;
                Matrix.Multiply(ref tmpMat1, ref markerTransform, out parentMarkerTransform);

                if(markerNode.Optimize && !markerNode.MarkerFound)
                    pruneForMarkerNode = true;
            }
            else if (node is TrackerNode)
            {
                TrackerNode tNode = (TrackerNode)node;
                tmpMat1 = tNode.WorldTransformation;
                Matrix.Multiply(ref tmpMat1, ref markerTransform, out parentMarkerTransform);
            }
            else if (node is ParticleNode)
            {
                ParticleNode pNode = (ParticleNode)node;
                if (pNode.Parent is GeometryNode)
                    pNode.WorldTransformation = ((GeometryNode)pNode.Parent).WorldTransformation;
                else
                    pNode.WorldTransformation = parentTransformation;

                pNode.WorldTransformation *= markerTransform;
                if (!pNode.IsRendered)
                {
                    pNode.IsRendered = true;
                    renderedEffects.Add(pNode);
                }

                pNode.ShouldRender = true;
            }
            else if (node is SwitchNode)
            {
                SwitchNode sNode = (SwitchNode)node;
                switchPass = sNode.SwitchID;
                if (sNode.SwitchChanged)
                {
                    foreach (Node child in sNode.Children)
                        RecursivelyRemoveFromRendering(child);
                    sNode.SwitchChanged = false;
                }
            }
            else if (node is SoundNode)
            {
                SoundNode soNode = (SoundNode)node;
                soNode.WorldTransformation = parentTransformation;
            }

            if (node is BranchNode)
            {
                BranchNode bNode = (BranchNode)node;
                if (switchPass >= 0)
                    RecursivePrepareForRendering(bNode.Children[switchPass], ref parentTransformation,
                        ref parentMarkerTransform, isWorldTransformationDirty || calculateAll);
                else
                {
                    if (!(bNode.Prune || pruneForMarkerNode))
                    {
                        int numLocalLights = 0;
                        // First, only look for the LightNode since if it's local, we need to propagate it
                        // to the siblings as well as the descendants
                        foreach (Node child in bNode.Children)
                        {
                            if (child.Enabled && (child is LightNode))
                            {
                                LightNode lNode = (LightNode)child;
                                Matrix.Multiply(ref parentTransformation, ref parentMarkerTransform, out tmpMat1);
                                lNode.WorldTransformation = tmpMat1;
                                if (lNode.Global)
                                    globalLights.Add(lNode);
                                else
                                {
                                    localLights.Push(lNode);
                                    numLocalLights++;
                                }
                            }
                        }

                        // Now, go through all of the child nodes except the light nodes
                        foreach (Node child in bNode.Children)
                        {
                            if(!(child is LightNode))
                                RecursivePrepareForRendering(child, ref parentTransformation,
                                    ref parentMarkerTransform, isWorldTransformationDirty || calculateAll);
                        }

                        // Pops off the local lights from the stack
                        for (int i = 0; i < numLocalLights; i++)
                            localLights.Pop();
                    }
                }
            }
        }

        /// <summary>
        /// Recursively removes a node and all of its children from the scene graph.
        /// </summary>
        /// <param name="node"></param>
        internal virtual void RecursivelyRemoveFromRendering(Node node)
        {
            nodeTable.Remove(node.Name);
            if (node is GeometryNode)
                RemoveFromRenderedGroups((GeometryNode)node);
            else if (node is ParticleNode)
            {
                renderedEffects.Remove((ParticleNode)node);
                ((ParticleNode)node).IsRendered = false;
            }

            if(node is BranchNode)
                foreach (Node child in ((BranchNode)node).Children)
                    RecursivelyRemoveFromRendering(child);
        }

        /// <summary>
        /// Adds a geometry node to the render group with opaque material.
        /// </summary>
        /// <param name="node"></param>
        protected virtual void AddToRenderedGroups(GeometryNode node)
        {
            if (!node.IsRendered)
            {
                if (!nodeRenderGroups.ContainsKey(node.GroupID))
                {
                    List<GeometryNode> renderNodes = new List<GeometryNode>();
                    renderNodes.Add(node);
                    nodeRenderGroups.Add(node.GroupID, renderNodes);
                    if (!renderGroups.ContainsKey(node.GroupID))
                        renderGroups.Add(node.GroupID, true);
                }
                else
                    nodeRenderGroups[node.GroupID].Add(node);

                opaqueGroups.Add(node);

                networkObjects.Add(node.Network.Identifier, new NetObj(node.Network));

                node.IsRendered = true;
            }
            else
            {
                if (transparentGroup.Contains(node))
                {
                    transparentGroup.Remove(node);
                    if (!nodeRenderGroups.ContainsKey(node.GroupID))
                    {
                        List<GeometryNode> renderNodes = new List<GeometryNode>();
                        renderNodes.Add(node);
                        nodeRenderGroups.Add(node.GroupID, renderNodes);
                    }
                    else
                        nodeRenderGroups[node.GroupID].Add(node);

                    opaqueGroups.Add(node);
                }
            }
        }

        /// <summary>
        /// Adds a geometry node to the transparency group. The transparency group contains 
        /// geometry nodes that have transparent material color (Materia.Diffuse.W < 1.0f).
        /// </summary>
        /// <param name="node"></param>
        protected virtual void AddToTransparencyGroup(GeometryNode node)
        {
            if (!node.IsRendered)
            {
                if (!renderGroups.ContainsKey(node.GroupID))
                    renderGroups.Add(node.GroupID, true);

                transparentGroup.Add(node);

                networkObjects.Add(node.Network.Identifier, new NetObj(node.Network));

                node.IsRendered = true;
                needTransparencySort = true;
            }
            else
            {
                if (opaqueGroups.Contains(node))
                {
                    opaqueGroups.Remove(node);
                    nodeRenderGroups[node.GroupID].Remove(node);
                    transparentGroup.Add(node);
                    needTransparencySort = true;
                }
            }
        }

        /// <summary>
        /// Add a geometry node to the occluder group. The occluder group contains geometry nodes
        /// that are defined as a occluder object (GeometryNode.IsOccluder == true).
        /// </summary>
        /// <param name="node"></param>
        /// <remarks>In VRScene, occlusion doesn't make sense, so added to render group</remarks>
        protected virtual void AddToOccluderGroup(GeometryNode node)
        {
            if (!node.IsRendered)
            {
                if (!renderGroups.ContainsKey(node.GroupID))
                    renderGroups.Add(node.GroupID, true);

                occluderGroup.Add(node);

                networkObjects.Add(node.Network.Identifier, new NetObj(node.Network));

                node.IsRendered = true;
            }
        }

        /// <summary>
        /// Removes a geometry node from the scene graph
        /// </summary>
        /// <param name="node">A geometry node to be removed</param>
        protected virtual void RemoveFromRenderedGroups(GeometryNode node)
        {
            if (node.IsRendered)
            {
                if (nodeRenderGroups.ContainsKey(node.GroupID) &&
                    nodeRenderGroups[node.GroupID].Contains(node))
                {
                    nodeRenderGroups[node.GroupID].Remove(node);
                    opaqueGroups.Remove(node);
                    networkObjects.Remove(node.Network.Identifier);
                    if(physicsEngine != null && node.AddToPhysicsEngine)
                        physicsEngine.RemovePhysicsObject(node.Physics);

                    if (nodeRenderGroups[node.GroupID].Count == 0)
                        nodeRenderGroups.Remove(node.GroupID);
                }
                else if (transparentGroup.Contains(node))
                {
                    transparentGroup.Remove(node);
                    networkObjects.Remove(node.Network.Identifier);
                    if(physicsEngine != null && node.AddToPhysicsEngine)
                        physicsEngine.RemovePhysicsObject(node.Physics);
                }
                else if (occluderGroup.Contains(node))
                {
                    occluderGroup.Remove(node);
                    if(physicsEngine != null && node.AddToPhysicsEngine)
                        physicsEngine.RemovePhysicsObject(node.Physics);
                    networkObjects.Remove(node.Network.Identifier);
                }

                node.IsRendered = false;
            }
        }

        /// <summary>
        /// Renders the shadows.
        /// </summary>
        protected virtual void RenderShadows(List<LightSource> globalLightSources)
        {
            // Generate shadows for all of the opaque geometries

            State.ShadowShader.SetParameters(globalLights, emptyLightList);
            State.ShadowShader.GenerateShadows(
                delegate
                {
                    foreach (int renderGroup in nodeRenderGroups.Keys)
                        if (renderGroups[renderGroup])
                            foreach (GeometryNode node in nodeRenderGroups[renderGroup])
                                if (node.Model != null && node.Model.Enabled && node.ShouldRender)
                                    node.Model.GenerateShadows(node.WorldTransformation *
                                        node.MarkerTransform);

                    foreach (GeometryNode transNode in transparentGroup)
                        if (renderGroups[transNode.GroupID])
                            if (transNode.Model != null && transNode.Model.Enabled && transNode.ShouldRender)
                                transNode.Model.GenerateShadows(transNode.WorldTransformation *
                                    transNode.MarkerTransform);

                    foreach (GeometryNode occluderNode in occluderGroup)
                        if (renderGroups[occluderNode.GroupID] && occluderNode.Model != null
                            && occluderNode.Model.Enabled && occluderNode.ShouldRender)
                            occluderNode.Model.GenerateShadows(occluderNode.WorldTransformation *
                                occluderNode.MarkerTransform);
                });

            // Render shadows for all of the opaque geometries
            State.ShadowShader.RenderShadows(
                delegate
                {
                    foreach (int renderGroup in nodeRenderGroups.Keys)
                        if (renderGroups[renderGroup])
                            foreach (GeometryNode node in nodeRenderGroups[renderGroup])
                                if (node.Model != null && node.Model.Enabled && 
                                    node.Material.Diffuse.W == 1.0f && node.ShouldRender &&
                                    IsWithinViewFrustum(node.BoundingVolume))
                                    node.Model.UseShadows(node.WorldTransformation * node.MarkerTransform);

                    foreach (GeometryNode occluderNode in occluderGroup)
                        if (renderGroups[occluderNode.GroupID] && occluderNode.Model != null
                            && occluderNode.Model.Enabled && occluderNode.ShouldRender/* &&
                            cameraNode.BoundingFrustum.Intersects(occluderNode.BoundingVolume)*/)
                            occluderNode.Model.UseShadows(occluderNode.WorldTransformation *
                                occluderNode.MarkerTransform);
                });
        }

        /// <summary>
        /// Renders the scene.
        /// </summary>
        protected virtual void RenderSceneGraph(List<LightSource> globalLightSources)
        {
            State.Device.Clear(backgroundColor);
            triangleCount = 0;

            State.Device.RenderState.DepthBufferEnable = true;
            // Render the occlusion objects first
            foreach (GeometryNode occluderNode in occluderGroup)
            {
                if (renderGroups[occluderNode.GroupID] && occluderNode.Model != null
                    && occluderNode.Model.Enabled && occluderNode.ShouldRender &&
                    IsWithinViewFrustum(occluderNode.BoundingVolume))
                {
                    triangleCount += occluderNode.Model.TriangleCount;

                    tmpMat1 = occluderNode.WorldTransformation;
                    tmpMat2 = occluderNode.MarkerTransform;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                    occluderNode.Model.Render(tmpMat3, emptyMaterial);
                    // If it's stereo rendering, then we want to set this back to false only after
                    // drawing the left eye view (renderLeftView is NOTted because it's rendering
                    // left eye view, renderLeftView is already set to false at this point
                    if(!(cameraNode.Stereo && !renderLeftView))
                        occluderNode.ShouldRender = false;
                }
            }

            // Now turn off the depth buffer to overwrite the scene with background image
            if (occluderGroup.Count > 0 || showCameraImage || (backgroundTexture != null))
                State.Device.RenderState.DepthBufferEnable = false;

            if (showCameraImage)
            {
                spriteBatch.Begin();

                int x = videoVisibleArea.X, y = videoVisibleArea.Y, width = videoVisibleArea.Width,
                    height = videoVisibleArea.Height;
                Texture2D image = MarkerBase.Instance.BackgroundTexture;
                // If not doing stereo video overlay
                if (leftEyeVideoID >= 0 && rightEyeVideoID >= 0)
                {
                    // In RenderScene, renderLeftView is already flipped, so we'll take care of right eye
                    // video if renderLeftView is true, and left eye video if false
                    if (renderLeftView)
                    {
                        if (!singleVideoStereo)
                        {
                            // If right eye video is not the default overlaid video, then
                            // we use the additional image
                            if (rightEyeVideoID != overlayVideoID)
                            {
                                image = MarkerBase.Instance.AdditionalBackgroundTexture;
                            }
                        }

                        x += rightEyeVideoImageShift;
                    }
                    else
                    {
                        if (!singleVideoStereo)
                        {
                            // If left eye video is not the default overlaid video, then
                            // we use the additional image
                            if (leftEyeVideoID != overlayVideoID)
                            {
                                image = MarkerBase.Instance.AdditionalBackgroundTexture;
                            }
                        }

                        x += leftEyeVideoImageShift;
                    }
                }

                spriteBatch.Draw(image, new Rectangle(x, y, width, height), Color.White);

                spriteBatch.End();
            }
            else if (backgroundTexture != null)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(BackgroundTexture,
                    new Rectangle(0, 0, State.Width, State.Height), Color.White);
                spriteBatch.End();
            }

            // Now turn on the depth buffer back for normal rendering
            if (occluderGroup.Count > 0 || showCameraImage || (backgroundTexture != null))
                State.Device.RenderState.DepthBufferEnable = true;

            // First render all of the opaque geometries
            foreach (int renderGroup in nodeRenderGroups.Keys)
            {
                if (renderGroups[renderGroup])
                {
                    foreach (GeometryNode node in nodeRenderGroups[renderGroup])
                    {
                        if (node.Model != null && node.Model.Enabled && node.ShouldRender && 
                            IsWithinViewFrustum(node.BoundingVolume))
                        { 
                            triangleCount += node.Model.TriangleCount;

                            if (node.Model.UseLighting)
                            {
                                node.Model.Shader.SetParameters(globalLights, node.LocalLights);
                                if (node.Model.Shader is SimpleEffectShader)
                                    ((SimpleEffectShader)node.Model.Shader).PreferPerPixelLighting =
                                        preferPerPixelLighting;
                            }
                            else
                            {
                                node.Model.Shader.SetParameters(emptyLightList, emptyLightList);
                            }

                            if(environment != null)
                                node.Model.Shader.SetParameters(environment);
                            tmpMat1 = node.WorldTransformation;
                            if (!node.MarkerTransform.Equals(Matrix.Identity))
                            {
                                tmpMat2 = node.MarkerTransform;
                                Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                                tmpMat1 = tmpMat3;
                            }

                            node.Model.Render(tmpMat1, node.Material);

                            // If it's stereo rendering, then we want to set this back to false only after
                            // drawing the left eye view (renderLeftView is NOTted because it's rendering
                            // left eye view, renderLeftView is already set to false at this point
                            if (!(cameraNode.Stereo && !renderLeftView))
                                node.ShouldRender = false;

                            if (renderAabb && node.AddToPhysicsEngine && (physicsEngine != null))
                                RenderAabb(node.MarkerTransform, physicsEngine.GetAxisAlignedBoundingBox(node.Physics));

                            if (renderCollisionMesh && node.AddToPhysicsEngine && (physicsEngine != null))
                                RenderColMesh(node.MarkerTransform, physicsEngine.GetCollisionMesh(node.Physics));
                        }
                    }
                }
            }

            // Before rendering tranparent objects, we need to sort them back to front
            if (needTransparencySort)
            {
                if(transparencySortOrder != null)
                    transparentGroup.Sort(transparencySortOrder);
                needTransparencySort = false;
            }

            // Then render all of the geometries with transparent material
            GeometryNode transparentNode = null;
            State.Device.RenderState.DepthBufferWriteEnable = false;
            for (int i = 0; i < transparentGroup.Count; i++)
            {
                transparentNode = transparentGroup[i];
                if (renderGroups[transparentNode.GroupID])
                {
                    if (transparentNode.Model != null && transparentNode.Model.Enabled &&
                        transparentNode.ShouldRender &&
                        IsWithinViewFrustum(transparentNode.BoundingVolume))
                    {
                        triangleCount += transparentNode.Model.TriangleCount;
                        if (transparentNode.Model.UseLighting)
                        {
                            transparentNode.Model.Shader.SetParameters(globalLights, transparentNode.LocalLights);
                            if (transparentNode.Model.Shader is SimpleEffectShader)
                                ((SimpleEffectShader)transparentNode.Model.Shader).PreferPerPixelLighting =
                                    preferPerPixelLighting;
                        }
                        else
                            transparentNode.Model.Shader.SetParameters(emptyLightList, emptyLightList);

                        tmpMat1 = transparentNode.WorldTransformation;
                        if (!transparentNode.MarkerTransform.Equals(Matrix.Identity))
                        {
                            tmpMat2 = transparentNode.MarkerTransform;
                            Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                            tmpMat1 = tmpMat3;
                        }
                        transparentNode.Model.Render(tmpMat1, transparentNode.Material);

                        // If it's stereo rendering, then we want to set this back to false only after
                        // drawing the left eye view (renderLeftView is NOTted because it's rendering
                        // left eye view, renderLeftView is already set to false at this point
                        if (!(cameraNode.Stereo && !renderLeftView))
                            transparentNode.ShouldRender = false;

                        if (renderAabb && transparentNode.AddToPhysicsEngine && (physicsEngine != null))
                                RenderAabb(transparentNode.MarkerTransform,
                                    physicsEngine.GetAxisAlignedBoundingBox(transparentNode.Physics));

                            if (renderCollisionMesh && transparentNode.AddToPhysicsEngine && 
                                (physicsEngine != null))
                            RenderColMesh(transparentNode.MarkerTransform,
                                physicsEngine.GetCollisionMesh(transparentNode.Physics));
                    }
                }
            }

            // Finally render all of the particle effects
            foreach (ParticleNode particle in renderedEffects)
            {
                if (particle.ShouldRender)
                {
                    particle.Render();

                    // If it's stereo rendering, then we want to set this back to false only after
                    // drawing the left eye view (renderLeftView is NOTted because it's rendering
                    // left eye view, renderLeftView is already set to false at this point
                    if (!(cameraNode.Stereo && !renderLeftView))
                        particle.ShouldRender = false;
                }
            }

            State.Device.RenderState.DepthBufferWriteEnable = true;

            // Show shadows we calculated above
            try
            {
                if(enableShadowMapping)
                    State.ShadowShader.ShowShadows();
            }
            catch (Exception exp) { }

            uiRenderer.TriangleCount = triangleCount;
        }

        /// <summary>
        /// Updates the optical marker tracker as well as the video image
        /// </summary>
        protected virtual void UpdateTracker()
        {
            if (State.IsMultiCore)
            {
                while (true)
                {
                    if (readyToUpdateTracker)
                    {
                        UpdateTrackerAndImage();
                    }
                }
            }
            else
                UpdateTrackerAndImage();
        }

        /// <summary>
        /// Updates the optical marker tracker as well as the video image
        /// </summary>
        protected void UpdateTrackerAndImage()
        {
            // Wait for video ID to change before updating the tracker and image
            while (waitForVideoIDChange) { }
            waitForTrackerUpdate = true;
            if (showCameraImage)
            {
                if (tracker == null)
                {
                    if(!freezeVideo)
                        tmpImage = MarkerBase.Instance.VideoCaptures[actualOverlayVideoID].
                            GetImageTexture(true, false);
                }
                else
                {
                    if (overlayAndTrackerUseSameID)
                    {
                        if (actualOverlayVideoID < 0)
                        {
                            tmpImage = tracker.StaticImage;
                            tracker.ProcessImage();
                        }
                        else
                        {
                            if (!freezeVideo)
                                tmpImage = MarkerBase.Instance.VideoCaptures[actualOverlayVideoID].
                                    GetImageTexture(true, true);
                            tracker.ProcessImage(MarkerBase.Instance.VideoCaptures[actualOverlayVideoID]);
                        }
                    }
                    else
                    {
                        if (actualOverlayVideoID < 0)
                            tmpImage = tracker.StaticImage;
                        else if (!freezeVideo)
                            tmpImage = MarkerBase.Instance.VideoCaptures[actualOverlayVideoID].
                                GetImageTexture(true, false);

                        if (actualTrackerVideoID < 0)
                            tracker.ProcessImage();
                        else
                        {
                            if(!freezeVideo)
                                MarkerBase.Instance.VideoCaptures[actualTrackerVideoID].GetImageTexture(false, true);
                            tracker.ProcessImage(MarkerBase.Instance.VideoCaptures[actualTrackerVideoID]);
                        }
                    }
                }

                // Update 2nd camera's video image if using two cameras for stereoscopic view
                if (cameraNode.Stereo && !singleVideoStereo)
                {
                    MarkerBase.Instance.UpdateAdditionalImage(MarkerBase.Instance.VideoCaptures[
                        (leftEyeVideoID == overlayVideoID) ? actualRightEyeVideoID : actualLeftEyeVideoID].
                        GetImageTexture(true, false));
                }
            }
            else if (trackMarkers && (tracker != null))
            {
                if (actualTrackerVideoID < 0)
                    tracker.ProcessImage();
                else
                {
                    if (!freezeVideo)
                        MarkerBase.Instance.VideoCaptures[actualTrackerVideoID].GetImageTexture(false, true);
                    tracker.ProcessImage(MarkerBase.Instance.VideoCaptures[actualTrackerVideoID]);
                }
            }

            // Assign the video image right before updating the markers so that the marker 
            // transformation is synchronized with the video image
            videoImage = tmpImage;
            float elapsedTime = 0;
            float curTime = (float)DateTime.Now.TimeOfDay.TotalMilliseconds;
            if (prevTrackerTime != 0)
                elapsedTime = curTime - prevTrackerTime;
            prevTrackerTime = curTime;
            try
            {
                foreach (MarkerNode markerNode in markerUpdateList)
                    markerNode.Update(elapsedTime);
            }
            catch (Exception) { }

            waitForTrackerUpdate = false;
        }

        protected void AddNetMessage(List<byte> msgs, List<byte> riMsgs, List<byte> ruMsgs,
            List<byte> uriMsgs, List<byte> uruMsgs, INetworkObject networkObj)
        {
            byte[] id = ByteHelper.ConvertToByte(networkObj.Identifier + ":");
            byte[] data = networkObj.GetMessage();
            short size = (short)(id.Length + data.Length);

            msgs.AddRange(BitConverter.GetBytes(size));
            msgs.AddRange(id);
            msgs.AddRange(data);

            if (networkObj.Reliable)
            {
                if (networkObj.Ordered)
                    riMsgs.AddRange(msgs);
                else
                    ruMsgs.AddRange(msgs);
            }
            else
            {
                if (networkObj.Ordered)
                    uriMsgs.AddRange(msgs);
                else
                    uruMsgs.AddRange(msgs);
            }

            msgs.Clear();
        }

        /// <summary>
        /// Renders the axis-aligned bounding box obtained from the physics engine for each
        /// GeometryNode added to the physics engine for debugging.
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="aabb"></param>
        protected void RenderAabb(Matrix worldTransform, BoundingBox aabb)
        {
            Vector3 min = aabb.Min;
            Vector3 max = aabb.Max;

            Vector3 minMaxZ = Vector3Helper.Get(min.X, min.Y, max.Z);
            Vector3 minMaxX = Vector3Helper.Get(max.X, min.Y, min.Z);
            Vector3 minMaxY = Vector3Helper.Get(min.X, max.Y, min.Z);
            Vector3 maxMinX = Vector3Helper.Get(min.X, max.Y, max.Z);
            Vector3 maxMinY = Vector3Helper.Get(max.X, min.Y, max.Z);
            Vector3 maxMinZ = Vector3Helper.Get(max.X, max.Y, min.Z);

            VertexPositionColor[] verts = new VertexPositionColor[8];
            verts[0] = new VertexPositionColor(min, ColorHelper.Empty);
            verts[1] = new VertexPositionColor(max, ColorHelper.Empty);
            verts[2] = new VertexPositionColor(minMaxX, ColorHelper.Empty);
            verts[3] = new VertexPositionColor(minMaxY, ColorHelper.Empty);
            verts[4] = new VertexPositionColor(minMaxZ, ColorHelper.Empty);
            verts[5] = new VertexPositionColor(maxMinX, ColorHelper.Empty);
            verts[6] = new VertexPositionColor(maxMinY, ColorHelper.Empty);
            verts[7] = new VertexPositionColor(maxMinZ, ColorHelper.Empty);

            short[] indecies = new short[24];

            indecies[0] = 0; indecies[1] = 2;
            indecies[2] = 0; indecies[3] = 3;
            indecies[4] = 0; indecies[5] = 4;
            indecies[6] = 1; indecies[7] = 5;
            indecies[8] = 1; indecies[9] = 6;
            indecies[10] = 1; indecies[11] = 7;
            indecies[12] = 3; indecies[13] = 5;
            indecies[14] = 3; indecies[15] = 7;
            indecies[16] = 4; indecies[17] = 5;
            indecies[18] = 4; indecies[19] = 6;
            indecies[20] = 2; indecies[21] = 6;
            indecies[22] = 2; indecies[23] = 7;

            aabbShader.Render(
                worldTransform,
                "",
                delegate
                {
                    State.Device.DrawUserIndexedPrimitives<VertexPositionColor>(
                        PrimitiveType.LineList, verts, 0, 8, indecies, 0, 12);
                });
        }

        /// <summary>
        /// Renders the detailed collision mesh obtained from the physics engine for each
        /// GeometryNode added to the physics engine for debugging.
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="collisionMesh"></param>
        protected void RenderColMesh(Matrix worldTransform, List<List<Vector3>> collisionMesh)
        {
            if (collisionMesh == null)
                return;

            int count = 0;
            int indiceCount = 0;
            foreach(List<Vector3> polygonVerts in collisionMesh)
                count += polygonVerts.Count;

            VertexPositionColor[] verts = new VertexPositionColor[count];
            indiceCount = count * 2;
            count = 0;
            for (int i = 0; i < collisionMesh.Count; i++)
                for (int j = 0; j < collisionMesh[i].Count; j++)
                    verts[count++] = new VertexPositionColor(collisionMesh[i][j], cmeshColor);

            short[] indices = new short[indiceCount];
            indiceCount = 0;
            count = 0;
            int initCount = 0;
            for (int i = 0; i < collisionMesh.Count; i++)
            {
                initCount = count;

                for (int j = 0; j < collisionMesh[i].Count - 1; j++, count++)
                {
                    indices[indiceCount++] = (short)count;
                    indices[indiceCount++] = (short)(count + 1);
                }

                indices[indiceCount++] = (short)count;
                indices[indiceCount++] = (short)initCount;
                count++;
            }

            if (verts.Length > 0)
            {
                cmeshShader.Render(
                    worldTransform,
                    "",
                    delegate
                    {
                        State.Device.DrawUserIndexedPrimitives<VertexPositionColor>(
                            PrimitiveType.LineList, verts, 0, verts.Length, indices, 0, indices.Length / 2);
                    });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Indicates whether a node with the specified name exists in the current scene graph.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasNode(String name)
        {
            return nodeTable.ContainsKey(name);
        }

        /// <summary>
        /// Gets a Node object added to this scene graph with its node name.
        /// Null is returned if the name does not exist.
        /// </summary>
        /// <param name="name">The name of the node you're looking for</param>
        /// <returns></returns>
        public Node GetNode(String name)
        {
            return nodeTable[name];
        }

        /// <summary>
        /// Enables or disables the rendering of a certain group with the specified groupID.
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="enable"></param>
        public void EnableRenderGroups(int groupID, bool enable)
        {
            if (renderGroups.ContainsKey(groupID))
                renderGroups[groupID] = enable;
        }

        /// <summary>
        /// Adds a network object to send or receive messages associated with the
        /// object over the network.
        /// </summary>
        /// <param name="networkObj"></param>
        public void AddNetworkObject(INetworkObject networkObj)
        {
            if (!networkObjects.ContainsKey(networkObj.Identifier))
                networkObjects.Add(networkObj.Identifier, new NetObj(networkObj));
        }

        /// <summary>
        /// Removes a network object.
        /// </summary>
        /// <param name="networkObj"></param>
        public void RemoveNetworkObject(INetworkObject networkObj)
        {
            networkObjects.Remove(networkObj.Identifier);
        }

        /// <summary>
        /// Adds a video streaming decoder implementation for background rendering and 
        /// marker tracking.
        /// </summary>
        /// <remarks>
        /// The video capture device should be initialized before it can be added.
        /// </remarks>
        /// <param name="decoder">A video streaming decoder implementation</param>
        /// <exception cref="GoblinException">If the device is not initialized</exception>
        public virtual void AddVideoCaptureDevice(IVideoCapture decoder)
        {
            if (decoder == null)
                return;

            if (!decoder.Initialized)
                throw new GoblinException("You should initialize the video capture device first " +
                    "before you add it");

            MarkerBase.Instance.VideoCaptures.Add(decoder);

            overlayVideoID = decoder.VideoDeviceID;
            trackerVideoID = decoder.VideoDeviceID;
            actualOverlayVideoID = videoIDs.Count;
            actualTrackerVideoID = videoIDs.Count;
            MarkerBase.Instance.ActiveCaptureDevice = actualOverlayVideoID;
            videoIDs.Add(decoder.VideoDeviceID, actualOverlayVideoID);
            overlayAndTrackerUseSameID = true;
        }

        /// <summary>
        /// Captures the current frame/back buffer and stores it in a file with the specified format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureScene(String filename, ImageFileFormat format)
        {
            ResolveTexture2D screen = new ResolveTexture2D(State.Device, State.Width, State.Height,
                1, SurfaceFormat.Color);

            State.Device.ResolveBackBuffer(screen);

            screen.Save(filename, format);
        }

        /// <summary>
        /// Only renders the 3D scene. Unlike the Draw function, this function doesn't perform physics
        /// update or scene graph updates. It simply renders the 3D scene. This method is useful when you
        /// need to render the scene more than once (e.g., when rendering multiple viewport or stereoscopic
        /// view).
        /// </summary>
        /// <param name="renderUI">Whether to render 2D UI</param>
        public void RenderScene(bool renderUI)
        {
            if (cameraNode.Stereo)
            {
                if (renderLeftView)
                {
                    State.ViewMatrix = cameraNode.LeftCompoundViewMatrix;
                    State.ProjectionMatrix = ((StereoCamera)cameraNode.Camera).LeftProjection;
                    renderLeftView = false;
                }
                else
                {
                    State.ViewMatrix = cameraNode.RightCompoundViewMatrix;
                    State.ProjectionMatrix = ((StereoCamera)cameraNode.Camera).RightProjection;
                    renderLeftView = true;
                }
            }
            else
            {
                State.ViewMatrix = cameraNode.CompoundViewMatrix;
                State.ProjectionMatrix = cameraNode.Camera.Projection;
            }

            State.CameraTransform = cameraNode.WorldTransformation;

            try
            {
                if (enableShadowMapping)
                    RenderShadows(globalLightSources);
            }
            catch (Exception) { }

            RenderSceneGraph(globalLightSources);

            if (renderUI)
            {
                GameTime tmpTime = new GameTime();
                uiRenderer.Draw(tmpTime);
            }
        }

        #endregion

        #region Override Methods

        public override void Update(GameTime gameTime)
        {
            InputMapper.Instance.Update(gameTime, this.Game.IsActive);

            foreach (ParticleNode particle in renderedEffects)
                particle.Update(gameTime);

            Sound.Update(gameTime);
            if(cameraNode != null)
                Sound.UpdateListener(gameTime, cameraNode.WorldTransformation.Translation,
                    cameraNode.WorldTransformation.Forward, cameraNode.WorldTransformation.Up);

            // Take care of the networking
            if (State.EnableNetworking)
            {
                networkMessages.Clear();
                bool sendAll = false;
                if (State.IsServer)
                    networkServer.ReceiveMessage(ref networkMessages);
                else
                    networkClient.ReceiveMessage(ref networkMessages);

                String identifier = "";
                String[] splits = null;
                char[] seps = { ':' };
                byte[] inputData = null;
                byte[] data = null;
                short size = 0;
                int index = 0;
                foreach (byte[] msg in networkMessages)
                {
                    index = 0;
                    while (index < msg.Length)
                    {
                        size = ByteHelper.ConvertToShort(msg, index);
                        data = ByteHelper.Truncate(msg, index + 2, size);
                        //Console.WriteLine("Received: " + ByteHelper.ConvertToString(data));
                        splits = ByteHelper.ConvertToString(data).Split(seps);
                        identifier = splits[0];
                        if ((data.Length - identifier.Length) > 0)
                            inputData = ByteHelper.Truncate(data, identifier.Length + 1,
                                data.Length - identifier.Length - 1);

                        if (networkObjects.ContainsKey(identifier))
                            networkObjects[identifier].NetworkObject.InterpretMessage(inputData);
                        else if (identifier.Equals("NewConnectionEstablished"))
                            sendAll = true;
                        else
                            Log.Write("Network Identifier: " + identifier + " is not found");

                        index += (size + 2);
                    }

                    // If we're server, then broadcast the message received from the client to
                    // all of the connected clients except the client which sent the message
                    //if (State.IsServer)
                    //    networkServer.BroadcastMessage(msg, true, true, true);
                }

                foreach (NetObj netObj in networkObjects.Values)
                    if (!netObj.NetworkObject.Hold)
                        netObj.TimeElapsedSinceLastTransmit +=
                            (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                reliableInOrderMsgs.Clear();
                unreliableInOrderMsgs.Clear();
                reliableUnOrderMsgs.Clear();
                unreliableUnOrderMsgs.Clear();
                List<byte> msgs = new List<byte>();

                if (State.IsServer)
                {
                    if (sendAll)
                    {
                        foreach (NetObj netObj in networkObjects.Values)
                        {
                            if (!netObj.NetworkObject.Hold)
                                AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                    unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);
                        }
                    }
                    else
                    {
                        if (networkServer.NumConnectedClients >= State.NumberOfClientsToWait)
                        {
                            foreach (NetObj netObj in networkObjects.Values)
                                if (!netObj.NetworkObject.Hold &&
                                    (netObj.NetworkObject.ReadyToSend || netObj.IsTimeToTransmit))
                                {
                                    AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                        unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);

                                    netObj.NetworkObject.ReadyToSend = false;
                                    netObj.TimeElapsedSinceLastTransmit = 0;
                                }
                        }
                    }

                    if (reliableInOrderMsgs.Count > 0)
                        networkServer.BroadcastMessage(reliableInOrderMsgs.ToArray(), true, true, false);
                    if (reliableUnOrderMsgs.Count > 0)
                        networkServer.BroadcastMessage(reliableUnOrderMsgs.ToArray(), true, false, false);
                    if (unreliableInOrderMsgs.Count > 0)
                        networkServer.BroadcastMessage(unreliableInOrderMsgs.ToArray(), false, true, false);
                    if (unreliableUnOrderMsgs.Count > 0)
                        networkServer.BroadcastMessage(unreliableUnOrderMsgs.ToArray(), false, false, false);
                }
                else
                {
                    if (networkClient.IsConnected)
                    {
                        foreach (NetObj netObj in networkObjects.Values)
                            if (!netObj.NetworkObject.Hold && 
                                (netObj.NetworkObject.ReadyToSend || netObj.IsTimeToTransmit))
                            {
                                AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                    unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);

                                netObj.NetworkObject.ReadyToSend = false;
                                netObj.TimeElapsedSinceLastTransmit = 0;
                            }

                        if (reliableInOrderMsgs.Count > 0)
                            networkClient.SendMessage(reliableInOrderMsgs.ToArray(), true, true);
                        if (reliableUnOrderMsgs.Count > 0)
                            networkClient.SendMessage(reliableUnOrderMsgs.ToArray(), true, false);
                        if (unreliableInOrderMsgs.Count > 0)
                            networkClient.SendMessage(unreliableInOrderMsgs.ToArray(), false, true);
                        if (unreliableUnOrderMsgs.Count > 0)
                            networkClient.SendMessage(unreliableUnOrderMsgs.ToArray(), false, false);
                    }
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (rootNode == null || cameraNode == null)
                return;

            if (State.IsMultiCore)
                readyToUpdateTracker = true;
            else
                UpdateTracker();

            bool updatePhysicsEngine = (physicsEngine != null);

            if (State.EnableNetworking)
            {
                // If we're the server, then don't update the physics simulation until we get
                // connections from clients
                if (State.IsServer)
                {
                    if (networkServer.NumConnectedClients < State.NumberOfClientsToWait)
                        updatePhysicsEngine = false;
                }
                else
                    updatePhysicsEngine = false;
            }

            if (updatePhysicsEngine)
                physicsEngine.Update((float)gameTime.ElapsedRealTime.TotalSeconds);

            if (showCameraImage && (videoImage != null))
                MarkerBase.Instance.UpdateRendering(videoImage);

            PrepareSceneForRendering();

            // If the camera position changed, then we need to re-sort the transparency group
            if (!prevCameraTrans.Equals(cameraNode.WorldTransformation.Translation))
                needTransparencySort = true;

            prevCameraTrans = cameraNode.WorldTransformation.Translation;

            globalLightSources.Clear();
            foreach (LightNode lightNode in globalLights)
            {
                foreach (LightSource lightSource in lightNode.LightSources)
                {
                    if (lightSource.Enabled)
                    {
                        LightSource light = new LightSource(lightSource);
                        if (light.Type != LightType.Directional)
                        {
                            tmpMat1 = lightNode.WorldTransformation;
                            tmpVec1 = light.Position;
                            Matrix.CreateTranslation(ref tmpVec1, out tmpMat2);
                            Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                            light.Position = tmpMat3.Translation;
                        }
                        if (light.Type != LightType.Point)
                        {
                            tmpVec1 = light.Direction;
                            tmpMat1 = lightNode.WorldTransformation;
                            Matrix.CreateTranslation(ref tmpVec1, out tmpMat2);
                            MatrixHelper.GetRotationMatrix(ref tmpMat1, out tmpMat3);
                            Matrix.Multiply(ref tmpMat2, ref tmpMat3, out tmpMat1);

                            light.Direction = tmpMat1.Translation;
                        }
                        globalLightSources.Add(light);
                    }
                }
            }

            foreach (LODNode lodNode in lodNodes)
                if (lodNode.AutoComputeLevelOfDetail)
                    lodNode.Update(cameraNode.WorldTransformation.Translation);

            RenderScene(false);
        }

        protected override void Dispose(bool disposing)
        {
            if(updateThread != null)
                updateThread.Abort();

            renderGroups.Clear();
            nodeRenderGroups.Clear();
            transparentGroup.Clear();
            networkObjects.Clear();
            renderedEffects.Clear();
            occluderGroup.Clear();

            if(rootNode != null)
                rootNode.Dispose();

            rootNode = null;
            cameraNode = null;
            globalLights.Clear();

            if(physicsEngine != null)
                physicsEngine.Dispose();
            if (networkServer != null)
                networkServer.Shutdown();
            if (networkClient != null)
                networkClient.Shutdown();

            if (markerModuleInited)
                MarkerBase.Instance.Dispose();

            Sound.Dispose();
            InputMapper.Instance.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region Protected Classes
        protected class NetObj
        {
            private INetworkObject networkObject;
            private float timeElapsedSinceLastTransmit;
            private float transmitSpan;

            public NetObj(INetworkObject netObj)
            {
                this.networkObject = netObj;
                timeElapsedSinceLastTransmit = 0;
                if (networkObject.SendFrequencyInHertz != 0)
                    transmitSpan = 1000 / (float)networkObject.SendFrequencyInHertz;
                else
                    transmitSpan = float.MaxValue;
            }

            public INetworkObject NetworkObject
            {
                get { return networkObject; }
            }

            public float TimeElapsedSinceLastTransmit
            {
                get { return timeElapsedSinceLastTransmit; }
                set { timeElapsedSinceLastTransmit = value; }
            }

            public bool IsTimeToTransmit
            {
                get { return (timeElapsedSinceLastTransmit >= transmitSpan); }
            }
        }
        #endregion
    }
}
