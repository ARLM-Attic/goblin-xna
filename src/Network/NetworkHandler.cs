using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GoblinXNA.Helpers;

namespace GoblinXNA.Network
{
    /// <summary>
    /// An implementation of the INetworkHandler interface.
    /// </summary>
    public class NetworkHandler : INetworkHandler
    {
        #region Member Fields

        protected IServer networkServer;
        protected IClient networkClient;

        protected List<byte[]> networkMessages;
        protected List<byte> reliableInOrderMsgs;
        protected List<byte> unreliableInOrderMsgs;
        protected List<byte> reliableUnOrderMsgs;
        protected List<byte> unreliableUnOrderMsgs;

        protected bool updating;

        /// <summary>
        /// A list of network objects that can be transferred over the network
        /// </summary>
        protected Dictionary<String, NetObj> networkObjects;

        #endregion

        #region Constructors

        public NetworkHandler()
        {
            networkObjects = new Dictionary<string, NetObj>();
            updating = false;

            networkMessages = new List<byte[]>();
            reliableInOrderMsgs = new List<byte>();
            unreliableInOrderMsgs = new List<byte>();
            reliableUnOrderMsgs = new List<byte>();
            unreliableUnOrderMsgs = new List<byte>();
        }

        #endregion

        #region Properties

        public virtual IServer NetworkServer
        {
            get { return networkServer; }
            set
            {
                if (networkServer != null)
                    networkServer.Shutdown();

                networkServer = value;
                networkServer.Initialize();
            }
        }
        
        public virtual IClient NetworkClient
        {
            get { return networkClient; }
            set
            {
                if (networkClient != null)
                    networkClient.Shutdown();

                networkClient = value;
                networkClient.Connect();
            }
        }

        #endregion

        #region Public Methods
        
        public virtual void AddNetworkObject(INetworkObject networkObj)
        {
            // busy wait while the network handler is being updated
            while (updating) { }
            if (!networkObjects.ContainsKey(networkObj.Identifier))
                networkObjects.Add(networkObj.Identifier, new NetObj(networkObj));
        }

        public virtual void RemoveNetworkObject(INetworkObject networkObj)
        {
            // busy wait while the network handler is being updated
            while (updating) { }
            networkObjects.Remove(networkObj.Identifier);
        }

        public virtual void Dispose()
        {
            if (networkServer != null)
                networkServer.Shutdown();
            if (networkClient != null)
                networkClient.Shutdown();

            networkObjects.Clear();
            networkMessages.Clear();
        }

        public virtual void Update(float elapsedMsecs)
        {
            networkMessages.Clear();
            bool sendAll = false;
            if (State.IsServer)
                networkServer.ReceiveMessage(ref networkMessages);
            else
                networkClient.ReceiveMessage(ref networkMessages);

            String identifier = "";
            String[] splits = null;
            char[] seps = { ':' };
            byte[] inputData = null;
            byte[] data = null;
            short size = 0;
            int index = 0;
            foreach (byte[] msg in networkMessages)
            {
                index = 0;
                while (index < msg.Length)
                {
                    size = ByteHelper.ConvertToShort(msg, index);
                    data = ByteHelper.Truncate(msg, index + 2, size);
                    //Console.WriteLine("Received: " + ByteHelper.ConvertToString(data));
                    splits = ByteHelper.ConvertToString(data).Split(seps);
                    identifier = splits[0];
                    if ((data.Length - identifier.Length) > 0)
                        inputData = ByteHelper.Truncate(data, identifier.Length + 1,
                            data.Length - identifier.Length - 1);

                    if (networkObjects.ContainsKey(identifier))
                        networkObjects[identifier].NetworkObject.InterpretMessage(inputData);
                    else if (identifier.Equals("NewConnectionEstablished"))
                        sendAll = true;
                    else
                        Log.Write("Network Identifier: " + identifier + " is not found", Log.LogLevel.Log);

                    index += (size + 2);
                }

                // If we're server, then broadcast the message received from the client to
                // all of the connected clients except the client which sent the message
                //if (State.IsServer)
                //    networkServer.BroadcastMessage(msg, true, true, true);
            }

            updating = true;

            foreach (NetObj netObj in networkObjects.Values)
                if (!netObj.NetworkObject.Hold)
                    netObj.TimeElapsedSinceLastTransmit += elapsedMsecs;

            reliableInOrderMsgs.Clear();
            unreliableInOrderMsgs.Clear();
            reliableUnOrderMsgs.Clear();
            unreliableUnOrderMsgs.Clear();
            List<byte> msgs = new List<byte>();

            if (State.IsServer)
            {
                if (sendAll)
                {
                    foreach (NetObj netObj in networkObjects.Values)
                    {
                        if (!netObj.NetworkObject.Hold)
                            AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);
                    }
                }
                else
                {
                    if (networkServer.NumConnectedClients >= State.NumberOfClientsToWait)
                    {
                        foreach (NetObj netObj in networkObjects.Values)
                            if (!netObj.NetworkObject.Hold &&
                                (netObj.NetworkObject.ReadyToSend || netObj.IsTimeToTransmit))
                            {
                                AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                    unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);

                                netObj.NetworkObject.ReadyToSend = false;
                                netObj.TimeElapsedSinceLastTransmit = 0;
                            }
                    }
                }

