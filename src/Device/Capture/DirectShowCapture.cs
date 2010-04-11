using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

using DShowNET;
using DShowNET.Device;

using GoblinXNA;
using GoblinXNA.Device;

namespace GoblinXNA.Device.Capture
{
    public class DirectShowCapture2 : ISampleGrabberCB, IVideoCapture
    {
        private enum PlaybackState
        {
            Stopped,
            Paused,
            Running,
            Init
        }

        #region Member Fields

        private DsDevice videoDevice;

        /// <summary> base filter of the actually used video devices. </summary>
        private IBaseFilter capFilter;

        /// <summary> graph builder interface. </summary>
        private IGraphBuilder graphBuilder;

        /// <summary> capture graph builder interface. </summary>
        private ICaptureGraphBuilder2 capGraph;
        private ISampleGrabber sampGrabber;

        /// <summary> control interface. </summary>
        private IMediaControl mediaCtrl;

        /// <summary> event interface. </summary>
        private IMediaEventEx mediaEvt;

        /// <summary> grabber filter interface. </summary>
        private IBaseFilter baseGrabFlt;

        /// <summary> structure describing the bitmap to grab. </summary>
        private VideoInfoHeader videoInfoHeader;

        private IAMStreamConfig videoStreamConfig;

        private ArrayList capDevices;

        private PointF focalPoint;

        private IResizer resizer;

        private int videoDeviceID;

        private int cameraWidth;
        private int cameraHeight;
        private bool grayscale;
        private bool cameraInitialized;
        private Resolution resolution;
        private FrameRate frameRate;
        private ImageFormat format;

        private volatile PlaybackState playbackState = PlaybackState.Stopped;

        private IntPtr grabbedImage;

        /// <summary>
        /// Used to count the number of times it failed to capture an image
        /// If it fails more than certain times, it will assume that the video
        /// capture device can not be accessed
        /// </summary>
        private int failureCount;

        private const int FAILURE_THRESHOLD = 100;

        private bool processing = false;
        private bool grabbing = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a video capture using the DirectShow library.
        /// </summary>
        public DirectShowCapture2()
        {
            cameraInitialized = false;
            videoDeviceID = -1;
            focalPoint = new PointF(0, 0);
            grabbedImage = IntPtr.Zero;

            cameraWidth = 0;
            cameraHeight = 0;
            grayscale = false;

            failureCount = 0;
        }

        #endregion

        #region Properties

        public int Width
        {
            get { return cameraWidth; }
        }

        public int Height
        {
            get { return cameraHeight; }
        }

        public PointF FocalPoint
        {
            get { return focalPoint; }
            set { focalPoint = value; }
        }

        public int VideoDeviceID
        {
            get { return videoDeviceID; }
        }

        public bool GrayScale
        {
            get { return grayscale; }
        }

        public bool Initialized
        {
            get { return cameraInitialized; }
        }

        public ImageFormat Format
        {
            get { return format; }
        }

        public IResizer MarkerTrackingImageResizer
        {
            get { return resizer; }
            set { resizer = value; }
        }

        public SpriteEffects RenderFormat
        {
            get { return SpriteEffects.FlipVertically; }
        }

        #endregion

        #region Public Methods

