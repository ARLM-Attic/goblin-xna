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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Helpers;

namespace GoblinXNA.Device.iWear
{
    /// <summary>
    /// A 6DOF input device class that supports VUZIX's iWear VS920 device which is a stereoscopic display
    /// with embedded orientation tracker. 
    /// </summary>
    public class iWearTracker : InputDevice_6DOF
    {
        #region Member Fields

        private String identifier;
        private bool isAvailable;
        private bool stereoAvailable;
        private Quaternion rotation;
        private float yaw;
        private float pitch;
        private float roll;

        private OcclusionQuery g_QueryGPU;
        private IntPtr stereoHandle;
        private int windowBottomLine;

        private static iWearTracker tracker;

        #endregion

        #region Constructor

        /// <summary>
        /// A private constructor.
        /// </summary>
        private iWearTracker()
        {
            identifier = "iWearTracker";
            isAvailable = false;
            stereoAvailable = false;
            rotation = Quaternion.Identity;
            yaw = 0;
            pitch = 0;
            roll = 0;

            stereoHandle = ((IntPtr)(-1));
            // Setup a query, to provide GPU syncing method.
            g_QueryGPU = new OcclusionQuery(State.Device);
            windowBottomLine = 0;
        }

        #endregion

        #region Public Properties

        public String Identifier
        {
            get { return identifier; }
            set { identifier = value; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
        }

        /// <summary>
        /// Gets whether the stereoscopic view is available.
        /// </summary>
        public bool IsStereoAvailable
        {
            get { return stereoAvailable; }
        }

        /// <summary>
        /// Gets the yaw (in radians) updated by the device's orientation tracker. 
        /// </summary>
        public float Yaw
        {
            get { return yaw; }
        }

        /// <summary>
        /// Gets the pitch (in radians) updated by the device's orientation tracker. 
        /// </summary>
        public float Pitch
        {
            get { return pitch; }
        }

        /// <summary>
        /// Gets the roll (in radians) updated by the device's orientation tracker. 
        /// </summary>
        public float Roll
        {
            get { return roll; }
        }

        /// <summary>
        /// Sets whether to enable internal filtering by the tracker. The internal filtering is
        /// disabled by default.
        /// </summary>
        public bool EnableFiltering
        {
            set
            {
                try
                {
                    iWearDllBridge.IWRSetFilterState(value);
                }
                catch
                {
                    Log.Write("Filtering is not available. Could be pre 2.4 driver install.", 
                        Log.LogLevel.Error);
                }
            }
        }

        /// <summary>
        /// Sets whether to enable stereoscopic view. If stereo is available, then stereoscopic view
        /// is enabled by default.
        /// </summary>
        public bool EnableStereo
        {
            set
            {
                if (!stereoAvailable)
                    return;

                iWearDllBridge.IWRSetStereoEnabled(stereoHandle, value);
            }
        }

        /// <summary>
        /// Gets the rotation updated by the device's orientation tracker. 
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
            }
        }

        public Matrix WorldTransformation
        {
            get
            {
                return Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
            }
        }

        /// <summary>
        /// Gets the instance of iWearTracker.
        /// </summary>
        public static iWearTracker Instance
        {
            get
            {
                if (tracker == null)
                    tracker = new iWearTracker();

                return tracker;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the iWear tracker.
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Acquire tracking interface
                iWearDllBridge.IWROpenTracker();
                isAvailable = true;
            }
            catch (Exception)
            {
                Log.Write("Unable to open iWear Drivers...Check VR920 Driver installation.", Log.LogLevel.Error);
                return;
            }

            // Acquire stereoscopic handle
            stereoHandle = iWearDllBridge.IWROpenStereo();
            if (stereoHandle == ((IntPtr)(-1)))
            {
                Log.Write("Unable to obtain stereo handle. Please ensure your VR920 is connected, and " +
                    "that your firmware supports stereoscopy.", Log.LogLevel.Error);
            }
            else
            {
                stereoAvailable = true;
                iWearDllBridge.IWRSetStereoEnabled(stereoHandle, true);
            }
        }

        /// <summary>
        /// Begin GPU query. This method must be called before rendering any 3D object (any information that
        /// will be passed to the GPU for rendering) and before calling EndGPUQuery() method.
        /// </summary>
        public void BeginGPUQuery()
        {
            if (stereoAvailable)
                g_QueryGPU.Begin();
        }

        /// <summary>
        /// End GPU query. This method must be called after rendering any 3D object (any information that
        /// will be passed to the GPU for rendering) and after calling BeginGPUQuery() method.
        /// </summary>
        public void EndGPUQuery()
        {
            if (stereoAvailable)
                g_QueryGPU.End();
        }

        /// <summary>
        /// Synchronize.
        /// </summary>
        /// <param name="eye"></param>
        /// <returns></returns>
        public bool SynchronizeEye(iWearDllBridge.Eyes eye)
        {
            if (!stereoAvailable)
                return false;

            iWearDllBridge.IWRWaitForOpenFrame(stereoHandle, false);
            // In windowed mode, we must poll for vSync.
            if (!State.Graphics.IsFullScreen)
                while (State.Device.RasterStatus.ScanLine < windowBottomLine) ;

            while (!g_QueryGPU.IsComplete)
            {
                // Waiting on gpu to complete rendering.
                // MUST be certain the frame will scan out on the next vSync interval.
            }

            if (eye == iWearDllBridge.Eyes.LEFT_EYE)
                State.Device.Present();

            return iWearDllBridge.IWRSetStereoLR(stereoHandle, (int)eye);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        public void UpdateBottomLine(Game game)
        {
            if (!State.Graphics.IsFullScreen)
            {
                // In windowed mode we need the bottom line of our window.
                // MUST Never be greater than the displays last scanline.
                windowBottomLine = game.Window.ClientBounds.Bottom;
                if (windowBottomLine >= State.Device.DisplayMode.Height)
                    windowBottomLine = State.Device.DisplayMode.Height - 1;
            }
        }

        public void Update(GameTime gameTime, bool deviceActive)
        {
            if (!isAvailable)
                return;

            int iwr_status;
            int y = 0, p = 0, r = 0;

            // Get iWear tracking yaw, pitch, roll
            iwr_status = (int)iWearDllBridge.IWRGetTracking(ref y, ref p, ref r);
            if (iwr_status != 0)
            {
                Log.Write("iWear tracker is either OFFLine or unplugged.", Log.LogLevel.Error);
                isAvailable = false;
                return;
            }

            yaw = ConvertToRadians(y);
            pitch = ConvertToRadians(p);
            roll = ConvertToRadians(r);
        }

        public void Dispose()
        {
            if (stereoHandle != ((IntPtr)(-1)))
                iWearDllBridge.IWRCloseStereo(stereoHandle);
            iWearDllBridge.IWRCloseTracker();
            g_QueryGPU.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Convert Raw values from the iWear Tracker to radians.
        /// </summary>
        private float ConvertToRadians(long value)
        {
            return (float)value * MathHelper.Pi / 32768.0f;
        }

        #endregion
    }
}
