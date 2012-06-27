using System;
using System.Text;                          // StringBuilder class
using System.Runtime.InteropServices;       // DllImport declarations

//////////////////////////////////////////////////////////////////////////////
//
//      File Name:      ISenseLib.cs
//      Description:    C# interface to InterSense DLL
//      Created:        2008-07-01
//      Updated:        2010-04-13
//      Author:         Rand Kmiec
//
//      Copyright:      InterSense 2008 - All rights Reserved.
//
//      Comments:       This file is a C# analog to the isense.h and isense.c files; it allows
//                      the InterSense DLL to be called from C#.  Please see the isense.h file for
//                      additional information.
//
//                       
//////////////////////////////////////////////////////////////////////////////

namespace GoblinXNA.Device.InterSense2
{
    /// <summary>
    /// This DLL bridge is for InterSense SDK version 4.17
    /// </summary>
    public class ISDllBridge
    {
        /*****************************************/
        /*                                       */
        /*   Enumerations from InterSense DLL    */
        /*                                       */
        /*****************************************/
        // tracking system type
        public enum ISD_SYSTEM_TYPE
        {
            ISD_NONE = 0,           // none found, or can't identify 
            ISD_PRECISION_SERIES,   // InertiaCube2, IS-300, IS-600, IS-900 and IS-1200 
            ISD_INTERTRAX_SERIES    // InterTrax 
        };

        // tracking system model 
        public enum ISD_SYSTEM_MODEL
        {
            ISD_UNKNOWN = 0,
            ISD_IS300,          // 3DOF system 
            ISD_IS600,          // 6DOF system 
            ISD_IS900,          // 6DOF system   
            ISD_INTERTRAX,      // InterTrax (Serial) 
            ISD_INTERTRAX_2,    // InterTrax (USB) 
            ISD_INTERTRAX_LS,   // InterTraxLS, verification required 
            ISD_INTERTRAX_LC,   // InterTraxLC 
            ISD_ICUBE2,         // InertiaCube2 
            ISD_ICUBE2_PRO,     // InertiaCube2 Pro 
            ISD_IS1200,         // 6DOF system   
            ISD_ICUBE3,         // InertiaCube3 
            ISD_ICUBE4,         // InertiaCube4
            ISD_INTERTRAX_3,    // InterTrax3 
            ISD_IMUK,           // K-Sensor 
            ISD_ICUBE2B_PRO     // InertiaCube2B Pro 
        };

        public enum ISD_INTERFACE_TYPE
        {
            ISD_INTERFACE_UNKNOWN = 0,
            ISD_INTERFACE_SERIAL,
            ISD_INTERFACE_USB,
            ISD_INTERFACE_ETHERNET_UDP,
            ISD_INTERFACE_ETHERNET_TCP,
            ISD_INTERFACE_IOCARD,
            ISD_INTERFACE_PCMCIA,
            ISD_INTERFACE_FILE
        };


        /*****************************************/
        /*                                       */
        /*      Variables from InterSense DLL    */
        /*                                       */
        /*****************************************/

        public const string ISLIB_VERSION = "4.17";

        public const int ISD_MAX_TRACKERS = 32;
        public const int ISD_MAX_STATIONS = 8;

        // orientation format 
        public const int ISD_EULER = 1;
        public const int ISD_QUATERNION = 2;

        // Coordinate frame in which position and orientation data is reported 
        public const int ISD_DEFAULT_FRAME = 1;    // InterSense default 
        public const int ISD_VSET_FRAME = 2;       // Virtual set frame, use for camera tracker only 

        // number of supported stylus buttons 
        public const int ISD_MAX_BUTTONS = 8;

        // hardware is limited to 10 analog/digital input channels per station 
        public const int ISD_MAX_CHANNELS = 10;

        // maximum supported number of bytes for auxiliary input data
        public const int ISD_MAX_AUX_INPUTS = 4;

        // maximum supported number of bytes for auxiliary output data
        public const int ISD_MAX_AUX_OUTPUTS = 4;


