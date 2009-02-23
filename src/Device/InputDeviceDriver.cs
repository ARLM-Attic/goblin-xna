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
 * Author: John Waugh
 *         Mark Eaddy
 * 
 *************************************************************************************/ 

using System;
using System.Xml.Serialization;

namespace GoblinXNA.Device
{
	/// <summary>
	/// This is the base class for any provider of InputDevice.
	/// </summary>
	
	[Serializable]
	abstract public class InputDeviceDriver : IDisposable
	{
		public InputDeviceDriver()
		{
			identifier = "<InputDeviceDriver_uninitialized_identifier>";
		}

		[XmlElement("driver_uid")]
		public string identifier;

		/// <summary>
		/// This function returns the InputDevice corresponding to a name
		/// </summary>
		/// <param name="input_uid"></param>
		/// <returns></returns>
		abstract public InputDevice GetInputByName(String input_uid);

        abstract public InputDevice_6DOF Get6DOFInputByName(String input_uid);

		/// <summary>
		/// Subclasses should use this function to update all mapped inputs.
		/// </summary>
		/// <param name="elapsed_seconds"></param>
		abstract public bool Update(float elapsed_seconds);

		/// <summary>
		/// This function is available in case your class needs to do additional work to completely
		/// restore its state after being deserialized
		/// </summary>
		virtual public void InitAfterDeserialization()
		{}

		virtual public void Dispose()
		{ }
	}
}
