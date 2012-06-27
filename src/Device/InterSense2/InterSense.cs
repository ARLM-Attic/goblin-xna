using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using GoblinXNA.Device;

namespace GoblinXNA.Device.InterSense2
{
    public class InterSense : InputDevice_6DOF
    {
        #region Member Fields

        private int handle;
        private ISDllBridge.ISD_TRACKING_DATA_TYPE data;
        private ISDllBridge.ISD_STATION_INFO_TYPE[] station;
        private ISDllBridge.ISD_TRACKER_INFO_TYPE tracker;
        private ISDllBridge.ISD_HARDWARE_INFO_TYPE hwInfo;

        private uint maxStations;
        private int stationID;

        private bool isAvailable;
        private static InterSense iSense;

        #endregion

        #region Constructor

        InterSense()
        {
            handle = ISDllBridge.ISD_OpenTracker(IntPtr.Zero, 0, false, false);
            maxStations = 0;
            stationID = 1;

            if (handle > 0)
            {
                tracker = new ISDllBridge.ISD_TRACKER_INFO_TYPE();
                hwInfo = new ISDllBridge.ISD_HARDWARE_INFO_TYPE();
                station = new ISDllBridge.ISD_STATION_INFO_TYPE[8];

                // Get tracker configuration info 
                ISDllBridge.ISD_GetTrackerConfig(handle, ref tracker, false);

                if (ISDllBridge.ISD_GetSystemHardwareInfo(handle, ref hwInfo))
                {
                    if (hwInfo.Valid)
                    {
                        maxStations = hwInfo.Cap_MaxStations;
                    }
                }

                ISDllBridge.ISD_GetStationConfig(handle, ref station[stationID - 1], stationID, false);

                data = new ISDllBridge.ISD_TRACKING_DATA_TYPE();

                isAvailable = true;
            }
            else
                isAvailable = false;
        }

        #endregion

        #region Propreties

        public string Identifier
        {
            get { return "InterSense"; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
        }

        public Matrix WorldTransformation
        {
           // get { return Matrix.Identity; }
            get{ Matrix returnMatrix = new Matrix();
                Quaternion orientation = new Quaternion(TrackingData.Quaternion[0],TrackingData.Quaternion[1],TrackingData.Quaternion[2],TrackingData.Quaternion[3]);
                returnMatrix = Matrix.CreateFromQuaternion(orientation) * Matrix.CreateTranslation(new Vector3(TrackingData.Position[0],TrackingData.Position[1],TrackingData.Position[2]));
                return returnMatrix;
            }
        }

        /// <summary>
        /// Gets the instance of InterSense tracker.
        /// </summary>
        public static InterSense Instance
        {
            get
            {
                if (iSense == null)
                    iSense = new InterSense();

                return iSense;
            }
        }

        public int StationID
        {
            get { return stationID; }
            set 
            {
                if (stationID != value && stationID < maxStations)
                {
                    stationID = value;
                    ISDllBridge.ISD_GetStationConfig(handle, ref station[stationID - 1], stationID, false);
                }
            }
        }

        public ISDllBridge.ISD_TRACKER_INFO_TYPE TrackerInfo
        {
            get { return tracker; }
        }

        public ISDllBridge.ISD_STATION_INFO_TYPE StationInfo
        {
            get { return station[stationID - 1]; }
            set
            {
                station[stationID - 1] = value;
                ISDllBridge.ISD_SetStationConfig(handle, ref station[stationID - 1], stationID, false);
            }
        }

        public ISDllBridge.ISD_STATION_DATA_TYPE TrackingData
        {
            get { return data.Station[stationID - 1]; }
        }

        public float TrackingTime
        {
            get { return ISDllBridge.ISD_GetTime(); }
        }

        #endregion

        public void Update(TimeSpan elapsedTime, bool deviceActive)
        {
            if (isAvailable)
            {
                ISDllBridge.ISD_GetTrackingData(handle, ref data);
            }
        }

        public void Dispose()
        {
            if(isAvailable)
                ISDllBridge.ISD_CloseTracker(handle);
        }
    }
}
