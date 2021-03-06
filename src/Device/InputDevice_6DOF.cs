/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
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

using Microsoft.Xna.Framework;

namespace GoblinXNA.Device
{
    /// <summary>
    /// An interface that defines the properties and methods for any input device that 
    /// can provide 6 degrees of freedom input.
    /// </summary>
	public interface InputDevice_6DOF 
	{
        /// <summary>
        /// Gets a unique identifier of this 6DOF input device.
        /// </summary>
        String Identifier { get; }

        /// <summary>
        /// Gets whether this 6DOF input device is available to use.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the world transformation returned by the 6DOF input device.
        /// </summary>
        Matrix WorldTransformation { get; }

        /// <summary>
        /// Updates the state of this 6DOF input device.
        /// </summary>
        /// <param name="elapsedTime"></param>
        /// <param name="deviceActive"></param>
        void Update(TimeSpan elapsedTime, bool deviceActive);

        /// <summary>
        /// Disposes this 6DOF input device.
        /// </summary>
        void Dispose();
	}
}
