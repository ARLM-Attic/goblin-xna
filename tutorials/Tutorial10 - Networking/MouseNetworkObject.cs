using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GoblinXNA.Network;
using GoblinXNA.Helpers;

namespace Tutorial10___Networking
{
    /// <summary>
    /// An implementation of INetworkObject interface for transmitting mouse press information.
    /// </summary>
    public class MouseNetworkObject : INetworkObject
    {
        /// <summary>
        /// A delegate function to be called when a mouse press event is sent over the network
        /// </summary>
        /// <param name="near">A point on the near clipping plane</param>
        /// <param name="far">A point on the far clipping plane</param>
        public delegate void ShootFunction(Vector3 near, Vector3 far);

        #region Member Fields

        private bool readyToSend;
        private bool hold;
        private int sendFrequencyInHertz;

        private bool reliable;
        private bool inOrder;

        private int pressedButton;
        private Vector3 nearPoint;
        private Vector3 farPoint;
        private ShootFunction callbackFunc;

        #endregion

        #region Constructors

        public MouseNetworkObject()
        {
            readyToSend = false;
            hold = false;
            sendFrequencyInHertz = 0;

            reliable = true;
            inOrder = true;
        }

        #endregion

        #region Properties
        public String Identifier
        {
            get { return "MouseNetworkObject"; }
        }

        public bool ReadyToSend
        {
            get { return readyToSend; }
            set { readyToSend = value; }
        }

        public bool Hold
        {
            get { return hold; }
            set { hold = value; }
        }

        public int SendFrequencyInHertz
        {
            get { return sendFrequencyInHertz; }
            set { sendFrequencyInHertz = value; }
        }

        public bool Reliable
        {
            get { return reliable; }
            set { reliable = value; }
        }

        public bool Ordered
        {
            get { return inOrder; }
            set { inOrder = value; }
        }

        public ShootFunction CallbackFunc
        {
            get { return callbackFunc; }
            set { callbackFunc = value; }
        }

        public int PressedButton
        {
            get { return pressedButton; }
            set { pressedButton = value; }
        }

        public Vector3 NearPoint
        {
            get { return nearPoint; }
            set { nearPoint = value; }
        }

        public Vector3 FarPoint
        {
            get { return farPoint; }
            set { farPoint = value; }
        }
        #endregion

        #region Public Methods
        public byte[] GetMessage()
        {
            // 1 byte: pressedButton
            // 12 bytes: near point (3 floats)
            // 12 bytes: far point (3 floats)
            byte[] data = new byte[1 + 12 + 12];

            data[0] = (byte)pressedButton;
            ByteHelper.FillByteArray(ref data, 1, BitConverter.GetBytes(nearPoint.X));
            ByteHelper.FillByteArray(ref data, 5, BitConverter.GetBytes(nearPoint.Y));
            ByteHelper.FillByteArray(ref data, 9, BitConverter.GetBytes(nearPoint.Z));
            ByteHelper.FillByteArray(ref data, 13, BitConverter.GetBytes(farPoint.X));
            ByteHelper.FillByteArray(ref data, 17, BitConverter.GetBytes(farPoint.Y));
            ByteHelper.FillByteArray(ref data, 21, BitConverter.GetBytes(farPoint.Z));

            return data;
        }

        public void InterpretMessage(byte[] msg)
        {
            pressedButton = (int)msg[0];

            nearPoint.X = ByteHelper.ConvertToFloat(msg, 1);
            nearPoint.Y = ByteHelper.ConvertToFloat(msg, 5);
            nearPoint.Z = ByteHelper.ConvertToFloat(msg, 9);

            farPoint.X = ByteHelper.ConvertToFloat(msg, 13);
            farPoint.Y = ByteHelper.ConvertToFloat(msg, 17);
            farPoint.Z = ByteHelper.ConvertToFloat(msg, 21);

            if (callbackFunc != null)
                callbackFunc(nearPoint, farPoint);
        }

        #endregion
    }
}
