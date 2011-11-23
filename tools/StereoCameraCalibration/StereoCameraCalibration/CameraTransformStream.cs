using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using GoblinXNA.Network;
using GoblinXNA.Helpers;

namespace StereoCameraCalibration
{
    public class CameraTransformStream : INetworkObject
    {
        public delegate void SetCameraTransforms(Matrix leftCamTransform, Matrix rightCamTransform);

        #region Member Fields

        private bool readyToSend;
        private bool hold;
        private int sendFrequencyInHertz;

        private bool reliable;
        private bool ordered;
        private byte[] data;

        private Matrix rightTransform;
        private Matrix leftTransform;
        private int matrixLength;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a network object.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public CameraTransformStream()
        {
            readyToSend = false;
            hold = false;
            sendFrequencyInHertz = 0;

            ordered = true;
            reliable = true;

            matrixLength = sizeof(float) * 7;
            data = new byte[matrixLength * 2];
            leftTransform = Matrix.Identity;
            rightTransform = Matrix.Identity;
        }

        #endregion

        #region Properties

        public String Identifier
        {
            get { return "CameraTransform"; }
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
            get { return ordered; }
            set { ordered = value; }
        }

        public Matrix LeftCameraTransform
        {
            get { return leftTransform; }
            set { leftTransform = value; }
        }

        public Matrix RightCameraTransform
        {
            get { return rightTransform; }
            set { rightTransform = value; }
        }

        public SetCameraTransforms SetCameraTransformCallback
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public byte[] GetMessage()
        {
            ByteHelper.FillByteArray(ref data, 0, MatrixHelper.ConvertToOptimizedBytes(LeftCameraTransform));
            ByteHelper.FillByteArray(ref data, matrixLength, MatrixHelper.ConvertToOptimizedBytes(RightCameraTransform));

            return data;
        }

        public void InterpretMessage(byte[] msg, int startIndex, int length)
        {
            MatrixHelper.ConvertFromOptimizedBytes(msg, startIndex, matrixLength, ref leftTransform);
            MatrixHelper.ConvertFromOptimizedBytes(msg, startIndex + matrixLength, matrixLength, ref rightTransform);

            SetCameraTransformCallback(leftTransform, rightTransform);
        }

        #endregion
    }
}