        public void InitVideoCapture(int videoDeviceID, FrameRate framerate, Resolution resolution, 
            ImageFormat format, bool grayscale)
        {
            if (cameraInitialized)
                return;

            this.resolution = resolution;
            this.grayscale = grayscale;
            this.frameRate = framerate;
            this.videoDeviceID = videoDeviceID;
            this.format = format;

            switch (resolution)
            {
                case Resolution._160x120:
                    cameraWidth = 160;
                    cameraHeight = 120;
                    break;
                case Resolution._320x240:
                    cameraWidth = 320;
                    cameraHeight = 240;
                    break;
                case Resolution._640x480:
                    cameraWidth = 640;
                    cameraHeight = 480;
                    break;
                case Resolution._800x600:
                    cameraWidth = 800;
                    cameraHeight = 600;
                    break;
                case Resolution._1024x768:
                    cameraWidth = 1024;
                    cameraHeight = 768;
                    break;
                case Resolution._1280x960:
                    cameraWidth = 1280;
                    cameraHeight = 960;
                    break;
                case Resolution._1600x1200:
                    cameraWidth = 1600;
                    cameraHeight = 1200;
                    break;
            }

            double frame_rate = 0;
            switch (frameRate)
            {
                case FrameRate._15Hz: frame_rate = 15; break;
                case FrameRate._30Hz: frame_rate = 29.997; break;
                case FrameRate._50Hz: frame_rate = 50; break;
                case FrameRate._60Hz: frame_rate = 60; break;
                case FrameRate._120Hz: frame_rate = 120; break;
                case FrameRate._240Hz: frame_rate = 240; break;
            }

            if (!DsUtils.IsCorrectDirectXVersion())
                throw new GoblinException("DirectX 8.1 NOT installed!");

            if (!DsDev.GetDevicesOfCat(FilterCategory.VideoInputDevice, out capDevices))
                throw new GoblinException("No video capture devices found!");

            DsDevice dev = null;
            if (videoDeviceID >= capDevices.Count)
            {
                String suggestion = "Try the following device IDs:";
                for (int i = 0; i < capDevices.Count; i++)
                {
                    suggestion += " " + i + ":" + ((DsDevice)capDevices[i]).Name + ", ";
                }
                throw new GoblinException("VideoDeviceID " + videoDeviceID + " is out of the range. "
                    + suggestion);
            }
            dev = (DsDevice)capDevices[videoDeviceID];

            if (dev == null)
                throw new GoblinException("This video device cannot be accessed");

            StartupVideo(dev.Mon);

            cameraInitialized = true;
        }

        public void GetImageTexture(int[] returnImage, ref IntPtr imagePtr)
        {
            if (grabbedImage != IntPtr.Zero)
            {
                processing = true;
                Marshal.Copy(grabbedImage, returnImage, 0, returnImage.Length);

                int stride = cameraWidth * 3;
                int srcStride = cameraWidth * 4;

                if (imagePtr != IntPtr.Zero)
                {
                    unsafe
                    {
                        byte* src = (byte*)(new IntPtr(grabbedImage.ToInt32() + 
                            (cameraHeight - 1) * srcStride));
                        byte* dst = (byte*)imagePtr;
                        for (int i = 0; i < cameraHeight; i++)
                        {
                            for (int j = 0, k = 0; j < stride; j += 3, k += 4)
                            {
                                //*(dst + j) = *(src + k + 2);
                                *(dst + j + 1) = *(src + k + 1);
                                //*(dst + j + 2) = *(src + k + 0);
                            }

                            src -= srcStride;
                            dst += stride;
                        }
                    }
                }
                    
                processing = false;
            }
        }

        public void Dispose()
        {
            int hr;
            try
            {
                if (mediaCtrl != null)
                {
                    hr = mediaCtrl.Stop();
                    mediaCtrl = null;
                }

                if (mediaEvt != null)
                    mediaEvt = null;

                baseGrabFlt = null;
                if (sampGrabber != null)
                    Marshal.ReleaseComObject(sampGrabber); sampGrabber = null;

                if (capGraph != null)
                    Marshal.ReleaseComObject(capGraph); capGraph = null;

                if (graphBuilder != null)
                    Marshal.ReleaseComObject(graphBuilder); graphBuilder = null;

                if (capFilter != null)
                    Marshal.ReleaseComObject(capFilter); capFilter = null;

                if (capDevices != null)
                {
                    foreach (DsDevice d in capDevices)
                        d.Dispose();
                    capDevices = null;
                }
            }
            catch (Exception)
            { }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///  Retrieves the value of one member of the IAMStreamConfig format block.
        ///  Helper function for several properties that expose
        ///  video/audio settings from IAMStreamConfig.GetFormat().
        ///  IAMStreamConfig.GetFormat() returns a AMMediaType struct.
        ///  AMMediaType.formatPtr points to a format block structure.
        ///  This format block structure may be one of several 
        ///  types, the type being determined by AMMediaType.formatType.
        /// </summary>
        private object getStreamConfigSetting(IAMStreamConfig streamConfig, string fieldName)
        {
            if (streamConfig == null)
                throw new NotSupportedException();

            object returnValue = null;
            IntPtr pmt = IntPtr.Zero;
            AMMediaType mediaType = new AMMediaType();

            try
            {
                // Get the current format info
                int hr = streamConfig.GetFormat(out pmt);
                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);
                Marshal.PtrToStructure(pmt, mediaType);

                // The formatPtr member points to different structures
                // dependingon the formatType
                object formatStruct;
                if (mediaType.formatType == FormatType.WaveEx)
                    formatStruct = new WaveFormatEx();
                else if (mediaType.formatType == FormatType.VideoInfo)
                    formatStruct = new VideoInfoHeader();
                else if (mediaType.formatType == FormatType.VideoInfo2)
                    formatStruct = new VideoInfoHeader2();
                else
                    throw new NotSupportedException("This device does not support a recognized format block.");

                // Retrieve the nested structure
                Marshal.PtrToStructure(mediaType.formatPtr, formatStruct);

                // Find the required field
                Type structType = formatStruct.GetType();
                FieldInfo fieldInfo = structType.GetField(fieldName);
                if (fieldInfo == null)
                    throw new NotSupportedException("Unable to find the member '" + fieldName + "' in the format block.");

                // Extract the field's current value
                returnValue = fieldInfo.GetValue(formatStruct);

            }
            finally
            {
                DsUtils.FreeAMMediaType(mediaType);
                Marshal.FreeCoTaskMem(pmt);
            }

            return (returnValue);
        }

