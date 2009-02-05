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
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Network;

namespace GoblinXNA.Device
{
    /// <summary>
    /// A class that maps all of the available input devices to a set of unified functions.
    /// </summary>
    public class InputMapper
    {
        /// <summary>
        /// An identifier string for the mouse device.
        /// </summary>
        /// <see cref=""/>
        public static String Mouse = DeviceEnumerator.Mouse;

        /// <summary>
        /// An identifier string for the keyboard device.
        /// </summary>
        public static String Keyboard = DeviceEnumerator.Keyboard;

        /// <summary>
        /// An identifier string for the generic input device which combines both mouse
        /// and keyboard input device to provide pseudo-6DOF input.
        /// </summary>
        public static String MouseAndKeyboard = DeviceEnumerator.MouseAndKeyboard;

        /// <summary>
        /// An identifier string for station 0 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation0 = DeviceEnumerator.InterSenseStation0;

        /// <summary>
        /// An identifier string for station 1 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation1 = DeviceEnumerator.InterSenseStation1;

        /// <summary>
        /// An identifier string for station 2 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation2 = DeviceEnumerator.InterSenseStation2;

        /// <summary>
        /// An identifier string for station 3 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation3 = DeviceEnumerator.InterSenseStation3;

        /// <summary>
        /// An identifier string for station 4 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation4 = DeviceEnumerator.InterSenseStation4;

        /// <summary>
        /// An identifier string for station 5 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation5 = DeviceEnumerator.InterSenseStation5;

        /// <summary>
        /// An identifier string for station 6 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation6 = DeviceEnumerator.InterSenseStation6;

        /// <summary>
        /// An identifier string for station 7 of the InterSense tracker.
        /// </summary>
        public static String InterSenseStation7 = DeviceEnumerator.InterSenseStation7;

        /// <summary>
        /// An identifier string for the Global Positioning System.
        /// </summary>
        /// <remarks>
        /// Not supported yet.
        /// </remarks>
        public static String GPS = DeviceEnumerator.GPS;

        private static DeviceEnumerator enumerator;

        /// <summary>
        /// A static constructor.
        /// </summary>
        /// <remarks>
        /// Don't instantiate this.
        /// </remarks>
        static InputMapper()
        {
            enumerator = new DeviceEnumerator();
        }

        /// <summary>
        /// Gets the world transformation of a 6DOF input device with the given string identifier.
        /// </summary>
        /// <param name="identifier">A string identifier for a 6DOF input device</param>
        /// <returns></returns>
        public static Matrix GetWorldTransformation(String identifier)
        {
            if (!enumerator.Available6DOFDevices.ContainsKey(identifier))
                return Matrix.Identity;
            else
                return enumerator.Available6DOFDevices[identifier].WorldTransformation;
        }

        /// <summary>
        /// Triggers a delegate/callback function defined in a non-6DOF input device with the given
        /// string identifier by passing an array of bytes that contains data in certain format.
        /// For the specific data format, please see each of the TriggerDelegates(byte[]) 
        /// functions implemented in each class that implements InputDevice interface
        /// (e.g., MouseInput).
        /// </summary>
        /// <param name="identifier">A string identifier for a non-6DOF input device</param>
        /// <param name="data"></param>
        public static void TriggerInputDeviceDelegates(String identifier, byte[] data)
        {
            if (enumerator.AvailableDevices.ContainsKey(identifier))
                enumerator.AvailableDevices[identifier].TriggerDelegates(data);
        }

        /// <summary>
        /// Indicates whether a non-6DOF input device with the given string identifier is available.
        /// </summary>
        /// <param name="identifier">A string identifier for a non-6DOF input device</param>
        /// <returns></returns>
        public static bool ContainsInputDevice(String identifier)
        {
            return enumerator.AvailableDevices.ContainsKey(identifier);
        }

        /// <summary>
        /// Indicates whether a 6DOF input device with the given string identifier is available.
        /// </summary>
        /// <param name="identifier">A string identifier for a 6DOF input device</param>
        /// <returns></returns>
        public static bool Contains6DOFInputDevice(String identifier)
        {
            return enumerator.Available6DOFDevices.ContainsKey(identifier);
        }

        /// <summary>
        /// Updates all of the status of the available 6DOF and non-6DOF input devices.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="deviceActive"></param>
        public static void Update(GameTime gameTime, bool deviceActive)
        {
            enumerator.Update(gameTime, deviceActive);
        }
    }
}
