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
 * Authors: Mark Eaddy 
 *          Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Diagnostics;
using System.Collections;

using Microsoft.Xna.Framework;

using System.Xml.Serialization;
using GoblinXNA;
using GoblinXNA.Helpers;

namespace GoblinXNA.Device.InterSense
{
	/// <summary>
	/// A driver class for accessing the InterSense trackers.
	/// </summary>
	public class InterSense : InputDeviceDriver, IDisposable
	{
		/*public const int nINDEX_STATION_1					= 0;
		public const int nINDEX_STATION_2_HAND				= 1; // Hand tracker
		public const int nINDEX_STATION_3_HMD				= 2; // Tracker attached to HMD
		public const int nINDEX_STATION_4_WIRELESS			= 3; // Wireless*/

		[XmlElement("connection")]
		public ConnectionInfo connInfo;
		
		[XmlArray("stations")]
		[XmlArrayItem(typeof(StationInfo))]
		public StationInfo[]	stationInfo;    

		private Hashtable		stationHash = new Hashtable(ISDllBridge.ISD_MAX_STATIONS);
		private StationArray	stationArray;

		//depending on our connection type, we will use 
		private IntPtr isenseHandle = IntPtr.Zero;
		private InterSenseSocket isenseSocket = null;
		
		#region "StationArray internal class"
		/// <summary>
		/// Manages the array of stations. Uses lazy allocation to allocate
		/// and update only stations that are actually used.
		/// </summary>
		public class StationArray
		{
			public StationArray(StationInfo[] info)
			{
				this.info = info;
			}

			public bool isActive(long index)
			{
				return stations[index] != null;
			}

			public void SetData(ISDllBridge.ISD_TRACKER_DATA_TYPE dataISense)
			{
				foreach (InterSenseStation station in stations)
				{
					if (station != null)
						station.SetData(dataISense);
				}
			}

			// Array indexer
			public InterSenseStation this[long index]
			{
				get 
				{ 
					if (stations[index] == null)
						stations[index] = new InterSenseStation(index,info[index].transform);

					return stations[index]; 
				}
			}

			// DATA
			private StationInfo[]	info = null;
			private InterSenseStation[] stations = new InterSenseStation[ISDllBridge.ISD_MAX_STATIONS];
		}
		#endregion

		#region "StationInfo internal class"
		/// <summary>
		/// This class maintains a mapping of name->index, for use in serialization
		/// </summary>
		public class StationInfo
		{
			public StationInfo()
			{}
			public StationInfo(string name, int index, Matrix headTransform)
			{
				this.name = name;
				this.index = index;
				this.transform = headTransform;
			}

			[XmlElement]
			public string	name;           

			[XmlElement]
			public long		index;          

			[XmlElement("head_transform")]
			public Matrix	transform;      
		}
		#endregion
		
		#region "ConnectionInfo internal class"
		public class ConnectionInfo
		{
			public ConnectionInfo()
			{}
			public ConnectionInfo(string host, int port, bool preferNetwork, bool tryBoth)
			{
				this.preferNetwork = preferNetwork;
				this.tryBoth = tryBoth;
				this.host = host;
				this.port = port;
			}
			
			[XmlElement]
			public string	host;               

			[XmlElement]
			public int		port;               

			[XmlElement("prefer_network")]
			public bool		preferNetwork;

			[XmlElement("try_both")]
			public bool		tryBoth;
		}
		#endregion

		public InterSense() : this("InterSense") { }

		public InterSense(string unique_identifier)
		{
			identifier = unique_identifier;

            if (!(State.GetSettingVariable("InterSenseHost").Equals("") ||
                State.GetSettingVariable("InterSensePort").Equals("")))
            {
                connInfo = new ConnectionInfo(State.GetSettingVariable("InterSenseHost"),
                    int.Parse(State.GetSettingVariable("InterSensePort")), true, true);
            }
            else
                connInfo = null;

			stationInfo = new StationInfo[ISDllBridge.ISD_MAX_STATIONS];
			for(int i=0;i<stationInfo.Length;i++)
			{
				stationInfo[i] = new StationInfo("InterSenseStation"+i,i, Matrix.CreateTranslation(0, -0.1524f, -0.1397f));
				stationHash[stationInfo[i].name] = stationInfo[i].index;
			}
			stationArray = new StationArray(stationInfo);
		}