        /// <summary>
        ///  Set the value of one member of the IAMStreamConfig format block.
        ///  Helper function for several properties that expose
        ///  video/audio settings from IAMStreamConfig.GetFormat().
        ///  IAMStreamConfig.GetFormat() returns a AMMediaType struct.
        ///  AMMediaType.formatPtr points to a format block structure.
        ///  This format block structure may be one of several 
        ///  types, the type being determined by AMMediaType.formatType.
        /// </summary>
        private object setStreamConfigSetting(IAMStreamConfig streamConfig, string fieldName, object newValue)
        {
            if (streamConfig == null)
                throw new NotSupportedException();

            object returnValue = null;
            IntPtr pmt = IntPtr.Zero;
            AMMediaType mediaType = new AMMediaType();

            try
            {
                // Get the current format info
                int hr = streamConfig.GetFormat(out pmt);
                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);
                Marshal.PtrToStructure(pmt, mediaType);

                // The formatPtr member points to different structures
                // dependingon the formatType
                object formatStruct;
                if (mediaType.formatType == FormatType.WaveEx)
                    formatStruct = new WaveFormatEx();
                else if (mediaType.formatType == FormatType.VideoInfo)
                    formatStruct = new VideoInfoHeader();
                else if (mediaType.formatType == FormatType.VideoInfo2)
                    formatStruct = new VideoInfoHeader2();
                else
                    throw new NotSupportedException("This device does not support a recognized format block.");

                // Retrieve the nested structure
                Marshal.PtrToStructure(mediaType.formatPtr, formatStruct);

                // Find the required field
                Type structType = formatStruct.GetType();
                FieldInfo fieldInfo = structType.GetField(fieldName);
                if (fieldInfo == null)
                    throw new NotSupportedException("Unable to find the member '" + fieldName + "' in the format block.");

                // Update the value of the field
                fieldInfo.SetValue(formatStruct, newValue);

                // PtrToStructure copies the data so we need to copy it back
                Marshal.StructureToPtr(formatStruct, mediaType.formatPtr, false);

                // Save the changes
                hr = streamConfig.SetFormat(mediaType);
                if (hr != 0)
                    Marshal.ThrowExceptionForHR(hr);
            }
            finally
            {
                //DsUtils.FreeAMMediaType(mediaType);
                Marshal.FreeCoTaskMem(pmt);
            }

            return (mediaType);
        }

        private void StartupVideo(UCOMIMoniker mon)
        {
            int hr;
            try
            {
                GetInterfaces();

                CreateCaptureDevice(mon);

                SetupGraph();

                hr = mediaCtrl.Run();
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                sampGrabber.SetCallback(this, 1);
            }
            catch (Exception ee)
            {
                throw new GoblinException("Could not start video stream\r\n" + ee.Message);
            }
        }

