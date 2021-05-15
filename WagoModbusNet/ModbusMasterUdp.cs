using System;
using System.Net;
using System.Net.Sockets;

namespace WagoModbusNet
{
    public class ModbusMasterUdp : ModbusMaster
    {
        public ModbusMasterUdp(string hostname)
            => Hostname = hostname;

        public ModbusMasterUdp(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }

        private static ushort _transactionId = 4711;
        private ushort TransactionId => ++_transactionId;

        private string _hostname = "";
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                _hostname = value;
                if (IPAddress.TryParse(value, out _ip) == false)
                {
                    /*//Sync name resolving would block up to 5 seconds
                     * IPHostEntry hst = Dns.GetHostEntry(value);
                     *_ip = hst.AddressList[0];
                     */
                    //Async name resolving will not block but needs also up to 5 seconds until it returns 
                    IAsyncResult asyncResult = Dns.BeginGetHostEntry(value, null, null);
                    asyncResult.AsyncWaitHandle.WaitOne(); // Wait until job is done - No chance to cancel request
                    IPHostEntry ipHostEntry = null;
                    try
                    {
                        ipHostEntry = Dns.EndGetHostEntry(asyncResult); //EndGetHostEntry will wait for you if calling before job is done 
                    }
                    catch { }

                    if (ipHostEntry != null)
                        _ip = ipHostEntry.AddressList[0];
                }
            }
        }

        public int Port { get; protected set; } = 502;

        public bool AutoConnect { get; protected set; }

        protected Socket Socket;
        protected IPAddress _ip = null;

        public override void Connect()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, Timeout);
            Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);
        }

        public virtual void Connect(string hostname)
        {
            Hostname = hostname;
            Connect();
        }

        public virtual void Connect(string hostname, int port)
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
        }

        // Send request and and wait for response
        protected override byte[] Query(byte[] requestAdu)
        {
            byte[] responsePdu = null;
            if (_ip == null) // TODO: remove check
            {
                // return new wmnRet(-301, "DNS error: Could not resolve Ip-Address for " + _hostname);
                // TODO: Since IP is created from _hostname, the exception should be thrown when _ip is assigned
            }
            if (!Connected)
                Connect(); // Connect will succesful in any case because it just create a socket instance

            try
            {
                // Send Request( synchron )             
                IPEndPoint ipepRemote = new IPEndPoint(_ip, Port);
                Socket.SendTo(requestAdu, ipepRemote);

                byte[] receiveBuffer = new byte[255];
                // Remote EndPoint to capture the identity of responding host.                    
                EndPoint epRemote = (EndPoint)ipepRemote;

                int byteCount = Socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref epRemote);

                responsePdu = CheckResponse(receiveBuffer, byteCount);
            }
            finally
            {
                if (AutoConnect)
                    Disconnect();
            }

            return responsePdu;
        }

        protected virtual byte[] CheckResponse(byte[] response, int length)
        {
            if (length < 8)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, do not receive minimal length of 8 byte");
            
            ushort respPduLength = (ushort)(response[5] | (ushort)(response[4] << 8));
            
            if (length < respPduLength + 6)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, do not receive complied telegram");
            
            if (IsModbusException(response[7]))
                throw ModbusException.GetModbusException(response[8]);

            // Strip ADU header and copy response PDU into output buffer 
            byte[] responsePdu = new byte[respPduLength];
            for (int i = 0; i < respPduLength; i++)
                responsePdu[i] = response[6 + i];
            
            return responsePdu;
        }

        // Prepare request telegram
        protected override byte[] BuildRequestAdu(byte[] reqPdu)
        {
            // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusTCP
            byte[] reqAdu = new byte[6 + reqPdu.Length];
            byte[] help; // Used to convert ushort into bytes

            help = BitConverter.GetBytes(this.TransactionId);
            reqAdu[0] = help[1]; // Transaction-ID -Hi
            reqAdu[1] = help[0]; // Transaction-ID -Lo
            reqAdu[2] = 0x00; // Protocol-ID - allways zero
            reqAdu[3] = 0x00; // Protocol-ID - allways zero
            help = BitConverter.GetBytes(reqPdu.Length);
            reqAdu[4] = help[1]; // Number of bytes follows -Hi 
            reqAdu[5] = help[0]; // Number of bytes follows -Lo 
            // Copy request PDU
            for (int i = 0; i < reqPdu.Length; i++)
                reqAdu[6 + i] = reqPdu[i];

            return reqAdu;
        }
    }
}
