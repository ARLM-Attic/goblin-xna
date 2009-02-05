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
 * Author: Levi Lister (levi.lister@gmail.com)
 *         Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using Lidgren.Network;

using GoblinXNA.Helpers;

namespace GoblinXNA.Network
{
    /// <summary>
    /// An implementation of the IServer interface using the Lidgren network library
    /// (http://code.google.com/p/lidgren-library-network).
    /// </summary>
    public class LidgrenServer : IServer
    {
        #region Member Fields

        protected int portNumber;
        protected byte[] myIPAddress;
        protected bool enableEncryption;
        protected String appName;
        protected NetConfiguration netConfig;
        protected NetBuffer buffer;
        protected NetServer netServer;
        protected Dictionary<String, String> approveList;

        protected NetConnection prevSender;
        protected Dictionary<String, NetConnection> clients;

        #endregion

        #region Events

        public event HandleClientConnection ClientConnected;
        public event HandleClientDisconnection ClientDisconnected;

        #endregion

        #region Properties

        public int PortNumber
        {
            get { return portNumber; }
            set 
            {
                if (portNumber != value)
                {
                    Shutdown();
                    portNumber = value;
                    Initialize();
                }
            }
        }

        public byte[] MyIPAddress
        {
            get { return myIPAddress; }
        }

        public int NumConnectedClients
        {
            get { return clients.Count; }
        }

        public List<String> ClientIPAddresses
        {
            get
            {
                List<String> ipAddresses = new List<string>();
                foreach (String ipAddress in clients.Keys)
                    ipAddresses.Add(ipAddress);

                return ipAddresses;
            }
        }

        public bool EnableEncryption
        {
            get { return enableEncryption; }
            set { enableEncryption = value; }
        }

        #endregion 

        #region Constructors
        /// <summary>
        /// Creates a Lidgren network server with an application name and the port number
        /// to establish the connection.
        /// </summary>
        /// <param name="appName">An application name. Can be any names</param>
        /// <param name="portNumber">The port number to establish the connection</param>
        public LidgrenServer(String appName, int portNumber)
        {
            this.portNumber = portNumber;
            this.appName = appName;
            enableEncryption = false;
            IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress addr = ipEntry.AddressList[0];
            myIPAddress = addr.GetAddressBytes();
            approveList = new Dictionary<string, string>();
            prevSender = null;
            clients = new Dictionary<string, NetConnection>();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            // Create a net configuration
            netConfig = new NetConfiguration(appName);
            netConfig.MaxConnections = 32;
            netConfig.Port = portNumber;

            // enable encryption; this key was generated using the 'GenerateEncryptionKeys' application
            if (enableEncryption)
            {
                // No encryption mechanism for latest Lidgren library
            }

            // Create a server
            netServer = new NetServer(netConfig);
            netServer.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
            netServer.Start();

            buffer = netServer.CreateBuffer();
        }

        public void BroadcastMessage(byte[] msg, bool reliable, bool inOrder, bool excludeSender)
        {
            // Test if any connections have been made to this machine, then send data
            if (clients.Count > 0)
            {
                // create new message to send to all clients
                NetBuffer buf = netServer.CreateBuffer();
                buf.Write(msg);

                //Log.Write("Sending message: " + msg.ToString(), Log.LogLevel.Log);
                //Console.WriteLine("Sending message: " + ByteHelper.ConvertToString(msg));

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

                // broadcast the message in order
                if (excludeSender && (prevSender != null))
                {
                    // if there is only one connection, then the sender to be excluded is
                    // the only connection the server has, so there is no point to broadcast
                    if (clients.Count > 1)
                        netServer.SendToAll(buf, channel, prevSender);
                }
                else
                    netServer.SendToAll(buf, channel);
            }
        }

        public void SendMessage(byte[] msg, List<String> ipAddresses, bool reliable, bool inOrder)
        {
            // Test if any connections have been made to this machine, then send data
            if (clients.Count > 0)
            {
                // create new message to send to all clients
                NetBuffer buf = netServer.CreateBuffer();
                buf.Write(msg);

                //Log.Write("Sending message: " + msg.ToString(), Log.LogLevel.Log);
                //Console.WriteLine("Sending message: " + ByteHelper.ConvertToString(msg));

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

                List<NetConnection> recipients = new List<NetConnection>();
                foreach (String ipAddress in ipAddresses)
                    if (clients.ContainsKey(ipAddress))
                        recipients.Add(clients[ipAddress]);

                if (recipients.Count > 0)
                    netServer.SendMessage(buf, recipients, channel);
            }
        }

        public List<byte[]> ReceiveMessage()
        {
            List<byte[]> messages = new List<byte[]>();

            NetMessageType type;
            NetConnection sender;

            // read a packet if available
            while (netServer.ReadMessage(buffer, out type, out sender))
            {
                switch (type)
                {
                    case NetMessageType.DebugMessage:
                        Log.Write(buffer.ReadString(), Log.LogLevel.Log);
                        break;
                    case NetMessageType.ConnectionApproval:
                        if (!approveList.ContainsKey(sender.RemoteEndpoint.ToString()))
                        {
                            Log.Write("Connection request from IP address: " + sender.RemoteEndpoint.ToString(),
                                Log.LogLevel.Log);
                            sender.Approve();
                            approveList.Add(sender.RemoteEndpoint.ToString(), "");
                        }
                        break;
                    case NetMessageType.StatusChanged:
                        Log.Write("New status for " + sender + ": " + sender.Status + 
                            " (" + buffer.ReadString() + ")", Log.LogLevel.Log);
                        if (sender.Status == NetConnectionStatus.Connected)
                        {
                            byte[] data = ByteHelper.ConvertToByte("NewConnectionEstablished");
                            byte[] size = BitConverter.GetBytes((short)data.Length);
                            messages.Add(ByteHelper.ConcatenateBytes(size, data));
                            clients.Add(sender.RemoteEndpoint.ToString(), sender);
                            approveList.Remove(sender.RemoteEndpoint.ToString());
                            if (sender != null)
                                prevSender = clients[sender.RemoteEndpoint.ToString()];
                            if (ClientConnected != null)
                                ClientConnected(sender.RemoteEndpoint.ToString());
                        }
                        else if (sender.Status == NetConnectionStatus.Disconnected)
                        {
                            clients.Remove(sender.RemoteEndpoint.ToString());
                            if (ClientDisconnected != null)
                                ClientDisconnected(sender.RemoteEndpoint.ToString());
                        }

                        break;
                    case NetMessageType.Data:
                        messages.Add(buffer.ToArray());
                        if (sender != null)
                            prevSender = clients[sender.RemoteEndpoint.ToString()];
                        break;
                }
            }

            return messages;
        }

        public void Shutdown()
        {
            // shutdown; sends disconnect to all connected clients with this reason string
            netServer.Shutdown("Application exiting");
        }

        #endregion
    }
}
