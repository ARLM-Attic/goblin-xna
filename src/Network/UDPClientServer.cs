using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using GoblinXNA.Helpers;

namespace GoblinXNA.Network
{
    public delegate void ReceiveMessage(object sender, UdpPacketReceivedEventArgs e);
    
    public class UDPClientServer
    {
        #region Member Fields

        private UdpAnySourceMulticastChannel channel;
        private String hostName;
        private int port;
        private int identifier;

        event ReceiveMessage messageReceived;

        #endregion

        #region Constructors

        public UDPClientServer(String _hostName, int _port)
        {
            identifier = new Random().Next(int.MaxValue);
            hostName = _hostName;
            port = _port;

            this.channel = new UdpAnySourceMulticastChannel(IPAddress.Parse(hostName), port);
            this.channel.PacketReceived += new EventHandler<UdpPacketReceivedEventArgs>(messageReceived);
            this.channel.Open();
        }

        public UDPClientServer()
        {
            identifier = new Random().Next(int.MaxValue);
            hostName = @"224.109.108.107";
            port = 3007;

            this.channel = new UdpAnySourceMulticastChannel(IPAddress.Parse(hostName), port);
            this.channel.PacketReceived += new EventHandler<UdpPacketReceivedEventArgs>(messageReceived);
            this.channel.Open();
        }
        #endregion

        #region Properties

        public UdpAnySourceMulticastChannel Channel
        {
            get { return channel; }
            set { this.channel = value; }
        }

        public String HostName
        {
            get { return hostName; }
            set { hostName = value; }
        }
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        public int Identifier
        {
            get { return identifier; }
            set { identifier = value; }
        }

        #endregion

        #region Public Methods

        public void SendMessage(String dataToSend)
        {
            this.channel.Send(dataToSend);
        }

        public void BroadcastMessage(IPEndPoint ipAddress, String dataToSend)
        {
            this.channel.SendTo(ipAddress, dataToSend);
        }

        /// <summary>
        /// Shuts down the client.
        /// </summary>
        void Shutdown()
        {
            this.channel.Close();
        }
        #endregion
    }

    public class UdpPacketReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public IPEndPoint Source { get; set; }

        public UdpPacketReceivedEventArgs(byte[] data, IPEndPoint source)
        {
            this.Message = Encoding.UTF8.GetString(data, 0, data.Length);
            this.Source = source;
        }
    }

    public class UdpAnySourceMulticastChannel : IDisposable
    {
        /// <summary>
        /// Occurs when [packet received].
        /// </summary>
        public event EventHandler<UdpPacketReceivedEventArgs> PacketReceived;
        /// <summary>
        /// Occurs when [after open].
        /// </summary>
        public event EventHandler AfterOpen;
        /// <summary>
        /// Occurs when [before close].
        /// </summary>
        public event EventHandler BeforeClose;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed { get; private set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is joined.
        /// </summary>
        /// <value><c>true</c> if this instance is joined; otherwise, <c>false</c>.</value>
        public static bool IsJoined;
        /// <summary>
        /// Gets or sets the size of the max message.
        /// </summary>
        /// <value>The size of the max message.</value>
        private byte[] ReceiveBuffer { get; set; }
        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>The client.</value>
        private UdpAnySourceMulticastClient Client { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAnySourceMulticastchannel"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        public UdpAnySourceMulticastChannel(IPAddress address, int port)
            : this(address, port, 1024)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpAnySourceMulticastchannel"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="port">The port.</param>
        /// <param name="maxMessageSize">Size of the max message.</param>
        public UdpAnySourceMulticastChannel(IPAddress address, int port, int maxMessageSize)
        {
            this.ReceiveBuffer = new byte[maxMessageSize];
            this.Client = new UdpAnySourceMulticastClient(address, port);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                this.IsDisposed = true;

                if (this.Client != null)
                    this.Client.Dispose();
            }
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            if (!IsJoined)
            {
                this.Client.BeginJoinGroup(
                    result =>
                    {
                        try
                        {
                            this.Client.EndJoinGroup(result);
                            IsJoined = true;

                            this.OnAfterOpen();
                            this.Receive();
                        }
                        catch
                        { }
                    }, null);
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            this.OnBeforeClose();
            IsJoined = false;
            this.Dispose();
        }

        /// <summary>
        /// Sends the specified format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void Send(string format, params object[] args)
        {
            if (IsJoined)
            {
                byte[] data = Encoding.UTF8.GetBytes(string.Format(format, args));

                this.Client.BeginSendToGroup(data, 0, data.Length,
                    result =>
                    {
                        this.Client.EndSendToGroup(result);
                    }, null);
            }
        }

        /// <summary>
        /// Sends the specified format.
        /// </summary>
        /// /// <param name="format">The destination.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void SendTo(IPEndPoint endPoint, string format, params object[] args)
        {
            if (IsJoined)
            {
                byte[] data = Encoding.UTF8.GetBytes(string.Format(format, args));

                this.Client.BeginSendTo(data, 0, data.Length, endPoint,
                    result =>
                    {
                        this.Client.EndSendToGroup(result);
                    }, null);
            }
        }

        /// <summary>
        /// Receives this instance.
        /// </summary>
        private void Receive()
        {
            if (IsJoined)
            {
                Array.Clear(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length);

                this.Client.BeginReceiveFromGroup(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length,
                    result =>
                    {
                        if (!IsDisposed)
                        {
                            IPEndPoint source;

                            try
                            {
                                this.Client.EndReceiveFromGroup(result, out source);
                                this.OnReceive(source, this.ReceiveBuffer);
                                this.Receive();
                            }
                            catch
                            {
                                IsJoined = false;
                                this.Open();
                            }

                        }
                    }, null);
            }
        }

        /// <summary>
        /// Called when [receive].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="data">The data.</param>
        private void OnReceive(IPEndPoint source, byte[] data)
        {
            EventHandler<UdpPacketReceivedEventArgs> handler = this.PacketReceived;

            if (handler != null)
                handler(this, new UdpPacketReceivedEventArgs(data, source));
        }

        /// <summary>
        /// Called when [after open].
        /// </summary>
        private void OnAfterOpen()
        {
            EventHandler handler = this.AfterOpen;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when [before close].
        /// </summary>
        private void OnBeforeClose()
        {
            EventHandler handler = this.BeforeClose;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