        /*****************************************/
        /*                                       */
        /*      Structs from InterSense DLL      */
        /*                                       */
        /*****************************************/


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_TRACKER_INFO_TYPE
        {
            /// <summary>
            /// Following item are for information only and should not be changed 
            /// </summary>
            public float LibVersion;    // InterSense Library version 

            public uint TrackerType;    // IS Precision series or InterTrax. 
            // TrackerType can be: 
            // ISD_PRECISION_SERIES for IS-300, IS-600, IS-900 and IS-1200 model trackers, 
            // ISD_INTERTRAX_SERIES for InterTrax, or 
            // ISD_NONE if tracker is not initialized 

            public uint TrackerModel;   // ISD_UNKNOWN, ISD_IS300, ISD_IS600, ISD_IS900, ISD_INTERTRAX 

            public uint Port;           // Number of the rs232 port. Starts with 1. 

            // Communications statistics. For information only. 

            public uint RecordsPerSec;
            public float KBitsPerSec;

            // Following items are used to configure the tracker and can be set in
            // the isenseX.ini file 

            public uint SyncState;  // 4 states: 0 - OFF, system is in free run 
            //           1 - ON, hardware genlock frequency is automatically determined
            //           2 - ON, hardware genlock frequency is specified by the user
            //           3 - ON, no hardware signal, lock to the user specified frequency  

            public float SyncRate;  // Sync frequency - number of hardware sync signals per second, 
            // or, if SyncState is 3 - data record output frequency 

            public uint SyncPhase;  // 0 to 100 per cent    

            public uint Interface;  // hardware interface, read-only 

            public uint UltTimeout; // IS-900 only, ultrasonic timeout (sampling rate)
            public uint UltVolume;  // IS-900 only, ultrasonic speaker volume
            public uint dwReserved4;

            public float FirmwareRev; // Firmware revision 
            public float fReserved2;
            public float fReserved3;
            public float fReserved4;

            public bool LedEnable;   // IS-900 only, blue led on the SoniDiscs enable flag
            public bool bReserved2;
            public bool bReserved3;
            public bool bReserved4;
        };


