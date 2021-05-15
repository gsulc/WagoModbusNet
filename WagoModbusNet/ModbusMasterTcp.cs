using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WagoModbusNet
{
    public class ModbusMasterTcp : ModbusMasterUdp
    {
        public ModbusMasterTcp(string hostname)
            : base(hostname) { }

        public ModbusMasterTcp(string hostname, int port)
            : base(hostname, port) { }

        public override void Connect()
        {
            if (Connected)
                Disconnect();

            // Create client socket
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);
            
            _mreConnectTimeout.Reset();
            
            Socket.BeginConnect(new IPEndPoint(_ip, Port), new AsyncCallback(OnConnect), Socket);
            if (_mreConnectTimeout.WaitOne(Timeout, false))
            {
                Connected = true;
                return;
            }
            else // Timeout expired 
            {
                Connected = false;
                Socket.Close();
                Socket = null;
                throw new ConnectionTimeoutException();
            }
        }

        private ManualResetEvent _mreConnectTimeout = new ManualResetEvent(false);

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                var s = ar.AsyncState as Socket;
                if (s != null)
                    s.EndConnect(ar);
            }
            catch (Exception)
            {
            }
            finally
            {
                _mreConnectTimeout.Set(); // Wake up waiting threat to go further
            }
        }

        public override void Connect(string hostname)
        {
            Hostname = hostname;
            Connect();
        }

        public override void Connect(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
            Connect();
        }

        public override void Disconnect()
        {
            if (Socket != null)
            {
                Socket.Close();
                Socket = null;
            }
            Connected = false;
        }

        // Send request and and wait for response
        protected override byte[] Query(byte[] requestAdu) 
        {
            byte[] responsePdu = null;  //Assign null to make compiler silent
            if (_ip == null) // TODO: remove check
            {
                throw new IpDnsException(Hostname);
                // TODO: Since IP is created from Hostname, the exception should be thrown when _ip is assigned
            }
            try
            {
                if (!Connected && AutoConnect)
                    Connect();
                
                if (!Connected)
                    throw new NotConnectedException();

                // Send request sync
                Socket.Send(requestAdu, 0, requestAdu.Length, SocketFlags.None);

                byte[] receiveBuffer = new byte[255];
                int byteCount = Socket.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None);
                responsePdu = CheckResponse(receiveBuffer, byteCount);
            }
            finally
            {
                if (AutoConnect)
                    Disconnect();
            }

            return responsePdu;
        }
    }
}