                if (reliableInOrderMsgs.Count > 0)
                    networkServer.BroadcastMessage(reliableInOrderMsgs.ToArray(), true, true, false);
                if (reliableUnOrderMsgs.Count > 0)
                    networkServer.BroadcastMessage(reliableUnOrderMsgs.ToArray(), true, false, false);
                if (unreliableInOrderMsgs.Count > 0)
                    networkServer.BroadcastMessage(unreliableInOrderMsgs.ToArray(), false, true, false);
                if (unreliableUnOrderMsgs.Count > 0)
                    networkServer.BroadcastMessage(unreliableUnOrderMsgs.ToArray(), false, false, false);
            }
            else
            {
                if (networkClient.IsConnected)
                {
                    foreach (NetObj netObj in networkObjects.Values)
                        if (!netObj.NetworkObject.Hold &&
                            (netObj.NetworkObject.ReadyToSend || netObj.IsTimeToTransmit))
                        {
                            AddNetMessage(msgs, reliableInOrderMsgs, reliableUnOrderMsgs,
                                unreliableInOrderMsgs, unreliableUnOrderMsgs, netObj.NetworkObject);

                            netObj.NetworkObject.ReadyToSend = false;
                            netObj.TimeElapsedSinceLastTransmit = 0;
                        }

                    if (reliableInOrderMsgs.Count > 0)
                        networkClient.SendMessage(reliableInOrderMsgs.ToArray(), true, true);
                    if (reliableUnOrderMsgs.Count > 0)
                        networkClient.SendMessage(reliableUnOrderMsgs.ToArray(), true, false);
                    if (unreliableInOrderMsgs.Count > 0)
                        networkClient.SendMessage(unreliableInOrderMsgs.ToArray(), false, true);
                    if (unreliableUnOrderMsgs.Count > 0)
                        networkClient.SendMessage(unreliableUnOrderMsgs.ToArray(), false, false);
                }
            }

            updating = false;
        }

        #endregion

        #region Protected Methods

        protected virtual void AddNetMessage(List<byte> msgs, List<byte> riMsgs, List<byte> ruMsgs,
            List<byte> uriMsgs, List<byte> uruMsgs, INetworkObject networkObj)
        {
            byte[] id = ByteHelper.ConvertToByte(networkObj.Identifier + ":");
            byte[] data = networkObj.GetMessage();
            short size = (short)(id.Length + data.Length);

            msgs.AddRange(BitConverter.GetBytes(size));
            msgs.AddRange(id);
            msgs.AddRange(data);

            if (networkObj.Reliable)
            {
                if (networkObj.Ordered)
                    riMsgs.AddRange(msgs);
                else
                    ruMsgs.AddRange(msgs);
            }
            else
            {
                if (networkObj.Ordered)
                    uriMsgs.AddRange(msgs);
                else
                    uruMsgs.AddRange(msgs);
            }

            msgs.Clear();
        }

        #endregion

        #region Protected Classes
        protected class NetObj
        {
            private INetworkObject networkObject;
            private float timeElapsedSinceLastTransmit;
            private float transmitSpan;

            public NetObj(INetworkObject netObj)
            {
                this.networkObject = netObj;
                timeElapsedSinceLastTransmit = 0;
                if (networkObject.SendFrequencyInHertz != 0)
                    transmitSpan = 1000 / (float)networkObject.SendFrequencyInHertz;
                else
                    transmitSpan = float.MaxValue;
            }

            public INetworkObject NetworkObject
            {
                get { return networkObject; }
            }

            public float TimeElapsedSinceLastTransmit
            {
                get { return timeElapsedSinceLastTransmit; }
                set { timeElapsedSinceLastTransmit = value; }
            }

            public bool IsTimeToTransmit
            {
                get { return (timeElapsedSinceLastTransmit >= transmitSpan); }
            }
        }
        #endregion
    }
}
