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

using GoblinXNA.Helpers;

namespace GoblinXNA.Network
{
    /// <summary>
    /// An implementation of the INetworkObject interface.
    /// </summary>
    public class NetworkObject : INetworkObject
    {
        #region Network Parameters
        protected bool readyToSend;
        protected bool hold;
        protected int sendFrequencyInHertz;

        protected bool reliable;
        protected bool ordered;

        protected Matrix worldTransform;
        protected bool hasChange;
        protected String name;
        protected int id;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a network object.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public NetworkObject(String name, int id)
        {
            this.name = name;
            this.id = id;

            readyToSend = false;
            hasChange = false;
            hold = false;
            sendFrequencyInHertz = 0;

            ordered = true;
            reliable = true;
        }
        #endregion

        #region Properties
        public String Identifier
        {
            get { return (name.Equals("") ? "Node " + id : name); }
            internal set { name = value; }
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

        internal bool HasChange
        {
            get { return hasChange; }
            set { hasChange = value; }
        }

        internal Matrix WorldTransform
        {
            get { return worldTransform; }
            set { worldTransform = value; }
        }
        #endregion

        #region Public Methods
        public byte[] GetMessage()
        {
            return MatrixHelper.ConvertToBytes(worldTransform);
        }

        public void InterpretMessage(byte[] msg)
        {
            worldTransform = MatrixHelper.ConvertFromBytes(msg);
            hasChange = true;
        }
        #endregion
    }
}
