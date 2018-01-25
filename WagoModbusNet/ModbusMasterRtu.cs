﻿/*
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
using System.IO.Ports;
using System.Text;

namespace WagoModbusNet
{
    public class ModbusMasterRtu : ModbusMaster
    {

        public ModbusMasterRtu()
        {
        }

        private SerialPort _serialPort;             // The serial interface instance
        private string _portName = "COM1";  // Name of serial interface like "COM23" 
        public string Portname
        {
            get { return _portName; }
            set { _portName = value; }
        }

        private int _baudrate = 9600;
        public int Baudrate
        {
            get { return _baudrate; }
            set { _baudrate = value; }
        }
        private int _databits = 8;
        public int Databits
        {
            get { return _databits; }
            set { _databits = value; }
        }
        private Parity _parity = Parity.None;
        public Parity Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }
        private StopBits _stopbits = StopBits.One;
        public StopBits StopBits
        {
            get { return _stopbits; }
            set { _stopbits = value; }
        }
        private Handshake _handshake = Handshake.None;
        public Handshake Handshake
        {
            get { return _handshake; }
            set { _handshake = value; }
        }

        // Receive response helpers        
        private byte[] _responseBuffer = new byte[512]; // TODO: inspect implementation. Should not keep around a buffer as a field.
        private int _responseBufferLength;

        protected bool _connected;
        public override bool Connected
        {
            get { return _connected; }
            set
            {
                if (value)
                {
                    _connected = (Connect().Value == 0) ? true : false;
                }
                else
                {
                    Disconnect();
                }
            }
        }

        public override wmnRet Connect()
        {
            if (_connected)
                Disconnect();
            try
            {
                //Create instance
                _serialPort = new SerialPort(_portName, _baudrate, _parity, _databits, _stopbits);
                _serialPort.Handshake = _handshake;
                _serialPort.WriteTimeout = _timeout;
                _serialPort.ReadTimeout = _timeout;
            }
            catch (Exception e)
            {
                // Could not create instance of SerialPort class
                return new wmnRet(-300, "NetException: " + e.Message);
            }
            try
            {
                _serialPort.Open();
            }
            catch (Exception e)
            {
                // Could not open serial port
                return new wmnRet(-300, "NetException: " + e.Message);
            }
            _connected = true;
            return new wmnRet(0, "Successful executed");
        }


        public virtual wmnRet Connect(string portname, int baudrate, Parity parity, int databits, StopBits stopbits, Handshake handshake)
        {
            //Copy settings into private members
            _portName = portname;
            _baudrate = baudrate;
            _parity = parity;
            _databits = databits;
            _stopbits = stopbits;
            _handshake = handshake;
            //Create instance
            return this.Connect();
        }

        public override void Disconnect()
        {
            if (_serialPort != null)
            {
                _serialPort.Close();
                _serialPort = null;
            }
            _connected = false;
        }

        // Send request and and wait for response 
        protected override byte[] Query(byte[] requestADU)
        {
            byte[] responsePDU = null;
            if (!_connected)
                throw new NotConnectedException();

            // Send Request( synchron ) 
            _serialPort.Write(requestADU, 0, requestADU.Length);
            _responseBuffer.Initialize();
            _responseBufferLength = 0;
            _serialPort.ReadTimeout = _timeout;
            int tmpTimeout = 50; // 50 ms
            if (_baudrate < 9600)
            {
                tmpTimeout = (int)((10000 / _baudrate) + 50);
            }
            wmnRet ret;
            try
            {
                // Read all data until a timeout exception is arrived
                do
                {
                    _responseBuffer[_responseBufferLength] = (byte)_serialPort.ReadByte();
                    _responseBufferLength++;
                    // Change receive timeout after first received byte
                    if (_serialPort.ReadTimeout != tmpTimeout)
                    {
                        _serialPort.ReadTimeout = tmpTimeout;
                    }
                }
                while (true);
            }
            catch (TimeoutException)
            {
                ; // Thats what we are waiting for to know "All data received" 
            }
            finally
            {
                // Check Response
                if (_responseBufferLength == 0)
                {
                    throw new TimeoutException(); // TODO: This is a direct replacement. It may not be the appropriate Exception.
                }
                else
                {
                    ret = CheckResponse(_responseBuffer, _responseBufferLength, out responsePDU);
                }
            }

            return responsePDU;
        }

        // TODO: refactor
        protected virtual wmnRet CheckResponse(byte[] respRaw, int respRawLength, out byte[] respPdu)
        {
            respPdu = null;
            // Check minimal response length 
            if (respRawLength < 5)
            {
                return new wmnRet(-500, "Error: Invalid response telegram, do not receive minimal length of 5 byte");
            }
            // Is response a "modbus exception response"
            if ((respRaw[1] & 0x80) > 0)
            {
                return new wmnRet((int)respRaw[2], "Modbus exception received: " + ((ModbusExceptionCodes)respRaw[2]).ToString());
            }
            // Check CRC
            byte[] crc16 = CRC16.CalcCRC16(respRaw, respRawLength - 2);
            if ((respRaw[respRawLength - 2] != crc16[0]) | (respRaw[respRawLength - 1] != crc16[1]))
            {
                return new wmnRet(-501, "Error: Invalid response telegram, CRC16-check failed");
            }
            // Strip ADU header and copy response PDU into output buffer 
            respPdu = new byte[respRawLength - 2];
            for (int i = 0; i < respRawLength - 2; i++)
            {
                respPdu[i] = respRaw[i];
            }
            return new wmnRet(0, "Successful executed");
        }

        protected override byte[] BuildRequestAdu(byte[] requestPDU)
        {
            byte[] requestADU = new byte[requestPDU.Length + 2];  // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusRTU
            // Copy request PDU
            for (int i = 0; i < requestPDU.Length; i++)
            {
                requestADU[i] = requestPDU[i];
            }
            // Calc CRC16
            byte[] crc16 = CRC16.CalcCRC16(requestADU, requestADU.Length - 2);
            // Append CRC
            requestADU[requestADU.Length - 2] = crc16[0];
            requestADU[requestADU.Length - 1] = crc16[1];

            return requestADU;
        }
    }
}