        ///////////////////////////////////////////////////////////////////////////////
        // ISD_STATION_INFO_TYPE can only be used with IS Precision Series tracking devices.
        // If passed to ISD_SetStationConfig or ISD_GetStationConfig with InterTrax, FALSE is returned. 
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_STATION_INFO_TYPE
        {
            /// <summary>
            /// A unique number identifying a station. It is the same as that passed to the ISD_SetStationState
            /// and ISD_GetStationState functions and can be 1 to ISD_MAX_STATIONS
            /// </summary>
            public uint ID;

            /// <summary>
            /// TRUE if ON, FALSE if OFF
            /// </summary>
            public bool State;           

            /// <summary>
            /// 0 or 2 for OFF or ON. Setting 1 is not in use and will have the same result as ON. 
            /// Only available for IS-X Series devices and InertiaCube2 Pro. For all others this setting 
            /// is always 2. This controls the state of the compass component of the InertiaCube. 
            /// Compass is only used when station is configured for GEOS or Dual modes, in Fusion mode 
            /// compass readings are not used, regardless of this setting. When station is configured 
            /// for FULL compass mode, the readings produced by the magnetometers inside the InertiaCube 
            /// are used as absolute reference orientation for yaw. Compass can be affected by metallic 
            /// objects and electronic equipment in close proximity to the InertiaCube. If compass is OFF, 
            /// no heading compensation is applied 
            /// </summary>
            public bool Compass;        

            /// <summary>
            /// InertiaCube associated with this station. If no InertiaCube is assigned, this number is -1. 
            /// Otherwise, it is a positive number 1 to 4. Only relevant for IS-300 and IS-600 Series devices. 
            /// For IS-900 it is always the same as the station number, for InterTrax and InertiaCube2 
            /// it’s always 1.
            /// </summary>
            public int InertiaCube;     

            public uint Enhancement;    // levels 0, 1, or 2 
            public uint Sensitivity;    // levels 1 to 4 
            public uint Prediction;     // 0 to 50 ms 
            public uint AngleFormat;    // ISD_EULER or ISD_QUATERNION 

            public bool TimeStamped;    // TRUE if time stamp is requested 
            public bool GetInputs;      // TRUE if button and joystick data is requested 
            public bool GetEncoderData; // TRUE if raw encoder data is requested 

            // This setting controls how Magnetic Environment Calibration is applied. This Calibration
            // calculates nominal field strength and dip angle for the environment in which sensor
            // is used. Based on these values system can assign weight to compass measurements,
            // allowing for bad measurements to be rejected. Values from 0 to 3 are accepted.
            // If CompassCompensation is set to 0, the calibration is ignored and all compass data is
            // used. Higher values result in tighter rejection threshold, resulting in more measurements 
            // being rejected. If sensor is used in an environment with a lot of magnetic inteference
            // this can result in drift due to insuficient compensation from the compass data. Default
            // setting is 2.
            // -----------------------------------------------------------------------------------------
            public byte CompassCompensation;

            // This setting controls how the system deals with sharp changes in IMU data that can
            // be caused by shock or impact. Sensors may experience momentary rotation rates or
            // accelerations that are outside of the specified range, resulting in undesirable 
            // behaviour. By turning on shock suppression you can have the system filter out
            // corrupted data. Values 0 (OFF) to 2 are accepted, with higher values resulting in
            // greated filterring.
            // -----------------------------------------------------------------------------------------
            public byte ImuShockSuppression;

            // This setting controls the rejection threshold for ultrasonic measurements. Currently
            // implemented only for PC Tracker. Default setting is 4, which results in measurements
            // with range error greater than 4 times the averate to be rejected. Please do not change
            // this setting without consulting with InterSense technical support.
            // -----------------------------------------------------------------------------------------
            public byte UrmRejectionFactor;

            public byte bReserved2;

            public uint CoordFrame;     // coord frame in which position and orientation data is reported  

            // AccelSensitivity is used for 3-DOF tracking with InertiaCube products only. It controls how 
            // fast tilt correction, using accelerometers, is applied. Valid values are 1 to 4, with 2 as default. 
            // Default is best for head tracking in static environment, with user seated. 
            // Level 1 reduces the amount of tilt correction during movement. While it will prevent any effect  
            // linear accelerations may have on pitch and roll, it will also reduce stability and dynamic accuracy. 
            // It should only be used in situations when sensor is not expected to experience a lot of movement.
            // Level 3 allows for more aggressive tilt compensation, appropriate when sensor is moved a lot, 
            // for example, when user is walking for long durations of time. 
            // Level 4 allows for even greater tilt corrections. It will reduce orientation accuracy by 
            // allowing linear accelerations to effect orientation, but increase stability. This level 
            // is appropriate for when user is running, or in other situations when sensor experiences 
            // a great deal of movement. 
            // -----------------------------------------------------------------------------------------
            public uint AccelSensitivity;

            public uint dwReserved3;
            public uint dwReserved4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] TipOffset;   // coordinates in station frame of the point being tracked
            public float fReserved4;

            public bool GetCameraData;  // TRUE to get computed FOV, aperture, etc  
            public bool GetAuxInputs;
            public bool GetCovarianceData;
            public bool GetExtendedData;
        };

