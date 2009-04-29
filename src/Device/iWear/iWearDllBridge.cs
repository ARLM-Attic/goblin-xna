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
using System.Runtime.InteropServices;
using System.Text;

namespace GoblinXNA.Device.iWear
{
    /// <summary>
    /// A DLL bridge to access the VR920s/iWear stereo and head tracking driver.
    /// </summary>
    public class iWearDllBridge
    {
        #region Enums

        public enum Eyes : int { LEFT_EYE = 0, RIGHT_EYE = 1 }

        #endregion

        #region DLL Imports

        // iWear Tracking.
        [DllImport("iWearDrv.dll", SetLastError = false, EntryPoint = "IWROpenTracker")]
        public static extern long IWROpenTracker();

        [DllImport("iWearDrv.dll", SetLastError = false, EntryPoint = "IWRCloseTracker")]
        public static extern void IWRCloseTracker();

        [DllImport("iWearDrv.dll", SetLastError = false, EntryPoint = "IWRGetTracking")]
        public static extern long IWRGetTracking(ref int yaw, ref int pitch, ref int roll);

        [DllImport("iWearDrv.dll", SetLastError = false, EntryPoint = "IWRSetFilterState")]
        public static extern void IWRSetFilterState(Boolean on);

        // iWear Stereoscopy.
        [DllImport("iWrstDrv.dll", SetLastError = false, EntryPoint = "IWRSTEREO_Open")]
        public static extern IntPtr IWROpenStereo();

        [DllImport("iWrstDrv.dll", SetLastError = false, EntryPoint = "IWRSTEREO_Close")]
        public static extern void IWRCloseStereo(IntPtr handle);

        [DllImport("iWrstDrv.dll", SetLastError = false, EntryPoint = "IWRSTEREO_SetLR")]
        public static extern Boolean IWRSetStereoLR(IntPtr handle, int eye);

        [DllImport("iWrstDrv.dll", SetLastError = false, EntryPoint = "IWRSTEREO_SetStereo")]
        public static extern Boolean IWRSetStereoEnabled(IntPtr handle, Boolean enabled);

        [DllImport("iWrstDrv.dll", SetLastError = false, EntryPoint = "IWRSTEREO_WaitForAck")]
        public static extern Byte IWRWaitForOpenFrame(IntPtr handle, Boolean eye);

        #endregion
    }
}
