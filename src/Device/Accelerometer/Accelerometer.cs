/************************************************************************************ 
 * Copyright (c) 2008-2011, Columbia University
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
 * Author: Nicolas Dedual (dedual@cs.columbia.edu)
 * 
 *************************************************************************************/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Devices.Sensors;

namespace GoblinXNA.Device.Accelerometer
{
    public class Accelerometer:InputDevice
    {
        #region Member Fields

        Motion motion;

        Vector3 accelerometerVector;
        Vector3 accVectorTemp;

        Vector3 orientationVector;
        Vector3 orientationVectorTemp;

        object accelerometerVectorLock = new object();
        object motionOrientationLock = new object();

        Microsoft.Devices.Sensors.Accelerometer accelerometer;

        String id;

        #endregion

        #region Constructors

        public Accelerometer()
        {
            accelerometer = new Microsoft.Devices.Sensors.Accelerometer();

            if (Motion.IsSupported)
            {
                if (motion == null)
                {
                    motion = new Motion();
                    motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                    motion.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<MotionReading>>(motion_CurrentValueChanged);
                }
            }
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets a unique identifier of this input device
        /// </summary>
        public String Identifier 
        {
            get { return id; } 
            set{id = value;}
        }

        public Vector3 CurrentAcceleration
        {
            get { return accelerometerVector; }
        }

        public Vector3 CurrentOrientation
        {
            get { return orientationVector; }
        }

        /// <summary>
        /// Gets whether this input device is available to use
        /// </summary>
        public bool IsAvailable { get { return (accelerometer.State.Equals(Microsoft.Devices.Sensors.SensorState.Ready)); } }
        #endregion

        #region Public Methods

        public void Initialize()
        {
            if (Motion.IsSupported)
            {
                try
                {
                    motion.Start();
                }
                catch (Exception)
                {

                }
            }
            else
            {

                accelerometer.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(OnAccelerometerReadingChanged);
                try
                {
                    accelerometer.Start();
                }
                catch
                {
                }
            }
        }

        public void Stop()
        {
            accelerometer.Stop();
            motion.Stop();
        }

        
        /// <summary>
        /// Updates the state of this input device
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="deviceActive"></param>
        public void Update(TimeSpan elapsedTime, bool deviceActive)
        {
            lock (accelerometerVectorLock)
            {
                accelerometerVector = accVectorTemp;
            }

            lock (motionOrientationLock)
            {
                orientationVector = orientationVectorTemp;
            }

        }
        /// <summary>
        /// Triggers the callback functions specified in this InputDevice programatically.
        /// </summary>
        /// <param name="data"></param>
        public void TriggerDelegates(byte[] data)
        {
        
            throw new GoblinException("TriggerDelegates for Accelerometer is not implemented yet.");
        
        }

        /// <summary>
        /// Disposes this input device.
        /// </summary>
        public void Dispose()
        {
            accelerometer.Stop();
            motion.Stop();
        }
        
        #endregion

        #region Private Methods

        void motion_CurrentValueChanged(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            lock (motionOrientationLock)
            {
                if (motion.IsDataValid)
                {
                    orientationVectorTemp = new Vector3(MathHelper.ToDegrees(e.SensorReading.Attitude.Yaw), MathHelper.ToDegrees(e.SensorReading.Attitude.Pitch), MathHelper.ToDegrees(e.SensorReading.Attitude.Roll));
                }
            }
        }

        void OnAccelerometerReadingChanged(object sender, SensorReadingEventArgs<AccelerometerReading> args)
        {
            lock (accelerometerVectorLock)
            {
                accVectorTemp = new Vector3((float)args.SensorReading.Acceleration.X, (float)args.SensorReading.Acceleration.Y, (float)args.SensorReading.Acceleration.Z);
            }
        }

        #endregion
    }
}