        ///////////////////////////////////////////////////////////////////////////////

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_STATION_DATA_TYPE
        {
            public byte TrackingStatus;     // tracking status byte 
            public byte NewData;            // TRUE if data changed since last call to ISD_Get_Data       
            public byte CommIntegrity;      // Communication integrity of wireless link 
            public byte BatteryState;       // Wireless devices only 0=n/a, 1=low, 2=ok

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Euler;              // Orientation in Euler angle format 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] Quaternion;         // Orientation in Quaternion format

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] Position;           // Always in meters 
            public float TimeStamp;            // Seconds, reported only if requested 
            public float StillTime;            // InertiaCube and PC-Tracker products only
            public float BatteryLevel;         // Battery Voltage, if available
            public float CompassYaw;           // Magnetometer heading, computed based on current orientation.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ISD_MAX_BUTTONS)]
            public bool[] ButtonState;         // Only if requested 


            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ISD_MAX_CHANNELS)]
            public short[] AnalogData;         // only if requested 

            // -----------------------------------------------------------------------------------------
            // Current hardware is limited to 10 channels, only 2 are used. 
            // The only device using this is the IS-900 wand that has a built-in
            // analog joystick. Channel 1 is x-axis rotation, channel 2 is y-axis
            // rotation 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ISD_MAX_AUX_INPUTS)]
            public byte[] AuxInputs;


            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AngularVelBodyFrame;  // rad/sec, in sensor body coordinate frame. 
            // This is the processed angular rate, with current biases 
            // removed. This is the angular rate used to produce 
            // orientation updates.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AngularVelNavFrame;   // rad/sec, in world coordinate frame, with boresight and other
            // transformations applied. 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AccelBodyFrame;       // meter^2/sec, in sensor body coordinate frame. This is 
            // the accelerometer measurements in the sensor body coordinate 
            // frame. Only factory calibration is applied to this data, 
            // gravity component is not removed.    

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AccelNavFrame;          // meters/sec^2, in the navigation (earth) coordinate frame. 
            // This is the accelerometer measurements with calibration,  
            // and current sensor orientation applied, and gravity  
            // subtracted. This is the best available estimate of
            // acceleration.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] VelocityNavFrame;       // meters/sec, 6-DOF systems only.  

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] AngularVelRaw;          // Raw gyro output, only factory calibration is applied. 
            // Some errors due to temperature dependant gyro bias 
            // drift will remain.

            public byte MeasQuality;        // Ultrasonic Measurement Quality (IS-900 only, firmware >= 4.26)
            public byte bReserved2;
            public byte bReserved3;
            public byte bReserved4;

            public uint TimeStampSeconds;       // Time Stamp in whole seconds.
            public uint TimeStampMicroSec;      // Fractional part of the Time Stamp in micro-seconds.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 61)]
            public uint[] Reserved;
        };

        ///////////////////////////////////////////////////////////////////////////////

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_CAMERA_ENCODER_DATA_TYPE
        {
            public byte TrackingStatus;     // tracking status byte 
            public byte bReserved1;         // pack to 4 byte boundary 
            public byte bReserved2;
            public byte bReserved3;

            public uint Timecode;            // timecode, not implemented yet 
            public int ApertureEncoder;     // Aperture encoder counts, relative to last reset of power up 
            public int FocusEncoder;        // Focus encoder counts 
            public int ZoomEncoder;         // Zoom encoded counts 
            public uint TimecodeUserBits;    // Time code user bits, not implemented yet 

            public float Aperture;           // Computed Aperture value 
            public float Focus;              // Computed focus value (mm), not implemented yet 
            public float FOV;                // Computed vertical FOV value (degrees) 
            public float NodalPoint;         // Nodal point offset due to zoom and focus (mm) 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] CovarianceOrientation;     // Available only for IS-1200

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] CovariancePosition;

            public uint dwReserved1;
            public uint dwReserved2;

            public float fReserved1;
            public float fReserved2;
            public float fReserved3;
            public float fReserved4;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_TRACKING_DATA_TYPE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ISD_MAX_STATIONS)]
            public ISD_STATION_DATA_TYPE[] Station;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_CAMERA_DATA_TYPE
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = ISD_MAX_STATIONS)]
            ISD_CAMERA_ENCODER_DATA_TYPE[] Camera;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_HARDWARE_INFO_TYPE
        {
            public bool Valid;         // set to TRUE if ISD_GetSystemHardwareInfo succeeded

            public uint TrackerType;      // see ISD_SYSTEM_TYPE
            public uint TrackerModel;     // see ISD_SYSTEM_MODEL
            public uint Port;             // hardware port number (Com1, etc.)
            public uint Interface;        // hardware interface (RS232, USB, etc.)
            public bool OnHost;           // tracking algorithms are executed in the dll
            public uint AuxSystem;        // position tracking hardware, see ISD_AUX_SYSTEM_TYPE
            public float FirmwareRev;     // Firmware revision 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public char[] ModelName;

            // Capabilities
            public bool Cap_Position;     // can tracker position
            public bool Cap_Orientation;  // can tracker orientation
            public bool Cap_Encoders;     // can support lens encoders
            public bool Cap_Prediction;   // predictive algorithms are available
            public bool Cap_Enhancement;  // enhancement level can be changed
            public bool Cap_Compass;      // compass setting can be changed
            public bool Cap_SelfTest;     // has the self-test capability
            public bool Cap_ErrorLog;     // can keep error log

            public bool Cap_UltVolume;    // can ultrasonic volume be software controled
            public bool Cap_UltGain;      // can microphone sensitivity be software controled
            public bool Cap_UltTimeout;   // can ultrasonic sampling frequency be changed
            public bool Cap_PhotoDiode;   // SoniDiscs support photodiode

            public uint Cap_MaxStations;  // number of supported stations
            public uint Cap_MaxImus;      // number of supported IMUs
            public uint Cap_MaxFPses;     // maximum number of Fixed Position Sensing Elements (constellation)
            public uint Cap_MaxChannels;  // maximum number of analog channels supported per station
            public uint Cap_MaxButtons;   // maximum number of digital button inputs per station

            public bool Cap_MeasData;     // can provide measurement data
            public bool Cap_DiagData;     // can provide diag data
            public bool Cap_PseConfig;    // supports PSE configuration/reporting tools
            public bool Cap_ConfigLock;   // supports configuration locking     

            public float Cap_UltMaxRange;  // maximum ultrasonic range  
            public float Cap_fReserved2;
            public float Cap_fReserved3;
            public float Cap_fReserved4;

            public bool Cap_CompassCal;   // supports dynamic compass calibration     
            public bool Cap_bReserved2;
            public bool Cap_bReserved3;
            public bool Cap_bReserved4;

            public uint Cap_dwReserved1;
            public uint Cap_dwReserved2;
            public uint Cap_dwReserved3;
            public uint Cap_dwReserved4;

            public bool bReserved1;
            public bool bReserved2;
            public bool bReserved3;
            public bool bReserved4;

            public uint BaudRate;           // Serial port baud rate      
            public uint NumTestLevels;      // Number of self test levels       
            public uint dwReserved3;
            public uint dwReserved4;

            public float fReserved1;
            public float fReserved2;
            public float fReserved3;
            public float fReserved4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved4;
        };

        ///////////////////////////////////////////////////////////////////////////////

        // Station hardware information.
        // This structure provides detailed information on station hardware and
        // it's capabilities.
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ISD_STATION_HARDWARE_INFO_TYPE
        {
            public bool Valid;             // set to TRUE if ISD_GetStationHardwareInfo succeeded

            public uint ID;                // unique number identifying a station. It is the same as that 
            // passed to the ISD_SetStationConfig and ISD_GetStationConfig   
            // functions and can be 1 to ISD_MAX_STATIONS 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public char[] DescVersion;     // Station Descriptor version 

            public float FirmwareRev;      // Station firmware revision.
            public uint SerialNum;         // Serial number 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public char[] CalDate;         // Cal date (mm/dd/yyyy)
            public uint Port;              // Hardware port number 

            public struct Capability
            {
                public bool Position;      // TRUE if station can track position
                public bool Orientation;   // TRUE if station can track orientation
                public uint Encoders;      // number lens encoders, is 0 then none are available
                public uint NumChannels;   // number of analog channels supported by this station, wand has 2 (joystick)
                public uint NumButtons;    // number of digital button inputs supported by this station
                public uint AuxInputs;     // number of auxiliary input channels (OEM products)
                public uint AuxOutputs;    // number of auxiliary output channels (OEM products)
                public bool Compass;       // TRUE is station has compass

                public bool bReserved1;
                public bool bReserved2;
                public bool bReserved3;
                public bool bReserved4;

                public uint dwReserved1;
                public uint dwReserved2;
                public uint dwReserved3;
                public uint dwReserved4;
            };

            public bool bReserved1;
            public bool bReserved2;
            public bool bReserved3;
            public bool bReserved4;

            public uint Type;           // station type        
            public uint DeviceID;
            public uint dwReserved3;
            public uint dwReserved4;

            public float fReserved1;
            public float fReserved2;
            public float fReserved3;
            public float fReserved4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            char[] cReserved4;
        };



        /*****************************************/
        /*                                       */
        /*      Functions from InterSense DLL    */
        /*                                       */
        /*****************************************/

        // Returns -1 on failure. To detect tracker automatically specify 0 for commPort.
        // hParent parameter to ISD_OpenTracker is optional and should only be used if 
        // information screen or tracker configuration tools are to be used when available 
        // in the future releases. If you would like a tracker initialization window to be 
        // displayed, specify TRUE value for the infoScreen parameter (not implemented in
        // this release). 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ISD_OpenTracker(
                                                 IntPtr hParent,
                                                 int commPort,
                                                 bool infoScreen,
                                                 bool verbose
                                                 );

        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ISD_OpenAllTrackers(
                                                     IntPtr hParent,
                                                     ref IntPtr handle,
                                                     bool infoScreen,
                                                     bool verbose
                                                     );

        // This function call deinitializes the tracker, closes communications port and 
        // frees the resources associated with this tracker. If 0 is passed, all currently
        // open trackers are closed. When last tracker is closed, program frees the DLL. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_CloseTracker(int handle);


        // Get general tracker information, such as type, model, port, etc.
        // Also retrieves genlock synchronization configuration, if available. 
        // See ISD_TRACKER_INFO_TYPE structure definition above for complete list of items 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetTrackerConfig(
                                                       int handle,
                                                       ref ISD_TRACKER_INFO_TYPE Tracker,
                                                       bool verbose
                                                       );

        // When used with IS Precision Series (IS-300, IS-600, IS-900, IS-1200) tracking devices 
        // this function call will set genlock synchronization  parameters, all other fields 
        // in the ISD_TRACKER_INFO_TYPE structure are for information purposes only 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_SetTrackerConfig(
                                                       int handle,
                                                       ref ISD_TRACKER_INFO_TYPE Tracker,
                                                       bool verbose
                                                       );

        // Get RecordsPerSec and KBitsPerSec without requesting genlock settings from the tracker.
        // Use this instead of ISD_GetTrackerConfig to prevent your program from stalling while
        // waiting for the tracker response. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetCommInfo(
                                                  int handle,
                                                  ref ISD_TRACKER_INFO_TYPE Tracker
                                                  );

        // Configure station as specified in the ISD_STATION_INFO_TYPE structure. Before 
        // this function is called, all elements of the structure must be assigned a value. 
        // stationID is a number from 1 to ISD_MAX_STATIONS. Should only be used with
        // IS Precision Series tracking devices, not valid for InterTrax.  
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_SetStationConfig(
                                                       int handle,
                                                       ref ISD_STATION_INFO_TYPE Station,
                                                       int stationID,
                                                       bool verbose
                                                       );

        // Fills the ISD_STATION_INFO_TYPE structure with current settings. Function
        // requests configuration records from the tracker and waits for the response.
        // If communications are interrupted, it will stall for several seconds while 
        // attempting to recover the settings. Should only be used with IS Precision Series 
        // tracking devices, not valid for InterTrax.
        // stationID is a number from 1 to ISD_MAX_STATIONS 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetStationConfig(
                                                       int handle,
                                                       ref ISD_STATION_INFO_TYPE Station,
                                                       int stationID,
                                                       bool verbose
                                                       );

        // Not supported on UNIX in this release
        // When a tracker is first opened, library automatically looks for a configuration
        // file in current directory of the application. File name convention is
        // isenseX.ini where X is a number, starting at 1, identifying one tracking 
        // system in the order of initialization. This function provides for a way to
        // manually configure the tracker using a different configuration file.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_ConfigureFromFile(
                                                        int handle,
                                                        String path,
                                                        bool verbose
                                                        );

        // Save tracker configuration. For devices with on-host processing,
        // like PC-Tracker model, this will write to isenseX.cfg file. 
        // Serial port devices like IS-300, IS-600 and IS-900 save configuration 
        // in the base unit, and this call will just send command to commit the
        // changes to permanent storage.
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_ConfigSave(int handle);

        // Get data from all configured stations. Data is places in the ISD_TRACKING_DATA_TYPE
        // structure. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ISD_GetTrackingData(
                                                     int handle,
                                                     ref ISD_TRACKING_DATA_TYPE Data
                                                     );

        // Get camera encode and other data for all configured stations. Data is places 
        // in the ISD_CAMERA_DATA_TYPE structure. This function does not service serial 
        // port, so ISD_GetData must be called prior to this. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetCameraData(
                                                    int handle,
                                                    ref ISD_CAMERA_DATA_TYPE Data
                                                    );


        // By default, ISD_GetData processes all the records available from the tracker
        // and only returns the latest data. As the result, data samples can be lost.
        // If all the data samples are required, you can use a ring buffer to store them.
        // ISD_SetupRingBuffer accepts the pointer to the ring buffer, and it's size.
        // Once activated, all processed data samples are stored in the buffer for use
        // by the application. 
        //
        // ISD_GetData can still be used to read the data, but it would return the
        // oldest saved data sample, then remove it from the buffer (first in - first out). 
        // By repeatedly calling ISD_GetData all samples are retrieved, the latest
        // coming last. All consecutive calls to ISD_GetData will return the last
        // sample, but the NewData flag will be FALSE to indicate that buffer has
        // been emptied.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_RingBufferSetup(
                                                      int handle,
                                                      int stationID,
                                                      ref ISD_STATION_DATA_TYPE dataBuffer,
                                                      int samples
                                                      );

        // Activate the ring buffer. While active, all data samples are stored in the 
        // buffer. Because this is a ring buffer, it will only store the number of samples
        // specified in the call to ISD_SetupRingBuffer, so the oldest samples can be 
        // overwritten.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_RingBufferStart(
                                                      int handle,
                                                      int stationID
                                                      );

        // Stop collection. The data will continue to be processed, but the contents of
        // the ring buffer will not be altered.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_RingBufferStop(
                                                     int handle,
                                                     int stationID
                                                     );

        // Queries the DLL for the latest data without removing it from the buffer or 
        // affecting the NewData flag. It also returns the indexes of the newest and the
        // oldest samples in the buffer. User program can use these to parse the buffer.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_RingBufferQuery(
                                                      int handle,
                                                      int stationID,
                                                      ref ISD_STATION_DATA_TYPE currentData,
                                                      IntPtr head,
                                                      IntPtr tail
                                                      );

        // Reset heading to zero 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_ResetHeading(
                                                   int handle,
                                                   int stationID
                                                   );

        // Works with all IS-X00 series products and InertiaCube2, and InterTraxLC.
        // For InterTrax30 and InterTrax2 behaves like ISD_ResetHeading.
        // Boresight station using specific reference angles. This is useful when
        // you need to apply a specific offset to system output. For example, if
        // a sensor is mounted at 40 degrees relative to the HMD, you can 
        // enter 0, 40, 0 to get the system to output zero when HMD is horizontal.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_BoresightReferenced(
                                                          int handle,
                                                          int stationID,
                                                          float yaw,
                                                          float pitch,
                                                          float roll
                                                          );

        // Works with all IS-X00 series products and InertiaCube2, and InterTraxLC.
        // For InterTrax30 and InterTrax2 behaves like ISD_ResetHeading.
        // Boresight, or unboresight a station. If 'set' is TRUE, all angles
        // are reset to zero. Otherwise, all boresight settings are cleared,
        // including those set by ISD_ResetHeading and ISD_BoresightReferenced
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_Boresight(
                                                int handle,
                                                int stationID,
                                                bool set
                                                );

        // Send a configuration script to the tracker. Script must consist of valid 
        // commands as described in the interface protocol. Commands in the script 
        // should be terminated by the New Line character '\n'. Line Feed character '\r' 
        // is added by the function and is not required. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_SendScript(
                                                 int handle,
                                                 ref StringBuilder script
                                                 );

        // Sends up to 4 output bytes to the auxiliary interface of the station  
        // specified. The number of bytes should match the number the auxiliary outputs
        // interface is set up to expect. If too many are specified, the extra bytes 
        // are ignored. 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_AuxOutput(
                                                int handle,
                                                int stationID,
                                                ref byte AuxOutput,
                                                int length
                                                );

        // Number of currently opened trackers is stored in the parameter passed to this
        // functions 
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_NumOpenTrackers(ref int num);


        // Platform independent time
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float ISD_GetTime();

        // Broadcast tracker data over the network
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_UdpDataBroadcast(
                                                       int handle,
                                                       int port,
                                                       ref ISD_TRACKING_DATA_TYPE trackingData,
                                                       ref ISD_CAMERA_DATA_TYPE cameraData
                                                       );

        // System hardware information.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetSystemHardwareInfo( /*ISD_TRACKER_HANDLE*/int handle,
                                                  ref ISD_HARDWARE_INFO_TYPE hwInfo);

        // Station hardware information.
        // ----------------------------------------------------------------------------
        [DllImport("isense.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ISD_GetStationHardwareInfo( /*ISD_TRACKER_HANDLE*/int handle,
                                                   ref ISD_STATION_HARDWARE_INFO_TYPE info,
                                                   int stationID);
    }
}