        /// <summary> 
        /// build the capture graph for grabber. 
        /// </summary>
        private void SetupGraph()
        {
            int hr;
            Guid cat;
            Guid med;
            try
            {
                hr = capGraph.SetFiltergraph(graphBuilder);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(capFilter, "Ds.NET Video Capture Device");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                AMMediaType media = new AMMediaType();
                media.majorType = MediaType.Video;
                media.subType = MediaSubType.RGB32;
                media.formatType = FormatType.VideoInfo;		// ???

                hr = sampGrabber.SetMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                object o;
                cat = PinCategory.Capture;
                med = MediaType.Video;
                Guid iid = typeof(IAMStreamConfig).GUID;
                hr = capGraph.FindInterface(
                    ref cat, ref med, capFilter, ref iid, out o);

                videoStreamConfig = o as IAMStreamConfig;

                hr = sampGrabber.SetBufferSamples(false);
                if (hr == 0)
                    hr = sampGrabber.SetOneShot(false);
                if (hr == 0)
                    hr = sampGrabber.SetCallback(null, 0);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                BitmapInfoHeader bmiHeader;
                bmiHeader = (BitmapInfoHeader)getStreamConfigSetting(videoStreamConfig, "BmiHeader");
                bmiHeader.Width = cameraWidth;
                bmiHeader.Height = cameraHeight;
                setStreamConfigSetting(videoStreamConfig, "BmiHeader", bmiHeader);

                bmiHeader = (BitmapInfoHeader)getStreamConfigSetting(videoStreamConfig, "BmiHeader");
                if (bmiHeader.Width != cameraWidth)
                    throw new GoblinException("Could not change the resolution to " + cameraWidth + "x" +
                        cameraHeight + ". The resolution has to be " + bmiHeader.Width + "x" +
                        bmiHeader.Height);

                cat = PinCategory.Preview;
                med = MediaType.Video;
                hr = capGraph.RenderStream(ref cat, ref med, capFilter, null, baseGrabFlt); // baseGrabFlt 
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                media = new AMMediaType();
                hr = sampGrabber.GetConnectedMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                    throw new NotSupportedException("Unknown Grabber Media Format");

                videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                Marshal.FreeCoTaskMem(media.formatPtr); media.formatPtr = IntPtr.Zero;
            }
            catch (Exception ee)
            {
                throw new GoblinException("Could not setup graph\r\n" + ee.Message);
            }
        }

        /// <summary> 
        /// create the used COM components and get the interfaces. 
        /// </summary>
        private void GetInterfaces()
        {
            Type comType = null;
            object comObj = null;
            String errMsg = "";
            try
            {
                comType = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow FilterGraph not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                graphBuilder = (IGraphBuilder)comObj; comObj = null;

                Guid clsid = Clsid.CaptureGraphBuilder2;
                Guid riid = typeof(ICaptureGraphBuilder2).GUID;
                comObj = DsBugWO.CreateDsInstance(ref clsid, ref riid);
                capGraph = (ICaptureGraphBuilder2)comObj; comObj = null;

                comType = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow SampleGrabber not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                sampGrabber = (ISampleGrabber)comObj; comObj = null;

                mediaCtrl = (IMediaControl)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                baseGrabFlt = (IBaseFilter)sampGrabber;
            }
            catch (Exception ee)
            {
                errMsg = "Could not get interfaces\r\n" + ee.Message;
            }
            finally
            {
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }

            if (errMsg.Length > 0)
                throw new GoblinException(errMsg);
        }

        /// <summary> 
        /// create the user selected capture device. 
        /// </summary>
        private void CreateCaptureDevice(UCOMIMoniker mon)
        {
            object capObj = null;
            String errMsg = "";
            try
            {
                Guid gbf = typeof(IBaseFilter).GUID;
                mon.BindToObject(null, null, ref gbf, out capObj);
                capFilter = (IBaseFilter)capObj; capObj = null;
            }
            catch (Exception ee)
            {
                errMsg = ee.Message;
            }
            finally
            {
                if (capObj != null)
                    Marshal.ReleaseComObject(capObj); capObj = null;
            }

            if (errMsg.Length > 0)
                throw new GoblinException("Could not create capture device\r\n" + errMsg);
        }

        #region ISampleGrabberCB Members
        /// <summary>
        /// Buffer Callback method from the  DirectShow.NET ISampleGrabberCB interface.  This method is called
        /// when a new frame is grabbed by the SampleGrabber.
        /// </summary>
        /// <param name="SampleTime">The sample time.</param>
        /// <param name="pBuffer">A pointer to the image buffer that contains the grabbed sample.</param>
        /// <param name="BufferLen">The length of the image buffer containing the grabbed sample.</param>
        /// <returns>0 = success.</returns>
        public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            while (processing) { }
            grabbedImage = pBuffer;
            return 0;
        }
        /// <summary>
        /// Sample CallBack method from the ISampleGrabberCB interface (DirectShow.NET).  Not used.
        /// </summary>
        /// <param name="SampleTime"></param>
        /// <param name="pSample"></param>
        /// <returns></returns>
        public int SampleCB(double SampleTime, IMediaSample pSample)
        {
            throw new GoblinException("The method or operation is not implemented.");
        }

        #endregion

        #endregion
    }
}
