/*
Description:    
    WagoModbusNet provide easy to use Modbus-Master classes for TCP, UDP, RTU and ASCII.
    WagoModbusNet based on dot.net framework 2.0.
    WagoModbusNet.Masters do not throw any exception, all function returns a struct of type 'wmnRet'.
    For a list of supported function codes see 'enum ModbusFunctionCodes'.    
  
Version: 1.0.1.0 (09.01.2013)
   
Author: WAGO Kontakttechnik GmbH & Co.KG
  
Contact: support@wago.com
 
Typical pitfal:
    You dial with a WAGO ethernet controller. Try to set outputs - but nothing happens!
    WAGO ethernet controller provide a "owner" policy for physical outputs.
    The "owner" could be CoDeSys-Runtime or Fieldbus-Master.
    Every time you download a PLC program the CoDeSys-Runtime becomes "owner" of physical outputs.
    Use tool "Ethernet-Settings.exe" and "format" and "extract" filesystem is easiest way to assign Modbus-Master as "owner".
    Alternativly you can "Login" with CoDeSys-IDE and perform "Reset(original)".
     
License:
    Copyright (c) WAGO Kontakttechnik GmbH & Co.KG 2013 

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
            this.Hostname = hostname;
        }

        public ModbusMasterTcp(string hostname, int port)
            : this()
        {
            this.Hostname = hostname;
            _port = port;
        }

        public override wmnRet Connect()
        {
            if (_connected)
                Disconnect();

            // Create client socket
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, _timeout);
            _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _timeout);
            // Reset timer
            _mreConnectTimeout.Reset();
            try
            {
                // Call async Connect 
                _sock.BeginConnect(new IPEndPoint(_ip, _port), new AsyncCallback(OnConnect), _sock);
                // Stay here until connection established or timeout expires
                if (_mreConnectTimeout.WaitOne(_timeout, false))
                {
                    // Successful connected
                    _connected = true;
                    return new wmnRet(0, "Successful executed");
                }
                else
                {
                    // Timeout expired 
                    _connected = false;
                    _sock.Close(); // Implizit .EndConnect free ressources 
                    _sock = null;
                    return new wmnRet(-101, "TIMEOUT-ERROR: Timeout expired while 'Try to connect ...'");
                }
            }
            catch (Exception e)
            {
                return new wmnRet(-300, "NetException: " + e.Message);
            }
        }

        private ManualResetEvent _mreConnectTimeout = new ManualResetEvent(false);

        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                Socket s = ar.AsyncState as Socket;
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

        public override wmnRet Connect(string hostname)
        {
            this.Hostname = hostname;
            return Connect();
        }

        public override wmnRet Connect(string hostname, int port)
        {
            this.Hostname = hostname;
            _port = port;
            return Connect();
        }

        public override void Disconnect()
        {
            //Close socket and free ressources 
            if (_sock != null)
            {
                _sock.Close();
                _sock = null;
            }
            _connected = false;
        }

        // Send request and and wait for response
        protected override wmnRet Query(byte[] reqAdu, out byte[] respPdu)
        {
            respPdu = null;  //Assign null to make compiler silent
            if (_ip == null)
            {
                return new wmnRet(-301, "DNS error: Could not resolve Ip-Address for " + _hostname);
            }
            try
            {
                if (!_connected && _autoConnect)
                {
                    Connect();
                }
                if (!_connected)
                {
                    return new wmnRet(-500, "Error: 'Not connected, call Connect()' ");
                }
                // Send request sync
                _sock.Send(reqAdu, 0, reqAdu.Length, SocketFlags.None);

                byte[] tmpBuf = new byte[255]; //Receive buffer

                // Try to receive response 
                int byteCount = _sock.Receive(tmpBuf, 0, tmpBuf.Length, SocketFlags.None);

                return CheckResponse(tmpBuf, byteCount, out respPdu);
            }
            catch (Exception e)
            {
                return new wmnRet(-300, "NetException: " + e.Message);
            }
            finally
            {
                if (_autoConnect)
                {
                    Disconnect();
                }
            }
        }
    }
}
