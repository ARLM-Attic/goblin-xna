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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.IO;

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
        #region Delegates

        public delegate void RenderBeforeUI(bool leftView);

        public delegate void RenderAfterUI(bool leftView);

        #endregion

        #region Member Fields
        protected const int VIDEO_BUFFER_SIZE = 1;
        /// <summary>
        /// The root node of this scene graph
        /// </summary>
        protected BranchNode rootNode;
        /// <summary>
        /// A hash table that stores Node objects with their names as the keys
        /// </summary>
        protected Dictionary<String, Node> nodeTable;

        /// <summary>
        /// A list of geometry nodes that are opaque (same as nodeRenderGroups but in
        /// a 1dimensional list for easy searching)
        /// </summary>
        protected List<GeometryNode> opaqueGroup;
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

        protected List<GeometryNode> ignoreDepthGroup;
        /// <summary>
        /// A list of particle effects that need to be rendered
        /// </summary>
        protected List<ParticleNode> renderedEffects;
        
        protected INetworkHandler networkHandler;
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

        /// <summary>
        /// Indicates whether we need to pass lighting info to shaders to update them
        /// </summary>
        protected bool needsToUpdateGlobalLighting;

        /// <summary>
        /// The marker tracker object
        /// </summary>
        protected IMarkerTracker markerTracker;
        /// <summary>
        /// The video capture object
        /// </summary>
        protected List<IVideoCapture> videoCaptures;

        protected Texture2D[] videoTextures;

        protected List<LODNode> lodNodes;

        protected GoblinXNA.Graphics.Environment environment;

        protected SpriteBatch spriteBatch;

        protected bool trackMarkers;
        protected bool markerModuleInited;
        protected List<MarkerNode> markerUpdateList;

        protected Color aabbColor;
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
        protected Color videoBackgroundColor;

        protected List<LightSource> globalLightSources;

        protected float physicsElapsedTime;

        /// <summary>
        /// For stereo rendering
        /// </summary>
        protected bool renderLeftView;

        protected float uiElapsedTime;

        protected ResolveTexture2D screen;

        protected String curCamNodeName; // used only when loading a scene graph from XML file

        protected RenderBeforeUI renderBeforeUI;
        protected RenderAfterUI renderAfterUI;

        protected bool isStarted;

        #region For Threading

        protected Thread markerTrackingThread;
        protected Thread physicsThread;
        protected bool isMarkerTrackingThreaded;
        protected bool isPhysicsThreaded;

        #endregion

        #region For Video Image Buffering

        protected int curVideoBufferIndex;
        protected int prevVideoBufferIndex;
        protected int prevMarkerProcessedIndex;
        protected bool renderingVideoTexture;
        protected bool copyingVideoImage;
        protected int[][][] bufferedVideoImages;
        protected IntPtr[] bufferedVideoPointers;
        protected int prevPointerSize;
        protected IntPtr nullPtr = IntPtr.Zero;
        protected bool readyToUpdateTracker;

        #endregion

        // Indicates whether the scene graph is currently being processed. This is used
        // to avoid adding a node while processing (removing while processing is acceptable)
        protected bool processing;

        #region For Augmented Reality Scene
        protected bool showCameraImage;
        protected int trackerVideoID;
        protected int actualTrackerVideoID;
        protected Dictionary<int, int> videoIDs;
        protected bool freezeVideo;

        // These variables are used to avoid updating the tracker while changing the video overlay
        // ID or tracker ID, or visa versa
        protected bool waitForVideoIDChange;
        protected bool waitForTrackerUpdate;

        // These variables are used for synchronizing the threaded (if State.MultiCore is true)
        // tracker update with the actual frame update
        protected uint trackerUpdateCount;
        protected uint frameUpdateCount;

        protected float prevTrackerTime;

        #region For Stereo Augmented Reality
        protected int leftEyeVideoID;
        protected int rightEyeVideoID;
        protected bool singleVideoStereo;
        protected int actualLeftEyeVideoID;
        protected int actualRightEyeVideoID;
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
            mainGame.Components.Add(this);
            this.DrawOrder = 101;

            uiRenderer = new UIRenderer();

            spriteBatch = new SpriteBatch(State.Device);

            rootNode = new BranchNode("Root");
            rootNode.SceneGraph = this;
            nodeTable = new Dictionary<string, Node>();

            videoCaptures = new List<IVideoCapture>();
            
            renderGroups = new Dictionary<int, bool>();
            transparentGroup = new List<GeometryNode>();
            opaqueGroup = new List<GeometryNode>();
            needTransparencySort = false;
            renderedEffects = new List<ParticleNode>();
            occluderGroup = new List<GeometryNode>();
            ignoreDepthGroup = new List<GeometryNode>();

            transparencySortOrder = new DefaultTransparencyComparer();

            cameraNode = null;
            prevCameraTrans = new Vector3();

            globalLights = new List<LightNode>();
            localLights = new Stack<LightNode>();
            globalLightSources = new List<LightSource>();
            needsToUpdateGlobalLighting = false;

            emptyLightList = new List<LightNode>();
            emptyMaterial = new Material();

            lodNodes = new List<LODNode>();

            environment = null;

            enableShadowMapping = false;
            enableLighting = true;
            preferPerPixelLighting = false;

            enableFrustumCulling = true;

            aabbColor = Color.Yellow;
            cmeshColor = Color.Purple;

            markerModuleInited = false;

            triangleCount = 0;
            renderAabb = false;
            renderCollisionMesh = false;

            renderLeftView = true;

            backgroundTexture = null;
            backgroundColor = Color.CornflowerBlue;
            videoBackgroundColor = Color.White;

            showCameraImage = false;
            trackerVideoID = 0;
            actualTrackerVideoID = 0;
            videoIDs = new Dictionary<int, int>();
            freezeVideo = false;
            waitForVideoIDChange = false;
            waitForTrackerUpdate = false;

            markerUpdateList = new List<MarkerNode>();
            prevTrackerTime = 0;

            leftEyeVideoID = -1;
            rightEyeVideoID = -1;
            actualLeftEyeVideoID = -1;
            actualRightEyeVideoID = -1;
            singleVideoStereo = true;

            trackerUpdateCount = 0;
            frameUpdateCount = 0;
            readyToUpdateTracker = false;

            physicsElapsedTime = 0;

            processing = false;
            isStarted = false;
            curCamNodeName = "";

            isMarkerTrackingThreaded = ((State.ThreadOption & (ushort)ThreadOptions.MarkerTracking) != 0);
            isPhysicsThreaded = ((State.ThreadOption & (ushort)ThreadOptions.PhysicsSimulation) != 0);

            // two per index for image buffers for stereo handling (even if stereo AR is not used)
            curVideoBufferIndex = 0;
            prevVideoBufferIndex = 0;
            prevMarkerProcessedIndex = 0;
            bufferedVideoImages = new int[2][][];
            videoTextures = new Texture2D[2];
            bufferedVideoPointers = new IntPtr[VIDEO_BUFFER_SIZE];

            for (int i = 0; i < 2; i++)
                bufferedVideoImages[i] = new int[VIDEO_BUFFER_SIZE][];

            if (isMarkerTrackingThreaded)
            {
                markerTrackingThread = new Thread(UpdateTracker);
                markerTrackingThread.Start();
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
                while (processing) { }
                rootNode = value; 

                // Clears all of the saved information related to scene graph
                nodeTable.Clear();
                renderGroups.Clear();
                transparentGroup.Clear();
                opaqueGroup.Clear();
                renderedEffects.Clear();
                occluderGroup.Clear();
                globalLights.Clear();
                markerUpdateList.Clear();
                lodNodes.Clear();
            }
        }

        /// <summary>
        /// Gets or sets the camera node of this scene graph. (There can be multiple camera nodes
        /// in one scene graph, but only one camera node should be active at a time.)
        /// </summary>
        public CameraNode CameraNode
        {
            get { return cameraNode; }
            set 
            {
                while (processing) { }
                cameraNode = value; 
            }
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
        /// Gets or sets the background color mixed with the video image. This is applicable only when
        /// ShowCameraImage is set to true. Default color is Color.White.
        /// </summary>
        /// <remarks>
        /// You can use this property to, for example, modify the transparency of the video image or
        /// mix a color to it.
        /// </remarks>
        public Color VideoBackgroundColor
        {
            get { return videoBackgroundColor; }
            set { videoBackgroundColor = value; }
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
            get { return markerTracker; }
            set
            {
                if (value == null)
                    return;

                if (!value.Initialized)
                    throw new GoblinException("You have to initialize the tracker before you assign " +
                        "to Scene.MarkerTracker");

                markerTracker = value;

                if (cameraNode == null)
                {
                    Camera markerCamera = new Camera();

                    markerCamera.View = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1),
                        new Vector3(0, 1, 0));

                    markerCamera.Projection = markerTracker.CameraProjection;

                    CameraNode markerCameraNode = new CameraNode("MarkerCameraNode", markerCamera);
                    RootNode.AddChild(markerCameraNode);
                    CameraNode = markerCameraNode;
                }
                else
                {
                    cameraNode.Camera.Projection = markerTracker.CameraProjection;
                }
            }
        }

        /// <summary>
        /// Gets a list of video capture instances.
        /// </summary>
        /// <returns>The video capture class</returns>
        public List<IVideoCapture> VideoCaptures
        {
            get { return videoCaptures; }
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
        /// Gets or sets the network handler implementation used for this scene graph.
        /// </summary>
        public INetworkHandler NetworkHandler
        {
            get { return networkHandler; }
            set { networkHandler = value; }
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
            set 
            { 
                enableLighting = value;
                needsToUpdateGlobalLighting = true;
            }
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
        /// Gets or sets the color used to draw the axis-aligned bounding box of each model for 
        /// debugging. Default color is Color.YellowGreen.
        /// </summary>
        public Color AabbColor
        {
            get { return aabbColor; }
            set { aabbColor = value; }
        }

        /// <summary>
        /// Gets or sets the color used to draw the actual mesh used for collision detection of 
        /// each model for debugging. Default color is Color.Purple.
        /// </summary>
        public Color CollisionMeshColor
        {
            get { return cmeshColor; }
            set { cmeshColor = value; }
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
            get { return videoCaptures; }
        }

        /// <summary>
        /// Gets or sets whether to show camera captured physical image in the background.
        /// By default, this is false. 
        /// </summary>
        /// <see cref="OverlayVideoID"/>
        public bool ShowCameraImage
        {
            get { return showCameraImage; }
            set 
            {
                while (processing) { }
                showCameraImage = value; 
            }
        }

        /// <summary>
        /// Gets or sets the video capture device ID used to provide the overlaid physical image.
        /// This ID should correspond to the videoDeviceID given to the initialized video device
        /// using InitVideoCapture method. 
        /// </summary>
        /// <remarks>
        /// Getting or setting this property is exactly same as getting or setting the LeftEyeVideoID
        /// property.
        /// </remarks>
        /// <exception cref="GoblinException"></exception>
        public int OverlayVideoID
        {
            get { return LeftEyeVideoID; }
            set { LeftEyeVideoID = value; }
        }

        /// <summary>
        /// Gets or sets the video capture device ID used to perform marker tracking (if available).
        /// This ID should correspond to the videoDeviceID given to the initialized video device
        /// using InitVideoCapture method. 
        /// </summary>
        public int TrackerVideoID
        {
            get { return trackerVideoID; }
            set
            {
                // Wait for the tracker update to end before modifying the ID
                while (waitForTrackerUpdate) { }
                waitForVideoIDChange = true;
                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("TrackerVideoID " + value + " does not exist");

                actualTrackerVideoID = videoIDs[value];
                trackerVideoID = value;
                InitializeVideoPointerSize(videoCaptures[actualTrackerVideoID]);

                waitForVideoIDChange = false;
            }
        }

        /// <summary>
        /// Gets or sets the video ID for left eye to use for stereo augmented reality. If you use 
        /// single camera for stereo, then you should set both LeftEyeVideoID and RightEyeVideoID to 
        /// the same video ID. 
        /// </summary>
        /// <exception cref="GoblinException">If video ID is not valid or your camera node does not
        /// contain stereo information.</exception>
        /// <see cref="RightEyeVideoID"/>
        public int LeftEyeVideoID
        {
            get { return leftEyeVideoID; }
            set 
            {
                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("VideoID " + value + " does not exist");

                //if (!cameraNode.Stereo)
                //    throw new GoblinException("You should set OverlayVideoID instead since your current " +
                //        "camera node does not contain stereo information");

                // Wait for the tracker update to end before modifying the ID
                while (waitForTrackerUpdate) { }
                waitForVideoIDChange = true;

                actualLeftEyeVideoID = videoIDs[value];
                leftEyeVideoID = value;
                InitializeVideoImageSize(0, videoCaptures[actualLeftEyeVideoID]);

                singleVideoStereo = (leftEyeVideoID == rightEyeVideoID);

                waitForVideoIDChange = false;
            }
        }

        /// <summary>
        /// Gets or sets the video ID for right eye to use for stereo augmented reality. If you use 
        /// single camera for stereo, then you should set both LeftEyeVideoID and RightEyeVideoID to 
        /// the same video ID.
        /// </summary>
        /// <exception cref="GoblinException">If video ID is not valid or your camera node does not
        /// contain stereo information.</exception>
        /// <see cref="LeftEyeVideoID"/>
        public int RightEyeVideoID
        {
            get { return rightEyeVideoID; }
            set
            {
                if (!videoIDs.ContainsKey(value))
                    throw new GoblinException("VideoID " + value + " does not exist");

                if (!cameraNode.Stereo)
                    throw new GoblinException("You should set OverlayVideoID instead since your current " +
                        "camera node does not contain stereo information");

                // Wait for the tracker update to end before modifying the ID
                while (waitForTrackerUpdate) { }
                waitForVideoIDChange = true;

                actualRightEyeVideoID = videoIDs[value];
                rightEyeVideoID = value;
                InitializeVideoImageSize(1, videoCaptures[actualRightEyeVideoID]);

                singleVideoStereo = (leftEyeVideoID == rightEyeVideoID);

                waitForVideoIDChange = false;
            }
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
        /// Sets the callback function that is invoked after all 3D geometries are rendered, but
        /// right before the 2D UIs are rendered.
        /// </summary>
        public RenderBeforeUI RenderBeforeUICallback
        {
            set { renderBeforeUI = value; }
        }

        /// <summary>
        /// Sets the callback function that is invoked after the 2D UIs are rendered.
        /// </summary>
        public RenderAfterUI RenderAfterUICallback
        {
            set { renderAfterUI = value; }
        }

        /// <summary>
        /// Gets or sets whether you need the image pointer to be passed back by your
        /// IVideoCapture.GetTextureImage(...) even if it is not needed by the Scene class.
        /// Default value is false. Use this with caution.
        /// </summary>
        public bool NeedsImagePtr { get; set; }

        /// <summary>
        /// Gets or sets whether you need the image data (int array) to be passed back by your
        /// IVideoCapture.GetTextureImage(...) even if it is not needed by the Scene class.
        /// Default value is false. Use this with caution.
        /// </summary>
        public bool NeedsImageData { get; set; }

        /// <summary>
        /// Gets or sets the shift amount applied to all 2D HUD drawings including the text
        /// in the X screen coordinate. 
        /// </summary>
        /// <remarks>
        /// This shift is applied only when right eye view is rendered in stereo mode. 
        /// </remarks>
        public int GlobalUIShift
        {
            get { return uiRenderer.GlobalUIShift; }
            set { uiRenderer.GlobalUIShift = value; }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Indicates whether the scene graph is currently being traversed.
        /// </summary>
        internal bool Processing
        {
            get { return processing; }
        }

        /// <summary>
        /// Indicates whether the scene processing has already started.
        /// </summary>
        /// <remarks>
        /// This is used to check whether adding nodes outside of tree traversal is 
        /// exception free. 
        /// </remarks>
        internal bool IsStarted
        {
            get { return isStarted; }
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

                        tmpVec2 = tNode.PreTranslation;
                        tmpVec3 = tNode.PostTranslation;

                        Matrix.CreateTranslation(ref tmpVec2, out tmpMat1);
                        Matrix.CreateScale(ref tmpVec1, out tmpMat2);
                        Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                        Matrix.CreateFromQuaternion(ref tmpQuat1, out tmpMat1);
                        Matrix.Multiply(ref tmpMat3, ref tmpMat1, out tmpMat2);

                        Matrix.CreateTranslation(ref tmpVec3, out tmpMat1);
                        Matrix.Multiply(ref tmpMat2, ref tmpMat1, out nodeWorldTransformation);

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
                bool shouldBeSynchronized = (gNode is SynchronizedGeometryNode);

                if (shouldBeSynchronized && State.EnableNetworking && State.IsServer)
                    prevMatrix = gNode.WorldTransformation;

                if (!shouldBeSynchronized || !(State.EnableNetworking && !State.IsServer))
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

                    if (gNode.Model != null && gNode.Model.OffsetToOrigin)
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
                    if (gNode.AddToPhysicsEngine)
                        physicsEngine.AddPhysicsObject(gNode.Physics);
                    else
                        physicsEngine.RemovePhysicsObject(gNode.Physics);

                    gNode.PhysicsStateChanged = false;
                }

                gNode.MarkerTransform = markerTransform;

                parentTransformation = gNode.WorldTransformation;

                if (shouldBeSynchronized && State.EnableNetworking && State.IsServer)
                {
                    //if (MatrixHelper.HasMovedSignificantly(prevMatrix, gNode.WorldTransformation))
                        ((SynchronizedGeometryNode)gNode).ReadyToSend = true;
                }

                if (gNode.OcclusionStateChanged)
                {
                    RemoveFromRenderedGroups(gNode);
                    gNode.OcclusionStateChanged = false;
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
                        AddToOpaqueGroup(gNode);
                }

                if (gNode is LODNode)
                {
                    LODNode lodNode = (LODNode)gNode;
                    if (!lodNodes.Contains(lodNode))
                        lodNodes.Add(lodNode);
                }

                if (gNode.Model != null)
                {
                    BoundingSphere boundingVol = gNode.BoundingVolume;
                    tmpMat1 = gNode.WorldTransformation;

                    tmpVec1.X = (float)Math.Sqrt(tmpMat1.M11 * tmpMat1.M11 + tmpMat1.M12 * tmpMat1.M12 + tmpMat1.M13 * tmpMat1.M13);
                    tmpVec1.Y = (float)Math.Sqrt(tmpMat1.M21 * tmpMat1.M21 + tmpMat1.M22 * tmpMat1.M22 + tmpMat1.M23 * tmpMat1.M23);
                    tmpVec1.Z = (float)Math.Sqrt(tmpMat1.M31 * tmpMat1.M31 + tmpMat1.M32 * tmpMat1.M32 + tmpMat1.M33 * tmpMat1.M33);

                    boundingVol.Radius = gNode.Model.MinimumBoundingSphere.Radius * Math.Max(tmpVec1.X,
                        Math.Max(tmpVec1.Y, tmpVec1.Z));

                    tmpVec1 = gNode.Model.MinimumBoundingSphere.Center;

                    Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                    tmpMat2 = gNode.WorldTransformation;
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

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
                }

                // Set the local lights that will affect this geometry model
                if (gNode.LocalLights.Count != localLights.Count)
                {
                    gNode.LocalLights.Clear();
                    gNode.LocalLights.AddRange(localLights);
                    gNode.NeedsToUpdateLocalLights = true;
                }
                else
                {
                    bool allMatch = true;
                    foreach(LightNode light in gNode.LocalLights)
                        if (!localLights.Contains(light))
                        {
                            allMatch = false;
                            break;
                        }

                    if (allMatch)
                    {
                        foreach (LightNode light in gNode.LocalLights)
                        {
                            gNode.NeedsToUpdateLocalLights |= light.HasChanged;
                            light.HasChanged = false;
                        }
                    }
                    else
                    {
                        gNode.LocalLights.Clear();
                        gNode.LocalLights.AddRange(localLights);
                        gNode.NeedsToUpdateLocalLights = true;
                    }
                }

                gNode.ShouldRender = true;
            }
            else if (node is MarkerNode)
            {
                trackMarkers = true;
                MarkerNode markerNode = (MarkerNode)node;
                if (!markerUpdateList.Contains(markerNode))
                    markerUpdateList.Add(markerNode);
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

                pNode.MarkerTransformation = markerTransform;
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
                        RecursivelyRemoveFromScene(child);
                    sNode.SwitchChanged = false;
                }
            }
            else if (node is SoundNode)
            {
                SoundNode soNode = (SoundNode)node;
                if (soNode.Parent is GeometryNode)
                    tmpMat1 = ((GeometryNode)soNode.Parent).WorldTransformation;
                else
                    tmpMat1 = parentTransformation;

                if (markerTransform.M44 == 0)
                {
                    // have not decided what to do yet if marker is not detected
                }
                else
                {
                    Matrix.Multiply(ref tmpMat1, ref markerTransform, out tmpMat1);
                }

                soNode.WorldTransformation = tmpMat1;
            }

            if (node is BranchNode)
            {
                BranchNode bNode = (BranchNode)node;
                // before processing them, first add or remove nodes if necessary
                if (bNode.ChangeList.Count > 0)
                {
                    List<BranchNode.NodeChangeInfo> changeList = 
                        new List<BranchNode.NodeChangeInfo>(bNode.ChangeList);
                    bNode.ChangeList.Clear();
                    foreach (BranchNode.NodeChangeInfo info in changeList)
                    {
                        if (info.Type == BranchNode.ChangeType.Add)
                            bNode.Children.Add(info.Node);
                        else
                        {
                            bNode.Children.Remove(info.Node);
                            RecursivelyRemoveFromScene(info.Node);
                        }
                    }
                }
                if (switchPass >= 0)
                {
                    if (bNode.Children[switchPass] is LightNode)
                    {
                        int numLocalLights = 0;
                        ProcessLightNode((LightNode)bNode.Children[switchPass],
                            ref parentTransformation, ref parentMarkerTransform,
                            ref numLocalLights);
                        if(numLocalLights > 0)
                            localLights.Pop();
                    }
                    else
                        RecursivePrepareForRendering(bNode.Children[switchPass], ref parentTransformation,
                            ref parentMarkerTransform, isWorldTransformationDirty || calculateAll);
                }
                else
                {
                    if (!(bNode.Prune || pruneForMarkerNode))
                    {
                        int numLocalLights = 0;
                        // First, only look for the LightNode since if it's local, we need to propagate it
                        // to the siblings as well as the descendants
                        foreach (Node child in bNode.Children)
                        {
                            if (child is LightNode)
                                ProcessLightNode((LightNode)child,
                                    ref parentTransformation, ref parentMarkerTransform, 
                                    ref numLocalLights);
                        }

                        // Now, go through all of the child nodes except the light nodes
                        foreach (Node child in bNode.Children)
                        {
                            if (!(child is LightNode))
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

        protected virtual void ProcessLightNode(LightNode lNode, ref Matrix parentTransformation, 
            ref Matrix parentMarkerTransform, ref int numLocalLights)
        {
            if (lNode.Enabled)
            {
                Matrix.Multiply(ref parentTransformation, ref parentMarkerTransform, out tmpMat1);
                lNode.WorldTransformation = tmpMat1;
                if (lNode.Global)
                {
                    if (globalLights.Contains(lNode))
                    {
                        if (lNode.HasChanged)
                            needsToUpdateGlobalLighting = true;
                    }
                    else
                    {
                        globalLights.Add(lNode);
                        needsToUpdateGlobalLighting = true;
                    }

                    lNode.HasChanged = false;
                }
                else
                {
                    localLights.Push(lNode);
                    numLocalLights++;
                }
            }
            else
            {
                if (lNode.Global)
                {
                    if (globalLights.Contains(lNode))
                    {
                        globalLights.Remove(lNode);
                        needsToUpdateGlobalLighting = true;
                    }
                }
            }
        }

        /// <summary>
        /// Recursively removes a node and all of its children from the scene graph.
        /// </summary>
        /// <param name="node"></param>
        internal virtual void RecursivelyRemoveFromScene(Node node)
        {
            nodeTable.Remove(node.Name);
            if (node is GeometryNode)
                RemoveFromRenderedGroups((GeometryNode)node);
            else if (node is ParticleNode)
            {
                renderedEffects.Remove((ParticleNode)node);
                ((ParticleNode)node).IsRendered = false;
            }
            else if (node is LightNode)
            {
                if (globalLights.Contains((LightNode)node))
                {
                    globalLights.Remove((LightNode)node);
                    needsToUpdateGlobalLighting = true;
                }
            }
            else if (node is MarkerNode)
                markerUpdateList.Remove((MarkerNode)node);

            if(node is BranchNode)
                foreach (Node child in ((BranchNode)node).Children)
                    RecursivelyRemoveFromScene(child);
        }

        /// <summary>
        /// Adds a geometry node to the render group with opaque material.
        /// </summary>
        /// <param name="node"></param>
        protected virtual void AddToOpaqueGroup(GeometryNode node)
        {
            if (!node.IsRendered)
            {
                if (!renderGroups.ContainsKey(node.GroupID))
                    renderGroups.Add(node.GroupID, true);

                opaqueGroup.Add(node);

                if ((networkHandler != null) && (node is SynchronizedGeometryNode))
                    networkHandler.AddNetworkObject((SynchronizedGeometryNode)node);

                node.IsRendered = true;
            }
            else
            {
                if (transparentGroup.Contains(node))
                {
                    transparentGroup.Remove(node);
                    opaqueGroup.Add(node);
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

                if ((networkHandler != null) && (node is SynchronizedGeometryNode))
                    networkHandler.AddNetworkObject((SynchronizedGeometryNode)node);

                node.IsRendered = true;
                needTransparencySort = true;
            }
            else
            {
                if (opaqueGroup.Contains(node))
                {
                    opaqueGroup.Remove(node);
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

                if ((networkHandler != null) && (node is SynchronizedGeometryNode))
                    networkHandler.AddNetworkObject((SynchronizedGeometryNode)node);

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
                if (opaqueGroup.Contains(node))
                    opaqueGroup.Remove(node);
                else if (transparentGroup.Contains(node))
                    transparentGroup.Remove(node);
                else if (occluderGroup.Contains(node))
                    occluderGroup.Remove(node);

                if ((networkHandler != null) && (node is SynchronizedGeometryNode))
                    networkHandler.RemoveNetworkObject((SynchronizedGeometryNode)node);
                if (physicsEngine != null && node.AddToPhysicsEngine)
                    physicsEngine.RemovePhysicsObject(node.Physics);

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
                    foreach (GeometryNode node in opaqueGroup)
                        if (renderGroups[node.GroupID])
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
                    foreach (GeometryNode node in opaqueGroup)
                        if (renderGroups[node.GroupID])
                            if (node.Model != null && node.Model.Enabled && node.ShouldRender &&
                                IsWithinViewFrustum(node.BoundingVolume))
                                node.Model.UseShadows(node.WorldTransformation * node.MarkerTransform);

                    foreach (GeometryNode occluderNode in occluderGroup)
                        if (renderGroups[occluderNode.GroupID] && occluderNode.Model != null
                            && occluderNode.Model.Enabled && occluderNode.ShouldRender &&
                            IsWithinViewFrustum(occluderNode.BoundingVolume))
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
                    // drawing the left eye view 
                    if(!(cameraNode.Stereo && renderLeftView))
                        occluderNode.ShouldRender = false;
                }
            }

            // Now turn off the depth buffer to overwrite the scene with background image
            if (occluderGroup.Count > 0 || showCameraImage || (backgroundTexture != null))
                State.Device.RenderState.DepthBufferEnable = false;

            if (showCameraImage)
            {
                bool secondImage = false;
                // If not doing stereo video overlay
                if (leftEyeVideoID >= 0 && rightEyeVideoID >= 0)
                {
                    if (!renderLeftView)
                    {
                        // If right eye video is not the default overlaid video, then
                        // we use the additional image
                        if (rightEyeVideoID != leftEyeVideoID)
                        {
                            secondImage = true;
                        }
                    }
                }

                while (copyingVideoImage) { }
                renderingVideoTexture = true;
                spriteBatch.Begin();
                if (secondImage)
                {
                    IVideoCapture capDev = videoCaptures[actualRightEyeVideoID];
                    spriteBatch.Draw(videoTextures[1], new Rectangle(0, 0, State.Width, State.Height),
                        new Rectangle(0, 0, capDev.Width, capDev.Height),
                        videoBackgroundColor, 0, Vector2.Zero, capDev.RenderFormat, 0);
                }
                else
                {
                    IVideoCapture capDev = videoCaptures[actualLeftEyeVideoID];
                    spriteBatch.Draw(videoTextures[0], new Rectangle(0, 0, State.Width, State.Height),
                        new Rectangle(0, 0, capDev.Width, capDev.Height),
                        videoBackgroundColor, 0, Vector2.Zero, capDev.RenderFormat, 0);
                }

                spriteBatch.End();
                renderingVideoTexture = false;
            }
            else if (backgroundTexture != null)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(BackgroundTexture,
                    new Rectangle(0, 0, State.Width, State.Height), videoBackgroundColor);
                spriteBatch.End();
            }

            // Now turn on the depth buffer back for normal rendering
            if (occluderGroup.Count > 0 || showCameraImage || (backgroundTexture != null))
                State.Device.RenderState.DepthBufferEnable = true;

            State.Restore3DSettings();
            State.AlphaBlendingEnabled = true;

            // First render all of the opaque geometries
            RenderGroup(opaqueGroup);

            // Before rendering tranparent objects, we need to sort them back to front
            if (needTransparencySort)
            {
                if(transparencySortOrder != null)
                    transparentGroup.Sort(transparencySortOrder);
                needTransparencySort = false;
            }

            // Then render all of the geometries with transparent material
            RenderGroup(transparentGroup);

            // Render any geometries that ignore the depth buffer
            State.Device.RenderState.DepthBufferEnable = false;
            RenderGroup(ignoreDepthGroup);
            State.Device.RenderState.DepthBufferEnable = true;
            ignoreDepthGroup.Clear();

            needsToUpdateGlobalLighting = false;

            // Finally render all of the particle effects
            foreach (ParticleNode particle in renderedEffects)
            {
                if (particle.ShouldRender)
                {
                    particle.Render();

                    // If it's stereo rendering, then we want to set this back to false only after
                    // drawing the left eye view 
                    if (!(cameraNode.Stereo && renderLeftView))
                        particle.ShouldRender = false;
                }
            }

            State.AlphaBlendingEnabled = false;

            // Show shadows we calculated above
            try
            {
                if(enableShadowMapping)
                    State.ShadowShader.ShowShadows();
            }
            catch (Exception exp) { }

            uiRenderer.TriangleCount = triangleCount;
        }

        protected virtual void RenderGroup(List<GeometryNode> group)
        {
            bool isIgnoreGroup = (group == ignoreDepthGroup);
            foreach (GeometryNode node in group)
            {
                if (node.IgnoreDepth && !isIgnoreGroup)
                {
                    ignoreDepthGroup.Add(node);
                    continue;
                }

                if (renderGroups[node.GroupID])
                {
                    if (node.Model != null && node.Model.Enabled && node.ShouldRender &&
                        IsWithinViewFrustum(node.BoundingVolume) && node.Material.Diffuse.W > 0)
                    {
                        triangleCount += node.Model.TriangleCount;

                        if (enableLighting && node.Model.UseLighting)
                        {
                            if (needsToUpdateGlobalLighting || node.NeedsToUpdateLocalLights)
                            {
                                node.Model.Shader.SetParameters(globalLights, node.LocalLights);

                                foreach (IShader shader in node.Model.AfterEffectShaders)
                                    shader.SetParameters(globalLights, node.LocalLights);

                                node.NeedsToUpdateLocalLights = false;
                            }

                            if (node.Model.Shader is SimpleEffectShader)
                                ((SimpleEffectShader)node.Model.Shader).PreferPerPixelLighting =
                                    preferPerPixelLighting;
                        }
                        else
                        {
                            node.Model.Shader.SetParameters(emptyLightList, emptyLightList);
                            // this is needed for when UseLighting is set back to true
                            node.NeedsToUpdateLocalLights = true;
                        }

                        if (environment != null)
                            node.Model.Shader.SetParameters(environment);
                        node.Model.Shader.SetParameters(cameraNode);

                        foreach (IShader shader in node.Model.AfterEffectShaders)
                        {
                            if (environment != null)
                                shader.SetParameters(environment);
                            shader.SetParameters(cameraNode);
                        }

                        tmpMat1 = node.WorldTransformation;
                        if (!node.MarkerTransform.Equals(Matrix.Identity))
                        {
                            tmpMat2 = node.MarkerTransform;
                            Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);
                            node.Model.Render(tmpMat3, node.Material);
                        }
                        else
                            node.Model.Render(tmpMat1, node.Material);

                        // If it's stereo rendering, then we want to set this back to false only after
                        // drawing the left eye view 
                        if (!(cameraNode.Stereo && renderLeftView))
                            node.ShouldRender = false;

                        if (renderAabb && node.AddToPhysicsEngine && (physicsEngine != null))
                            AddAabbLine(node.MarkerTransform, physicsEngine.GetAxisAlignedBoundingBox(node.Physics));

                        if (renderCollisionMesh && node.AddToPhysicsEngine && (physicsEngine != null))
                            AddColMeshLine(node.MarkerTransform, physicsEngine.GetCollisionMesh(node.Physics));
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the sizes of the buffered video image holders. 
        /// </summary>
        /// <param name="id">Either 0 (left eye) or 1 (right eye). Should be 0 if mono.</param>
        /// <param name="videoDevice"></param>
        protected virtual void InitializeVideoImageSize(int id, IVideoCapture videoDevice)
        {
            int imageSize = videoDevice.Width * videoDevice.Height;
            if ((bufferedVideoImages[id][0] == null) || (imageSize != bufferedVideoImages[id][0].Length))
            {
                for (int i = 0; i < VIDEO_BUFFER_SIZE; i++)
                    bufferedVideoImages[id][i] = new int[imageSize];

                videoTextures[id] = new Texture2D(State.Device, videoDevice.Width, videoDevice.Height,
                    1, TextureUsage.None, SurfaceFormat.Bgr32);
            }
        }

        /// <summary>
        /// Initilizes the sizes of the bufferd video image pointers.
        /// </summary>
        /// <param name="videoDevice"></param>
        protected virtual void InitializeVideoPointerSize(IVideoCapture videoDevice)
        {
            int imageSize = videoDevice.Width * videoDevice.Height;
            // if IResizer is set, then we'll use the size of the resized image
            // Note that the ScalingFactor is multiplied twice; one for width, and the other for height
            if (videoDevice.MarkerTrackingImageResizer != null)
                imageSize = (int)(imageSize * videoDevice.MarkerTrackingImageResizer.ScalingFactor *
                    videoDevice.MarkerTrackingImageResizer.ScalingFactor);

            if (!videoDevice.GrayScale)
            {
                switch (videoDevice.Format)
                {
                    case ImageFormat.GRAYSCALE_8: break; // Nothing to do 
                    case ImageFormat.R5G6B5_16: imageSize *= 2; break;
                    case ImageFormat.B8G8R8_24:
                    case ImageFormat.R8G8B8_24: imageSize *= 3; break;
                    case ImageFormat.R8G8B8A8_32:
                    case ImageFormat.B8G8R8A8_32:
                    case ImageFormat.A8B8G8R8_32: imageSize *= 4; break;
                }
            }

            if (prevPointerSize == 0)
            {
                for (int i = 0; i < bufferedVideoPointers.Length; i++)
                    bufferedVideoPointers[i] = Marshal.AllocHGlobal(imageSize);
            }
            else if (prevPointerSize != imageSize)
            {
                for (int i = 0; i < bufferedVideoPointers.Length; i++)
                {
                    Marshal.FreeHGlobal(bufferedVideoPointers[i]);
                    bufferedVideoPointers[i] = Marshal.AllocHGlobal(imageSize);
                }
            }

            prevPointerSize = imageSize;
        }

        /// <summary>
        /// Updates the optical marker tracker as well as the video image
        /// </summary>
        protected virtual void UpdateTracker()
        {
            if (isMarkerTrackingThreaded)
            {
                while (true)
                {
                    if (readyToUpdateTracker && (videoCaptures.Count > 0))
                    {
                        // Synchronize the frame update with tracker update
                        while (trackerUpdateCount > frameUpdateCount)
                        {
                            Thread.Sleep(10);
                        }
                        UpdateTrackerAndImage();
                        trackerUpdateCount++;
                    }
                }
            }
            else if(videoCaptures.Count > 0)
                UpdateTrackerAndImage();
        }

        /// <summary>
        /// Updates the optical marker tracker as well as the video image
        /// </summary>
        protected void UpdateTrackerAndImage()
        {
            if(freezeVideo)
                return;

            // Wait for video ID to change before updating the tracker and image
            while (waitForVideoIDChange) { }
            waitForTrackerUpdate = true;

            // If a static image is used and the image is already processed, then we don't
            // need to process it again
            if (!((videoCaptures[actualTrackerVideoID] is NullCapture) &&
                ((NullCapture)videoCaptures[actualTrackerVideoID]).IsImageAlreadyProcessed))
            {
                bool processLeftImageData = NeedsImageData;
                bool processLeftImagePtr = NeedsImagePtr;
                bool processRightImageData = (cameraNode.Stereo) ? NeedsImageData : false;
                bool processRightImagePtr = (cameraNode.Stereo) ? NeedsImagePtr : false;
                bool processSeparateImagePtr = false;
                bool passToMarkerTracker = false;

                if (showCameraImage)
                {
                    processLeftImageData = true;

                    if (markerTracker != null)
                    {
                        passToMarkerTracker = true;

                        if (cameraNode.Stereo)
                        {
                            if (leftEyeVideoID == trackerVideoID)
                            {
                                processLeftImagePtr = true;

                                if (leftEyeVideoID != rightEyeVideoID)
                                    processRightImageData = true;
                            }
                            else if (rightEyeVideoID == trackerVideoID)
                            {
                                processRightImageData = true;
                                processRightImagePtr = true;
                            }
                            else
                            {
                                if (leftEyeVideoID != rightEyeVideoID)
                                    processRightImageData = true;

                                processSeparateImagePtr = true;
                            }
                        }
                        else
                        {
                            if (leftEyeVideoID == trackerVideoID)
                                processLeftImagePtr = true;
                            else
                                processSeparateImagePtr = true;
                        }
                    }
                    else
                    {
                        if (cameraNode.Stereo && (leftEyeVideoID != rightEyeVideoID))
                            processRightImageData = true;
                    }
                }
                else if (trackMarkers && (markerTracker != null))
                {
                    passToMarkerTracker = true;
                    processSeparateImagePtr = true;
                }

                if (processLeftImageData || processLeftImagePtr)
                {
                    IntPtr leftImagePtr = (processLeftImagePtr) ? 
                        bufferedVideoPointers[curVideoBufferIndex] : nullPtr;
                    videoCaptures[actualLeftEyeVideoID].GetImageTexture(
                        ((processLeftImageData) ? bufferedVideoImages[0][curVideoBufferIndex] : null),
                        ref leftImagePtr);

                    if (processLeftImageData)
                        SetTextureData(0);
                }

                if (processRightImageData || processRightImagePtr)
                {
                    IntPtr rightImagePtr = (processRightImagePtr) ?
                        bufferedVideoPointers[curVideoBufferIndex] : nullPtr;
                    videoCaptures[actualRightEyeVideoID].GetImageTexture(
                        ((processRightImageData) ? bufferedVideoImages[1][curVideoBufferIndex] : null),
                        ref rightImagePtr);

                    if (processRightImageData)
                        SetTextureData(1);
                }

                if (processSeparateImagePtr)
                    videoCaptures[actualTrackerVideoID].GetImageTexture(null,
                        ref bufferedVideoPointers[curVideoBufferIndex]);

                if(passToMarkerTracker)
                    markerTracker.ProcessImage(videoCaptures[actualTrackerVideoID],
                        bufferedVideoPointers[curVideoBufferIndex]);

                if (videoCaptures[actualTrackerVideoID] is NullCapture)
                    ((NullCapture)videoCaptures[actualTrackerVideoID]).IsImageAlreadyProcessed = true;
            }

            prevVideoBufferIndex = curVideoBufferIndex;
            curVideoBufferIndex = (curVideoBufferIndex + 1) % VIDEO_BUFFER_SIZE;

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

        /// <summary>
        /// Sets the video texture data.
        /// </summary>
        /// <param name="id"></param>
        protected void SetTextureData(int id)
        {
            if(videoTextures[id].IsDisposed)
                videoTextures[id] = new Texture2D(State.Device, videoTextures[id].Width, videoTextures[id].Height,
                    1, TextureUsage.None, SurfaceFormat.Bgr32);

            while (renderingVideoTexture) { }
            copyingVideoImage = true;
            try
            {
                videoTextures[id].SetData<int>(bufferedVideoImages[id][curVideoBufferIndex]);
            }
            catch (Exception exp)
            {
                State.Device.Textures[0] = null;
                videoTextures[id].SetData<int>(bufferedVideoImages[id][curVideoBufferIndex]);
            }
            copyingVideoImage = false;
        }

        /// <summary>
        /// Renders the axis-aligned bounding box obtained from the physics engine for each
        /// GeometryNode added to the physics engine for debugging.
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="aabb"></param>
        protected void AddAabbLine(Matrix worldTransform, BoundingBox aabb)
        {
            Vector3 min = aabb.Min;
            Vector3 max = aabb.Max;

            Vector3 minMaxZ = Vector3Helper.Get(min.X, min.Y, max.Z);
            Vector3 minMaxX = Vector3Helper.Get(max.X, min.Y, min.Z);
            Vector3 minMaxY = Vector3Helper.Get(min.X, max.Y, min.Z);
            Vector3 maxMinX = Vector3Helper.Get(min.X, max.Y, max.Z);
            Vector3 maxMinY = Vector3Helper.Get(max.X, min.Y, max.Z);
            Vector3 maxMinZ = Vector3Helper.Get(max.X, max.Y, min.Z);

            if (!worldTransform.Equals(Matrix.Identity))
            {
                tmpMat1 = Matrix.CreateTranslation(minMaxX);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                minMaxX = tmpMat1.Translation;

                tmpMat1 = Matrix.CreateTranslation(minMaxY);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                minMaxY = tmpMat1.Translation;

                tmpMat1 = Matrix.CreateTranslation(minMaxZ);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                minMaxZ = tmpMat1.Translation;

                tmpMat1 = Matrix.CreateTranslation(maxMinX);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                maxMinX = tmpMat1.Translation;

                tmpMat1 = Matrix.CreateTranslation(maxMinY);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                maxMinY = tmpMat1.Translation;

                tmpMat1 = Matrix.CreateTranslation(maxMinZ);
                Matrix.Multiply(ref tmpMat1, ref worldTransform, out tmpMat1);
                maxMinZ = tmpMat1.Translation;
            }

            State.LineManager.AddLine(min, minMaxX, aabbColor);
            State.LineManager.AddLine(min, minMaxY, aabbColor);
            State.LineManager.AddLine(min, minMaxZ, aabbColor);
            State.LineManager.AddLine(max, maxMinX, aabbColor);
            State.LineManager.AddLine(max, maxMinY, aabbColor);
            State.LineManager.AddLine(max, maxMinZ, aabbColor);
            State.LineManager.AddLine(minMaxY, maxMinX, aabbColor);
            State.LineManager.AddLine(minMaxY, maxMinZ, aabbColor);
            State.LineManager.AddLine(minMaxZ, maxMinX, aabbColor);
            State.LineManager.AddLine(minMaxZ, maxMinY, aabbColor);
            State.LineManager.AddLine(minMaxX, maxMinY, aabbColor);
            State.LineManager.AddLine(minMaxX, maxMinZ, aabbColor);
        }

        /// <summary>
        /// Renders the detailed collision mesh obtained from the physics engine for each
        /// GeometryNode added to the physics engine for debugging.
        /// </summary>
        /// <param name="worldTransform"></param>
        /// <param name="collisionMesh"></param>
        protected void AddColMeshLine(Matrix worldTransform, List<List<Vector3>> collisionMesh)
        {
            if (collisionMesh == null)
                return;

            int count = 0;
            int indiceCount = 0;
            foreach(List<Vector3> polygonVerts in collisionMesh)
                count += polygonVerts.Count;

            Vector3[] verts = new Vector3[count];
            count = 0;
            for (int i = 0; i < collisionMesh.Count; i++)
                for (int j = 0; j < collisionMesh[i].Count; j++)
                    verts[count++] = collisionMesh[i][j];

            count = 0;
            int initCount = 0;
            for (int i = 0; i < collisionMesh.Count; i++)
            {
                initCount = count;

                for (int j = 0; j < collisionMesh[i].Count - 1; j++, count++)
                {
                    State.LineManager.AddLine(new LineManager3D.Line(
                        verts[count], verts[count + 1], cmeshColor));
                }

                State.LineManager.AddLine(new LineManager3D.Line(
                        verts[count], verts[initCount], cmeshColor));
                count++;
            }
        }

        /// <summary>
        /// Updates the physics simulatin.
        /// </summary>
        protected void UpdatePhysicsSimulation()
        {
            physicsEngine.Update(physicsElapsedTime);
            physicsElapsedTime = 0;
        }

        /// <summary>
        /// Recursively saves a scene graph node into an XML document.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="node"></param>
        /// <param name="xmlDoc"></param>
        protected static void RecursivelySave(XmlElement xmlNode, Node node, XmlDocument xmlDoc)
        {
            XmlElement parentXmlNode = node.Save(xmlDoc);
            xmlNode.AppendChild(parentXmlNode);

            if (node is BranchNode && ((BranchNode)node).Children.Count > 0)
            {
                XmlElement childXmlNode = xmlDoc.CreateElement("Children");
                parentXmlNode.AppendChild(childXmlNode);

                foreach (Node child in ((BranchNode)node).Children)
                    RecursivelySave(childXmlNode, child, xmlDoc);
            }
        }

        /// <summary>
        /// Recursively loads each scene graph node from XML elements.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        protected static Node RecursivelyLoad(XmlElement xmlNode)
        {
            Type nodeType = Type.GetType(xmlNode.Name);
            Node node = (Node)Activator.CreateInstance(nodeType);
            node.Load(xmlNode);

            foreach (XmlElement childXmlNode in xmlNode.ChildNodes)
            {
                if (childXmlNode.Name.Equals("Children"))
                {
                    BranchNode branch = (BranchNode)node;
                    foreach (XmlElement child in childXmlNode.ChildNodes)
                    {
                        Node childNode = RecursivelyLoad(child);
                        branch.AddChild(childNode);
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Recursively loads a scene graph node from an XML document.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <returns></returns>
        protected Node RecursivelyLoadForSceneGraph(XmlElement xmlNode)
        {
            Type nodeType = Type.GetType(xmlNode.Name);
            Node node = (Node)Activator.CreateInstance(nodeType);
            node.Load(xmlNode);
            node.SceneGraph = this;

            if((node is CameraNode) && node.Name.Equals(curCamNodeName))
                cameraNode = (CameraNode)node;

            foreach (XmlElement childXmlNode in xmlNode.ChildNodes)
            {
                if (childXmlNode.Name.Equals("Children"))
                {
                    BranchNode branch = (BranchNode)node;
                    foreach (XmlElement child in childXmlNode.ChildNodes)
                    {
                        Node childNode = RecursivelyLoadForSceneGraph(child);
                        branch.AddChild(childNode);
                    }
                }
            }

            return node;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a Node object added to this scene graph with its node name.
        /// Null is returned if the name does not exist.
        /// </summary>
        /// <remarks>
        /// If you try to access a node right after adding it to the scene graph, this method will
        /// throw an exception since the node won't be added to the node list until the scene graph
        /// is processed after Draw(...) method is called.
        /// </remarks>
        /// <param name="name">The name of the node you're looking for</param>
        /// <returns></returns>
        public virtual Node GetNode(String name)
        {
            return nodeTable[name];
        }

        /// <summary>
        /// Indicates whether a node with the specified name exists in the current scene graph.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual bool HasNode(String name)
        {
            return nodeTable.ContainsKey(name);
        }

        /// <summary>
        /// Enables or disables the rendering of a certain group with the specified groupID.
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="enable"></param>
        public virtual void EnableRenderGroups(int groupID, bool enable)
        {
            if (renderGroups.ContainsKey(groupID))
                renderGroups[groupID] = enable;
        }

        /// <summary>
        /// Adds a video streaming decoder implementation for background rendering and 
        /// marker tracking.
        /// </summary>
        /// <remarks>
        /// The video capture device should be initialized before it can be added.
        /// </remarks>
        /// <param name="device">A video streaming decoder implementation</param>
        /// <exception cref="GoblinException">If the device is not initialized</exception>
        public virtual void AddVideoCaptureDevice(IVideoCapture device)
        {
            if (device == null)
                return;

            if (!device.Initialized)
                throw new GoblinException("You should initialize the video capture device first " +
                    "before you add it");

            if(!videoCaptures.Contains(device))
                videoCaptures.Add(device);

            videoIDs.Add(device.VideoDeviceID, videoIDs.Count);

            LeftEyeVideoID = device.VideoDeviceID;
            TrackerVideoID = device.VideoDeviceID;
        }

        /// <summary>
        /// Captures the current frame/back buffer and stores it in a file with the specified format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public virtual void CaptureScene(String filename, ImageFileFormat format)
        {
            if(screen == null)
                screen = new ResolveTexture2D(State.Device, State.Width, State.Height,
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
        public virtual void RenderScene()
        {
            if (cameraNode.Stereo)
            {
                if (renderLeftView)
                {
                    State.ViewMatrix = cameraNode.LeftCompoundViewMatrix;
                    State.ProjectionMatrix = ((StereoCamera)cameraNode.Camera).LeftProjection;
                }
                else
                {
                    State.ViewMatrix = cameraNode.RightCompoundViewMatrix;
                    State.ProjectionMatrix = ((StereoCamera)cameraNode.Camera).RightProjection;
                }
            }
            else
            {
                State.ViewMatrix = cameraNode.CompoundViewMatrix;
                State.ProjectionMatrix = cameraNode.Camera.Projection;
            }

            State.CameraTransform = cameraNode.WorldTransformation;
            State.ViewProjectionMatrix = State.ViewMatrix * State.ProjectionMatrix;

            try
            {
                if (enableShadowMapping)
                    RenderShadows(globalLightSources);
            }
            catch (Exception) { }

            RenderSceneGraph(globalLightSources);

            State.LineManager.Render();

            if (renderBeforeUI != null)
                renderBeforeUI(renderLeftView);

            if (cameraNode.Stereo)
            {
                if (renderLeftView) 
                    uiRenderer.Draw(uiElapsedTime, false, false);
                else
                    uiRenderer.Draw(0, true, true);
            }
            else
                uiRenderer.Draw(uiElapsedTime, true, false);

            if (renderAfterUI != null)
                renderAfterUI(renderLeftView);

            if (cameraNode.Stereo)
                renderLeftView = !renderLeftView;
        }

        /// <summary>
        /// Saves the current scene graph structure into an XML file.
        /// </summary>
        /// <param name="filename">An XML file</param>
        public virtual void SaveSceneGraph(String filename)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);

            XmlElement xmlRootNode = xmlDoc.CreateElement("GoblinXNA");
            xmlDoc.AppendChild(xmlRootNode);

            XmlElement xmlCurCamNode = xmlDoc.CreateElement("CurrentCameraNode");
            xmlCurCamNode.InnerText = cameraNode.Name;
            xmlRootNode.AppendChild(xmlCurCamNode);

            XmlElement xmlSceneNode = xmlDoc.CreateElement("SceneGraph");
            xmlRootNode.AppendChild(xmlSceneNode);

            RecursivelySave(xmlSceneNode, rootNode, xmlDoc);

            try
            {
                xmlDoc.Save(filename);
            }
            catch (Exception exp)
            {
                throw new GoblinException("Failed to save the scene: " + filename);
            }
        }

        /// <summary>
        /// Loads a scene graph from an XML file.
        /// </summary>
        /// <param name="filename">An XML file</param>
        public virtual void LoadSceneGraph(String filename)
        {
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(filename);
            }
            catch (Exception exp)
            {
                throw new GoblinException(exp.Message);
            }

            // Before loading the scene graph, first remove all of the currently added nodes
            RecursivelyRemoveFromScene(rootNode);

            foreach(XmlNode xmlNode in xmlDoc.ChildNodes)
            {
                if (xmlNode is XmlElement)
                {
                    if (xmlNode.Name.Equals("GoblinXNA"))
                    {
                        foreach (XmlNode goblinXmlNode in xmlNode.ChildNodes)
                        {
                            if (goblinXmlNode.Name.Equals("SceneGraph"))
                                rootNode = (BranchNode)RecursivelyLoadForSceneGraph(
                                    (XmlElement)goblinXmlNode.FirstChild);
                            else if (goblinXmlNode.Name.Equals("CurrentCameraNode"))
                                curCamNodeName = ((XmlElement)goblinXmlNode).InnerText;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves a scene node including its children nodes into an XML file.
        /// </summary>
        /// <param name="filename">An XML file</param>
        public static void SaveNode(String filename, Node node)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlElement xmlNode = xmlDoc.CreateElement("Node");

            xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);
            xmlDoc.AppendChild(xmlNode);

            RecursivelySave(xmlNode, node, xmlDoc);

            try
            {
                xmlDoc.Save(filename);
            }
            catch (Exception exp)
            {
                throw new GoblinException("Failed to save the node: " + filename);
            }
        }

        /// <summary>
        /// Loads a scene graph node from an XML file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Node LoadNode(String filename)
        {
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                xmlDoc.Load(filename);
            }
            catch (Exception exp)
            {
                throw new GoblinException(exp.Message);
            }

            Node node = null;

            foreach (XmlNode xmlNode in xmlDoc.ChildNodes)
            {
                if ((xmlNode is XmlElement) && xmlNode.Name.Equals("Node"))
                    node = RecursivelyLoad((XmlElement)xmlNode.FirstChild);
            }

            return node;
        }

        /// <summary>
        /// Encodes a scene graph node into an XML format, and converts the strings to a byte array
        /// so that the node can be transferred over the network.
        /// </summary>
        /// <param name="node">A node to be encoded</param>
        /// <returns>Array of bytes of XML string that encodes the node information</returns>
        public static byte[] EncodeNode(Node node)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            XmlElement xmlNode = xmlDoc.CreateElement("Node");

            xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);
            xmlDoc.AppendChild(xmlNode);

            RecursivelySave(xmlNode, node, xmlDoc);

            try
            {
                StringWriter sw = new StringWriter();
                XmlTextWriter tw = new XmlTextWriter(sw);
                xmlDoc.WriteTo(tw);

                return ByteHelper.ConvertToByte(sw.ToString());
            }
            catch (Exception exp)
            {
                throw new GoblinException("Failed to save the node: " + node.Name);
            }
        }

        /// <summary>
        /// Decodes the XML string represented as byte array to a node. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Node DecodeNode(byte[] data)
        {
            String xmlString = ByteHelper.ConvertToString(data);

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xmlString);
            }
            catch (Exception exp)
            {
                throw new GoblinException(exp.Message);
            }

            Node node = null;

            foreach (XmlNode xmlNode in xmlDoc.ChildNodes)
            {
                if ((xmlNode is XmlElement) && xmlNode.Name.Equals("Node"))
                    node = RecursivelyLoad((XmlElement)xmlNode.FirstChild);
            }

            return node;
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
            if (State.EnableNetworking && (networkHandler != null))
                networkHandler.Update((float)gameTime.ElapsedGameTime.TotalMilliseconds);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (rootNode == null || cameraNode == null)
                return;

            isStarted = true;

            uiElapsedTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            if (isMarkerTrackingThreaded)
                readyToUpdateTracker = true;
            else
                UpdateTracker();

            bool updatePhysicsEngine = (physicsEngine != null);

            if (State.EnableNetworking && (networkHandler != null))
            {
                // If we're the server, then don't update the physics simulation until we get
                // connections from clients
                if (State.IsServer)
                {
                    if (networkHandler.NetworkServer.NumConnectedClients < State.NumberOfClientsToWait)
                        updatePhysicsEngine = false;
                }
            }

            if (updatePhysicsEngine)
            {
                physicsElapsedTime += (float)gameTime.ElapsedRealTime.TotalSeconds;
                if (isPhysicsThreaded)
                {
                    if (physicsThread == null || physicsThread.ThreadState != ThreadState.Running)
                    {
                        physicsThread = new Thread(UpdatePhysicsSimulation);
                        physicsThread.Start();
                    }
                }
                else
                    UpdatePhysicsSimulation();
            }

            PrepareSceneForRendering();
            frameUpdateCount++;

            // If the camera position changed, then we need to re-sort the transparency group
            if (!prevCameraTrans.Equals(cameraNode.WorldTransformation.Translation))
                needTransparencySort = true;

            prevCameraTrans = cameraNode.WorldTransformation.Translation;

            globalLightSources.Clear();
            foreach (LightNode lightNode in globalLights)
            {
                if (lightNode.LightSource.Enabled)
                {
                    LightSource light = new LightSource(lightNode.LightSource);
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

            foreach (LODNode lodNode in lodNodes)
                if (lodNode.AutoComputeLevelOfDetail)
                    lodNode.Update(cameraNode.WorldTransformation.Translation);

            RenderScene();
        }

        protected override void Dispose(bool disposing)
        {
            uiRenderer.Dispose();

            if (markerTrackingThread != null)
                markerTrackingThread.Abort();
            if (physicsThread != null)
                physicsThread.Abort();

            renderGroups.Clear();
            opaqueGroup.Clear();
            transparentGroup.Clear();
            renderedEffects.Clear();
            occluderGroup.Clear();
            markerUpdateList.Clear();
            lodNodes.Clear();

            if(rootNode != null)
                rootNode.Dispose();

            rootNode = null;
            cameraNode = null;
            globalLights.Clear();

            if(physicsEngine != null)
                physicsEngine.Dispose();
            if (networkHandler != null)
                networkHandler.Dispose();

            if (markerTracker != null)
                markerTracker.Dispose();

            foreach (IVideoCapture videoCap in videoCaptures)
                videoCap.Dispose();

            Sound.Dispose();
            InputMapper.Instance.Dispose();

            for (int i = 0; i < bufferedVideoPointers.Length; i++)
                if (bufferedVideoPointers[i] != IntPtr.Zero)
                {
                    try
                    {
                        Marshal.FreeHGlobal(bufferedVideoPointers[i]);
                    }
                    catch (Exception) { }
                }

            base.Dispose(disposing);
        }

        #endregion
    }
}
