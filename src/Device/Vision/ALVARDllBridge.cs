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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GoblinXNA.Device.Vision
{
    /// <summary>
    /// A DLL bridge class that accesses the APIs defined in ALVARWrapper.dll, which contains
    /// wrapped methods from the original ALVAR marker & feature tracking library.
    /// </summary>
    public class ALVARDllBridge
    {
        #region Dll Imports

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_init_camera")]
        public static extern int alvar_init_camera(string calibFile, int width, int height);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_get_camera_params")]
        public static extern void alvar_get_camera_params(
            [Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 16)] double[] projMatrix,
            ref double fovX, ref double fovY);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_init_marker_detector")]
        public static extern void alvar_init_marker_detector(double markerSize, int markerRes,
            double margin);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_set_marker_size")]
        public static extern void alvar_set_marker_size(int id, double markerSize);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_set_detect_additional")]
        public static extern void alvar_set_detect_additional(bool enable);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_set_hide_texture_configuration")]
        public static extern void alvar_set_hide_texture_configuration(uint size, uint depth, 
		    uint channels, double margin);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_add_multi_marker")]
        public static extern void alvar_add_multi_marker(int num_ids,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 128)] int[] ids, String filename);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_add_multi_marker_bundle")]
        public static extern void alvar_add_multi_marker_bundle(int num_ids,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 128)] int[] ids);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_detect_marker")]
        public static extern void alvar_detect_marker(int numChannels, string colorModel,
            string channelSeq, IntPtr imageData, [In, Out] IntPtr interestedMarkerIDs,
            ref int numFoundMarkers, ref int numInterestedMarkers, 
            double maxMarkerError, double maxTrackError);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_get_poses")]
        public static extern void alvar_get_poses(
            [Out] IntPtr ids,
            [Out] IntPtr projMatrix,
            bool returnHideTextures,
            [Out] IntPtr hideTextures);

        [DllImport("ALVARWrapper.dll", EntryPoint = "alvar_get_multi_marker_poses")]
        public static extern void alvar_get_multi_marker_poses(
            [Out] IntPtr ids,
            [Out] IntPtr projMatrix,
            [Out] IntPtr errors,
            bool returnHideTextures,
            [Out] IntPtr hideTextures);

        #endregion
    }
}
