/************************************************************************************ 
 * Copyright (c) 2008, Columbia University
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
 *          Mike Sorvillo
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Device.Capture;
using GoblinXNA.SceneGraph;
using GoblinXNA.Graphics;

namespace GoblinXNA.Device.Vision.Marker
{
    /// <summary>
    /// The base class for optical marker trackers. Use this class to access the MarkerTracker
    /// and VideoCapture instances instead of directly instantiating them.
    /// </summary>
    public sealed class MarkerBase : IDisposable
    {
        #region Member Variables
        /// <summary>
        /// The marker base
        /// </summary>
        public static readonly MarkerBase Base = new MarkerBase();
        /// <summary>
        /// The camera diagnostic object
        /// </summary>
        private CameraDiagnostic m_kDiagnostics;
        /// <summary>
        /// The marker tracker object
        /// </summary>
        private MarkerTracker m_kTracker;
        /// <summary>
        /// The video capture object
        /// </summary>
        private List<VideoCapture> m_kVideoCapture;
        /// <summary>
        /// Indicates whether the person is useing a camera
        /// </summary>
        private bool m_bUseCamera;

        #region Rendering parameters
        private bool m_bRenderInitialized;
        private Texture2D videoTexture;
        private CameraNode markerCameraNode;
        #endregion
        #endregion

        #region Singleton Constructor

        private MarkerBase()
        {
            m_kTracker = null;
            m_kVideoCapture = null;
            m_kDiagnostics = null;
            m_bUseCamera = false;
            m_bRenderInitialized = false;

            markerCameraNode = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the diagnostic class
        /// </summary>
        /// <returns>The camera diagnostic class</returns>
        internal CameraDiagnostic Diagnostics
        {
            get { return m_kDiagnostics; }
        }

        /// <summary>
        /// Gets the optical marker tracker instance.
        /// </summary>
        /// <returns>The marker tracker class</returns>
        public MarkerTracker Tracker
        {
            get { return m_kTracker; }
        }

        /// <summary>
        /// Gets a list of video capture instances.
        /// </summary>
        /// <returns>The video capture class</returns>
        public List<VideoCapture> VideoCaptures
        {
            get { return m_kVideoCapture; }
        }

        /// <summary>
        /// Gets or sets whether to use camera's live video images for tracking
        /// instead of using a static image.
        /// </summary>
        /// <returns>The camera flag</returns>
        public bool UseCamera
        {
            set { m_bUseCamera = value; }
            get { return m_bUseCamera; }
        }

        public Node CameraNode
        {
            get
            {
                if (!m_bRenderInitialized)
                    throw new Exception("InitRendering() has to be called " +
                        "before you can get MarkerNode");

                return markerCameraNode;
            }
        }

        /// <summary>
        /// Gets the texture of the video image.
        /// </summary>
        public Texture2D BackgroundTexture
        {
            get { return videoTexture; }
        }

        /// <summary>
        /// Gets whether the background video image rendering routine is initialized.
        /// </summary>
        public bool RenderInitialized
        {
            get { return m_bRenderInitialized; }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the marker tracking module with 1 video capture device.
        /// </summary>
        public void InitModules()
        {
            InitModules(1);
        }

        /// <summary>
        /// Initializes the marker tracking module with the specified number of video
        /// capture devices.
        /// </summary>
        /// <param name="numOfCaptureDeviceToUse">The number of video capture devices to use</param>
        public void InitModules(int numOfCaptureDeviceToUse)
        {
            //create tracker and init it
            m_kTracker = new MarkerTracker();

            //create video capture and init it
            if (numOfCaptureDeviceToUse > 0)
            {
                m_kVideoCapture = new List<VideoCapture>();
                for (int i = 0; i < numOfCaptureDeviceToUse; i++)
                    m_kVideoCapture.Add(new VideoCapture());
            }

            //create camera diagnostics and init it
            m_kDiagnostics = new CameraDiagnostic();
            m_kDiagnostics.Init();
        }

        /// <summary>
        /// Initializes the background video image rendering routine.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public void InitRendering()
        {
            if (!State.Initialized)
                throw new GoblinException("GoblinXNA.GoblinSetting.InitGoblin(...) method needs to be called " +
                    "before you can call this method");

            videoTexture = new Texture2D(State.Device, m_kVideoCapture[0].Width,
                m_kVideoCapture[0].Height, 1, TextureUsage.None, SurfaceFormat.Bgr32);

            m_bRenderInitialized = true;
        }

        /// <summary>
        /// Initialize the camera setting used for marker tracking.
        /// </summary>
        public void InitCameraNode()
        {
            Camera markerCamera = new Camera();
            PrimitiveMesh mesh = new PrimitiveMesh();

            markerCamera.View = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1),
                new Vector3(0, 1, 0));

            double camera_dRight = (double)m_kTracker.CameraWidth / (double)(2.0 * m_kTracker.CameraFx);
            double camera_dTop = (double)m_kTracker.CameraHeight / (double)(2.0 * m_kTracker.CameraFy);

            camera_dRight *= 1024;
            camera_dTop *= 1024;

            markerCamera.FieldOfViewY = (float)Math.Atan(camera_dRight / 1380) * 2;
            markerCamera.AspectRatio = (float)(camera_dRight / camera_dTop);
            markerCamera.ZNearPlane = 1.0f;
            markerCamera.ZFarPlane = 102500;

            markerCameraNode = new CameraNode("MarkerCameraNode", markerCamera);
        }

        /// <summary>
        /// Update the background video image with the given texture data.
        /// </summary>
        /// <param name="imageData">The texture data in 32-bit BGR integer format (see
        /// Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgr32 format for details)</param>
        public void UpdateRendering(int[] imageData)
        {
            if (!m_bRenderInitialized || (imageData == null))
                return;

            if (videoTexture.IsDisposed)
            {
                videoTexture = new Texture2D(State.Device, m_kVideoCapture[0].Width,
                    m_kVideoCapture[0].Height, 1, TextureUsage.None, SurfaceFormat.Bgr32);
            }

            videoTexture.SetData(imageData);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            foreach (VideoCapture captureDevice in m_kVideoCapture)
                captureDevice.Dispose();
            m_kTracker.Dispose();
        }

        #endregion
    }
}
