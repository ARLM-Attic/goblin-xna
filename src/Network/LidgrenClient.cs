/************************************************************************************ 
 * Copyright (c) 2008-2010, Columbia University
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
 * Author: Levi Lister (levi.lister@gmail.com)
 *         Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Net.Sockets;

using Lidgren.Network;

using GoblinXNA.Helpers;

namespace GoblinXNA.Network
{
    /// <summary>
    /// An implementation of the IClient interface using the Lidgren network library
    /// (http://code.google.com/p/lidgren-library-network).
    /// </summary>
    public class LidgrenClient : IClient
    {
        #region Member Fields

        protected String appName;
        protected int portNumber;
        protected IPEndPoint hostPoint;
        protected byte[] myIPAddress;
        protected bool enableEncryption;
        protected bool isConnected;
        protected bool isServerDiscovered;
        protected bool waitForServer;
        protected int connectionTrialTimeout;
        protected int elapsedTime;
        protected bool shutDownForced;

        protected NetClient netClient;
        protected NetBuffer buffer;
        protected IPAddress myAddr;
        protected NetConfiguration netConfig;

        #endregion

        #region Events

        public event HandleServerConnection ServerConnected;
        public event HandleServerDisconnection ServerDisconnected;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a Lidgren network client with an application name, the port number,
        /// and the host name.
        /// </summary>
        /// <param name="appName">An application name. Must be the same as the server app name.</param>
        /// <param name="portNumber">The port number to establish the connection</param>
        /// <param name="hostName">The name of the server machine</param>
        public LidgrenClient(String appName, int portNumber, String hostName)
        {
            this.appName = appName;
            this.portNumber = portNumber;
            isConnected = false;
            isServerDiscovered = false;
            shutDownForced = false;
            enableEncryption = false;
            waitForServer = false;
            connectionTrialTimeout = -1;
            elapsedTime = 0;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            myAddr = ipEntry.AddressList[0];
            myIPAddress = myAddr.GetAddressBytes();

            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            IPAddress hostAddr = hostEntry.AddressList[0];
            hostPoint = new IPEndPoint(hostAddr, portNumber);

            // Create a configuration for the client
            netConfig = new NetConfiguration(appName);
        }

        /// <summary>
        /// Creates a Lidgren network client with an application name, the port number,
        /// and the host IP address in 4 bytes.
        /// </summary>
        /// <param name="appName">An application name. Must be the same as the server app name.</param>
        /// <param name="portNumber">The port number to establish the connection</param>
        /// <param name="hostIPAddress">The IP address of the host in 4 bytes</param>
        public LidgrenClient(String appName, int portNumber, byte[] hostIPAddress)
        {
            this.appName = appName;
            this.portNumber = portNumber;
            isConnected = false;
            isServerDiscovered = false;
            enableEncryption = false;
            shutDownForced = false;
            waitForServer = false;
            connectionTrialTimeout = -1;
            elapsedTime = 0;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            myAddr = ipEntry.AddressList[0];
            myIPAddress = myAddr.GetAddressBytes();

            IPAddress hostAddr = new IPAddress(hostIPAddress);
            hostPoint = new IPEndPoint(hostAddr, portNumber);

            // Create a configuration for the client
            netConfig = new NetConfiguration(appName);
        }
        #endregion

        #region Properties

        public int PortNumber
        {
            get { return portNumber; }
        }

        public byte[] MyIPAddress
        {
            get { return myIPAddress; }
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        /// <summary>
        /// Gets or sets whether to enable encryption. (NOT implemented for Lidgren.Network)
        /// </summary>
        public bool EnableEncryption
        {
            get { return enableEncryption; }
            set { enableEncryption = value; }
        }

        public bool WaitForServer
        {
            get { return waitForServer; }
            set { waitForServer = value; }
        }

        public int ConnectionTrialTimeOut
        {
            get { return connectionTrialTimeout; }
            set { connectionTrialTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the net configuration for a Lidgren client.
        /// </summary>
        /// <remarks>
        /// For detailed information about each of the properties of NetConfiguration,
        /// please see the documentation included in the Lidgren's distribution package.
        /// </remarks>
        public NetConfiguration NetConfig
        {
            get { return netConfig; }
            set { netConfig = value; }
        }

        #endregion

        #region Public Methods

        public void Connect()
        {
            if (netClient != null && netClient.Status != NetConnectionStatus.Disconnected)
                return;

            isConnected = false;

            // enable encryption; this key was generated using the 'GenerateEncryptionKeys' application
            if (enableEncryption)
            {
                // No encryption mechanism in Lidgren anymore
            }

            // Create a client
            netClient = new NetClient(netConfig);
            netClient.Start();

            buffer = netClient.CreateBuffer();

            try
            {
                if (waitForServer)
                {
                    Thread connectionThread = new Thread(new ThreadStart(TryConnect));
                    connectionThread.Start();
                }
                else
                {
                    if (myAddr.Equals(hostPoint.Address))
                        netClient.DiscoverLocalServers(portNumber);
                    else
                        netClient.DiscoverKnownServer(hostPoint, false);
                }
            }
            catch (SocketException se)
            {
                Log.Write("Socket exception is thrown in Connect: " + se.StackTrace);
            }
        }

        private void TryConnect()
        {
            while (!isServerDiscovered && !shutDownForced)
            {
                if (myAddr.Equals(hostPoint.Address))
                    netClient.DiscoverLocalServers(portNumber);
                else
                    netClient.DiscoverKnownServer(hostPoint, false);

                Thread.Sleep(500);

                elapsedTime += 500;

                if ((connectionTrialTimeout != -1) && (elapsedTime >= connectionTrialTimeout))
                    break;
            }
        }

        public void ReceiveMessage(ref List<byte[]> messages)
        {
            NetMessageType type;

            try
            {
                // read a packet if available
                while (netClient.ReadMessage(buffer, out type))
                {
                    switch (type)
                    {
                        case NetMessageType.ServerDiscovered:
                            NetBuffer buf = netClient.CreateBuffer();
                            buf.Write(myAddr.ToString());
                            netClient.Connect(buffer.ReadIPEndPoint(), buf.ToArray());
                            isServerDiscovered = true;
                            break;
                        case NetMessageType.DebugMessage:
                            Log.Write(buffer.ReadString(), Log.LogLevel.Log);
                            break;
                        case NetMessageType.StatusChanged:
                            if (netClient.Status == NetConnectionStatus.Connected)
                            {
                                isConnected = true;
                                if (ServerConnected != null)
                                    ServerConnected();
                            }
                            else
                            {
                                isConnected = false;
                                if (ServerDisconnected != null)
                                    ServerDisconnected();
                            }

                            Log.Write("New status: " + netClient.Status + " (" + buffer.ReadString() + ")",
                                Log.LogLevel.Log);
                            break;
                        case NetMessageType.Data:
                            messages.Add(buffer.ToArray());
                            break;
                    }
                }
            }
            catch (SocketException se)
            {
                Log.Write("Socket exception is thrown in ReceiveMessage: " + se.StackTrace);
            }
        }

        public void SendMessage(byte[] msg, bool reliable, bool inOrder)
        {
            // subsequent input; send chat message to server
            // create a message
            NetBuffer buf = netClient.CreateBuffer();
            buf.Write(msg);

            NetChannel channel = NetChannel.Unreliable;
            if (reliable)
            {
                if (inOrder)
                    channel = NetChannel.ReliableInOrder1;
                else
                    channel = NetChannel.ReliableUnordered;
            }
            else if (inOrder)
                channel = NetChannel.UnreliableInOrder1;

            try
            {
                netClient.SendMessage(buf, channel);
            }
            catch (SocketException se)
            {
                Log.Write("Socket exception is thrown in SendMessage: " + se.StackTrace);
            }
        }

        public void Shutdown()
        {
            shutDownForced = true;
            netClient.Disconnect("Disconnecting....");
            netClient.Shutdown("Client exitting");
        }

        #endregion
    }
}
