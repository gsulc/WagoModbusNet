using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WagoModbusNet
{
    public class ModbusMasterTcp : ModbusMasterUdp
    {

        public ModbusMasterTcp()
        {
        }

        public ModbusMasterTcp(string hostname)
            : this()
        {
            Hostname = hostname;
        }

        public ModbusMasterTcp(string hostname, int port)
            : this()
        {
            Hostname = hostname;
            _port = port;
        }

        public override void Connect()
        {
            if (_connected)
                Disconnect();

            // Create client socket
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
            // Reset timer
            _mreConnectTimeout.Reset();

            // Call async Connect 
            _socket.BeginConnect(new IPEndPoint(_ip, _port), new AsyncCallback(OnConnect), _socket);
            // Stay here until connection established or timeout expires
            if (_mreConnectTimeout.WaitOne(_timeout, false))
            {
                // Successful connected
                _connected = true;
                return;
            }
            else
            {
                // Timeout expired 
                _connected = false;
                _socket.Close(); // Implicit .EndConnect free ressources 
                _socket = null;
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
                {
                    s.EndConnect(ar);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                _mreConnectTimeout.Set();  //Wake up waiting threat to go further
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
            _port = port;
            Connect();
        }

        public override void Disconnect()
        {
            //Close socket and free ressources 
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
            _connected = false;
        }

        // Send request and and wait for response
        protected override byte[] Query(byte[] requestAdu) 
        {
            byte[] responsePdu = null;  //Assign null to make compiler silent
            if (_ip == null) // TODO: remove check
            {
                throw new IpDnsException(_hostname);
                // TODO: Since IP is created from _hostname, the exception should be thrown when _ip is assigned
            }
            try
            {
                if (!_connected && _autoConnect)
                    Connect();
                
                if (!_connected)
                    throw new NotConnectedException();

                // Send request sync
                _socket.Send(requestAdu, 0, requestAdu.Length, SocketFlags.None);

                byte[] receiveBuffer = new byte[255];
                int byteCount = _socket.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None);
                responsePdu = CheckResponse(receiveBuffer, byteCount);
            }
            finally
            {
                if (_autoConnect)
                    Disconnect();
            }

            return responsePdu;
        }
    }
}
