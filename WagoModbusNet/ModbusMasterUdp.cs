/* 
License:
    Copyright (c) WAGO Kontakttechnik GmbH & Co.KG 2013 
    Copyright (c) Gordon Sulc 2018

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
    and associated documentation files (the "Software"), to deal in the Software without restriction, 
    including without limitation the rights to use, copy, modify, merge, publish, distribute, 
    sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial 
    portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
    NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
    IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
    SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WagoModbusNet
{
    public class ModbusMasterUdp : ModbusMaster
    {
        private static ushort _transactionId = 4711;
        private ushort TransactionId
        {
            get 
            {
                return ++_transactionId; 
            }
        }

        protected string _hostname = "";
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
                    {
                        _ip = ipHostEntry.AddressList[0];
                    }
                }
            }
        }

        protected int _port = 502;
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        protected bool _autoConnect;
        public bool AutoConnect
        {
            get { return _autoConnect; }
            set { _autoConnect = value; }
        }

        public ModbusMasterUdp()
        {
        }

        public ModbusMasterUdp(string hostname)
            : this()
        {
            this.Hostname = hostname;
        }

        public ModbusMasterUdp(string hostname, int port)
            : this()
        {
            this.Hostname = hostname;
            this.Port = port;
        }

        protected bool _connected;
        public override bool Connected
        {
            get { return _connected; }
        }

        public override void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
        }

        public virtual void Connect(string hostname)
        {
            this.Hostname = hostname;
            Connect();
        }

        public virtual void Connect(string hostname, int port)
        {
            this.Hostname = hostname;
            _port = port;
            Connect();
        }

        public override void Disconnect()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }

        }
        protected Socket _socket;
        protected IPAddress _ip = null;

        // Send request and and wait for response
        protected override byte[] Query(byte[] requestAdu)
        {
            byte[] responsePdu = null;
            if (_ip == null) // TODO: remove check
            {
                // return new wmnRet(-301, "DNS error: Could not resolve Ip-Address for " + _hostname);
                // TODO: Since IP is created from _hostname, the exception should be thrown when _ip is assigned
            }
            if (!_connected)
                Connect(); // Connect will succesful in any case because it just create a socket instance

            try
            {
                // Send Request( synchron )             
                IPEndPoint ipepRemote = new IPEndPoint(_ip, _port);
                _socket.SendTo(requestAdu, ipepRemote);

                byte[] receiveBuffer = new byte[255];
                // Remote EndPoint to capture the identity of responding host.                    
                EndPoint epRemote = (EndPoint)ipepRemote;

                int byteCount = _socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref epRemote);

                responsePdu = CheckResponse(receiveBuffer, byteCount);
            }
            finally
            {
                if (_autoConnect)
                    Disconnect();
            }

            return responsePdu;
        }

        protected virtual byte[] CheckResponse(byte[] respRaw, int respRawLength)
        {
            byte[] responsePdu = null;
            // Check minimal response length of 8 byte
            if (respRawLength < 8)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, do not receive minimal length of 8 byte");
            
            //Decode act telegram lengh
            ushort respPduLength = (ushort)((ushort)respRaw[5] | (ushort)((ushort)(respRaw[4] << 8)));
            // Check all bytes received 
            if (respRawLength < respPduLength + 6)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, do not receive complied telegram");
            
            // Is response a "modbus exception response"
            if ((respRaw[7] & 0x80) > 0)
                throw ModbusException.GetModbusException(respRaw[8]);

            // Strip ADU header and copy response PDU into output buffer 
            responsePdu = new byte[respPduLength];
            for (int i = 0; i < respPduLength; i++)
            {
                responsePdu[i] = respRaw[6 + i];
            }
            return responsePdu;
        }

        // Prepare request telegram
        protected override byte[] BuildRequestAdu(byte[] reqPdu)
        {
            byte[] reqAdu = new byte[6 + reqPdu.Length]; // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusTCP
            byte[] help; // Used to convert ushort into bytes

            help = BitConverter.GetBytes(this.TransactionId);
            reqAdu[0] = help[1];						// Transaction-ID -Hi
            reqAdu[1] = help[0];						// Transaction-ID -Lo
            reqAdu[2] = 0x00;						    // Protocol-ID - allways zero
            reqAdu[3] = 0x00;						    // Protocol-ID - allways zero
            help = BitConverter.GetBytes(reqPdu.Length);
            reqAdu[4] = help[1];						// Number of bytes follows -Hi 
            reqAdu[5] = help[0];						// Number of bytes follows -Lo 
            // Copy request PDU
            for (int i = 0; i < reqPdu.Length; i++)
            {
                reqAdu[6 + i] = reqPdu[i];
            }

            return reqAdu;
        }
    }
}
