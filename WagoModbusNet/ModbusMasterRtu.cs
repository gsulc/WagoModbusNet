﻿/* 
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

        public string Portname { get; set; }
        public int Baudrate { get; set; } = 9600;
        public int Databits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;

        // Receive response helpers        
        private byte[] _responseBuffer = new byte[512]; // TODO: inspect implementation. Should not keep around a buffer as a field.
        private int _responseBufferLength;

        protected bool _connected;
        public override bool Connected
        {
            get { return _connected; }
        }

        public override void Connect()
        {
            if (_connected)
                Disconnect();

            _serialPort = new SerialPort(Portname, Baudrate, Parity, Databits, StopBits);
            _serialPort.Handshake = Handshake;
            _serialPort.WriteTimeout = _timeout;
            _serialPort.ReadTimeout = _timeout;
            _serialPort.Open();
            _connected = true;
        }


        public virtual void Connect(string portname, int baudrate, Parity parity, int databits, StopBits stopbits, Handshake handshake)
        {
            Portname = portname;
            Baudrate = baudrate;
            Parity = parity;
            Databits = databits;
            StopBits = stopbits;
            Handshake = handshake;
            
            Connect();
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
        protected override byte[] Query(byte[] requestAdu)
        {
            byte[] responsePdu = null;
            if (!_connected)
                throw new NotConnectedException();

            // Send Request( synchron ) 
            _serialPort.Write(requestAdu, 0, requestAdu.Length);
            _responseBuffer.Initialize();
            _responseBufferLength = 0;
            _serialPort.ReadTimeout = _timeout;
            int tmpTimeout = 50; // milliseconds
            if (Baudrate < 9600)
                tmpTimeout = (int)((10000 / Baudrate) + 50);

            try
            {
                // Read all data until a timeout exception is arrived
                do
                {
                    _responseBuffer[_responseBufferLength] = (byte)_serialPort.ReadByte();
                    _responseBufferLength++;
                    // Change receive timeout after first received byte
                    if (_serialPort.ReadTimeout != tmpTimeout)
                        _serialPort.ReadTimeout = tmpTimeout;
                } while (true);
            }
            catch (TimeoutException)
            {
                ; // Expected exception to know "All data received" 
            }
            finally
            {
                // Check Response
                if (_responseBufferLength == 0)
                    throw new TimeoutException("Timeout error: Did not receive response whitin specified 'Timeout'."); // TODO: This is a direct replacement. It may not be the appropriate Exception type.
                else
                    responsePdu = CheckResponse(_responseBuffer, _responseBufferLength);
            }

            return responsePdu;
        }
        
        protected virtual byte[] CheckResponse(byte[] respRaw, int respRawLength)
        {
            byte[] responsePdu = null;
            // Check minimal response length 
            if (respRawLength < 5)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram. Did not receive minimal length of 5 bytes.");
            
            // Is response a "modbus exception response"
            if ((respRaw[1] & 0x80) > 0)
                throw ModbusException.GetModbusException(respRaw[2]);
            
            // Check CRC
            byte[] crc16 = CRC16.CalcCRC16(respRaw, respRawLength - 2);
            if ((respRaw[respRawLength - 2] != crc16[0]) | (respRaw[respRawLength - 1] != crc16[1]))
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, CRC16-check failed");
            
            // Strip ADU header and copy response PDU into output buffer 
            responsePdu = new byte[respRawLength - 2];
            for (int i = 0; i < respRawLength - 2; i++)
                responsePdu[i] = respRaw[i];

            return responsePdu;
        }

        protected override byte[] BuildRequestAdu(byte[] requestPdu)
        {
            byte[] requestAdu = new byte[requestPdu.Length + 2];  // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusRTU
            // Copy request PDU
            for (int i = 0; i < requestPdu.Length; i++)
                requestAdu[i] = requestPdu[i];
            
            // Calc CRC16
            byte[] crc16 = CRC16.CalcCRC16(requestAdu, requestAdu.Length - 2);
            // Append CRC
            requestAdu[requestAdu.Length - 2] = crc16[0];
            requestAdu[requestAdu.Length - 1] = crc16[1];

            return requestAdu;
        }
    }
}
