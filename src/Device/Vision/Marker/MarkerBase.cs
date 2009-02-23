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
        /// The marker tracker object
        /// </summary>
        private IMarkerTracker m_kTracker;
        /// <summary>
        /// The video capture object
        /// </summary>
        private List<IVideoCapture> m_kVideoCapture;

        private int activeDevice;

        #region Rendering parameters
        private bool m_bRenderInitialized;
        private Texture2D videoTexture;
        private CameraNode markerCameraNode;

        private int prevWidth;
        private int prevHeight;
        #endregion
        #endregion

        #region Singleton Constructor

        private MarkerBase()
        {
            m_kTracker = null;
            m_kVideoCapture = new List<IVideoCapture>();
            m_bRenderInitialized = false;
            activeDevice = -1;

            markerCameraNode = null;

            prevWidth = 0;
            prevHeight = 0;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the optical marker tracker instance.
        /// </summary>
        /// <returns>The marker tracker class</returns>
        public IMarkerTracker Tracker
        {
            get { return m_kTracker; }
            set { m_kTracker = value; }
        }

        /// <summary>
        /// Gets or sets a list of video capture instances.
        /// </summary>
        /// <returns>The video capture class</returns>
        public List<IVideoCapture> VideoCaptures
        {
            get { return m_kVideoCapture; }
            set { m_kVideoCapture = value; }
        }

        /// <summary>
        /// Gets or sets the active capture device used for rendering the background image.
        /// </summary>
        internal int ActiveCaptureDevice
        {
            get { return activeDevice; }
            set 
            { 
                activeDevice = value;
                if (activeDevice >= 0)
                {
                    // if the new active capture device has different width or height
                    // from the previosly used capture device, then we need to reset
                    // the texture configuration
                    if ((m_kVideoCapture[activeDevice].Width != prevWidth) ||
                        (m_kVideoCapture[activeDevice].Height != prevHeight))
                        InitRendering();
                }
            }
        }

        public Node CameraNode
        {
            get { return markerCameraNode; }
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
        /// Initializes the background video image rendering routine.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public void InitRendering()
        {
            if (!State.Initialized)
                throw new GoblinException("GoblinXNA.GoblinSetting.InitGoblin(...) method needs to be called " +
                    "before you can call this method");

            int width = 0, height = 0;
            if (activeDevice < 0)
            {
                if (m_kTracker == null)
                    throw new GoblinException("marker tracker is null. Can't make texture for static image.");

                width = m_kTracker.ImageWidth;
                height = m_kTracker.ImageHeight;
            }
            else
            {
                if (activeDevice > m_kVideoCapture.Count)
                    throw new GoblinException(activeDevice + " is out of range");

                width = m_kVideoCapture[activeDevice].Width;
                height = m_kVideoCapture[activeDevice].Height;
            }

            prevWidth = width;
            prevHeight = height;

            videoTexture = new Texture2D(State.Device, width, height, 1, TextureUsage.None, 
                SurfaceFormat.Bgr32);

            m_bRenderInitialized = true;
        }

        /// <summary>
        /// Initialize the camera setting used for marker tracking.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public void InitCameraNode()
        {
            if (m_kTracker == null)
                throw new GoblinException("marker tracker is null, can not create a camera node");

            Camera markerCamera = new Camera();
            PrimitiveMesh mesh = new PrimitiveMesh();

            markerCamera.View = Matrix.CreateLookAt(new Vector3(0, 0, 0), new Vector3(0, 0, -1),
                new Vector3(0, 1, 0));

            markerCamera.Projection = m_kTracker.CameraProjection;

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
                InitRendering();

            try
            {
                videoTexture.SetData(imageData);
            }
            catch (Exception exp)
            {
                InitRendering();
                videoTexture.SetData(imageData);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            foreach (IVideoCapture captureDevice in m_kVideoCapture)
                captureDevice.Dispose();
            if(m_kTracker != null)
                m_kTracker.Dispose();
        }

        #endregion
    }
}