		public override void Dispose()
		{
			// Close the socket (if open)
			if (isenseSocket != null)
			{
				isenseSocket.Dispose();
				isenseSocket = null;
			}

			// Close the serial port (if open)
			if (isenseHandle != IntPtr.Zero)
			{
				ISDllBridge.ISD_CloseTracker(isenseHandle);
				isenseHandle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// This gets called after deserialization
		/// </summary>
		override public void InitAfterDeserialization()
		{
			//build the hash from the name mapping
			foreach(StationInfo i in stationInfo)
				stationHash[i.name] = i.index;
			stationArray = new StationArray(stationInfo);
		}

		/// <summary>
		/// Given a station name, returns the InputDevice that accesses its data
		/// </summary>
		/// <param name="input_uid"></param>
		/// <returns></returns>
		override public InputDevice_6DOF Get6DOFInputByName(string input_uid)
		{
			//do lazy initialization
			if (!Init())        
				return null;
			
			if (stationHash[input_uid] == null)
				return null;

			//because of lazy allocation, we need to access the stationArray once first
			//to make it created, then call Update to fill in data for it
			//then we can return the station
			InterSenseStation ret = stationArray[(long)stationHash[input_uid]];
			//Log.Write("Performing initial retrieval of data.");
			if (!Update(0))
				return null;
			
			return ret;
		}

        public override InputDevice GetInputByName(string input_uid)
        {
            throw new GoblinException("InterSense does not return InputDevice. " +
                "Instead, use get6DOFInputDeviceName");
        }

		//public override bool 
		public bool Init()
		{
			if (IsInited())
			{
				return true;
			}

            Log.Write("Connecting to InterSense...", Log.LogLevel.Log);

			// First try to connect to the InterSense network server
			bool bRet = InitSocket();
			if (!bRet)
			{
				// If that fails, try connecting directly using the serial port driver
				bRet = InitDriver();
			}

			if (bRet)
			{
                Log.Write("InterSense connection successful.", Log.LogLevel.Log);

			}
			else
			{
				Log.Write("Unable to connect to InterSense head tracker using socket or serial port driver.");
                return false;
			}

			return true;
		}

		private bool InitSocket()
		{
            if (connInfo == null)
                return false;

			if(isenseSocket == null)
				isenseSocket = new InterSenseSocket();

			Debug.Assert(!isenseSocket.IsConnected());

			return isenseSocket.Connect(connInfo.host,connInfo.port);
		}

		private bool InitDriver()
		{
			// Step 1:	Connect to InterSense using the serial port driver

			Debug.Assert(isenseHandle == IntPtr.Zero);

			try
			{
				isenseHandle = ISDllBridge.ISD_OpenTracker(IntPtr.Zero, 0, true, false);
			}
			catch (DllNotFoundException)
			{
				/*MyTrace.Log(TraceLevel.Warning,
					"Failed to connect to InterSense via serial port: {0}",
					ex.Message);*/
			}

			if (isenseHandle == IntPtr.Zero)
			{
				return false;
			}

			// Step 2:	Tell InterSense to give us euler

			for(short i = 1; i <= ISDllBridge.ISD_MAX_STATIONS; ++i)
			{
				ISDllBridge.ISD_STATION_INFO_TYPE stationInfoType;

				bool bRet = ISDllBridge.ISD_GetStationConfig(isenseHandle, 
					out stationInfoType, 
					i,
					true); 
				if (!bRet)
				{
					/*MyTrace.Log(TraceLevel.Error, "ISD_GetStationConfig({0}) failed.",
						i);*/
					return false;
				}

				stationInfoType.AngleFormat = ISDllBridge.ISD_EULER;

				bRet = ISDllBridge.ISD_SetStationConfig(isenseHandle, 
					ref stationInfoType, 
					i,
					true); 

				if (!bRet)
				{
					/*MyTrace.Log(TraceLevel.Error, "ISD_SetStationConfig({0}) failed.",
						i);*/
					return false;
				}
			}

			return true;
		}

		public bool IsInited()
		{
			return isenseSocket != null || isenseHandle != IntPtr.Zero;
		}

		/// <summary>
		/// Updates all its mapped inputs
		/// </summary>
		/// <param name="elapsed_seconds"></param>
		override public bool Update(float elapsed_seconds)
		{
			if (!IsInited())
			{
				return false;
			}

			ISDllBridge.ISD_TRACKER_DATA_TYPE dataISense;
			bool bSuccess = false;

			// Try the socket

			if (isenseSocket != null)
			{
				if (!isenseSocket.IsConnected())
				{
					return false;
				}

				// Pass the station array so that we know that stations to
				// request data for in the network message
				bSuccess = isenseSocket.GetData(stationArray, out dataISense);
				if (bSuccess)
				{
					stationArray.SetData(dataISense);
					return true;
				}
				else
				{
					// If that fails, try connecting directly using the serial port driver
					InitDriver();
					isenseSocket = null;
				}
			}

			// Try getting the data via serial port

			if (!bSuccess && isenseHandle != IntPtr.Zero)
			{
				bSuccess = ISDllBridge.ISD_GetData(isenseHandle, out dataISense);
				if (bSuccess)
				{
					stationArray.SetData(dataISense);
				}
			}

			return bSuccess;
		}
	}
}
