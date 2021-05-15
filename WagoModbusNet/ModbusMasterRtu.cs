using System;
using System.IO.Ports;

namespace WagoModbusNet
{
    public class ModbusMasterRtu : ModbusMaster
    {

        public ModbusMasterRtu()
        {
        }

        public ModbusMasterRtu(string portname, int baudrate, int databits, Parity parity, StopBits stopbits, Handshake handshake)
        {
            Portname = portname;
            Baudrate = baudrate;
            Parity = parity;
            Databits = databits;
            StopBits = stopbits;
            Handshake = handshake;
        }

        private SerialPort _serialPort;

        public string Portname { get; set; }
        public int Baudrate { get; set; } = 9600;
        public int Databits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;

        // Receive response helpers        
        private byte[] _responseBuffer = new byte[512]; // TODO: inspect implementation. Should not keep around a buffer as a field.
        private int _responseBufferLength;

        public override void Connect()
        {
            if (Connected)
                Disconnect();

            _serialPort = new SerialPort(Portname, Baudrate, Parity, Databits, StopBits);
            _serialPort.Handshake = Handshake;
            _serialPort.WriteTimeout = Timeout;
            _serialPort.ReadTimeout = Timeout;
            _serialPort.Open();
            Connected = true;
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
            Connected = false;
        }

        // Send request and and wait for response 
        protected override byte[] Query(byte[] requestAdu)
        {
            byte[] responsePdu = null;
            if (!Connected)
                throw new NotConnectedException();

            // Send Request( synchron ) 
            _serialPort.Write(requestAdu, 0, requestAdu.Length);
            _responseBuffer.Initialize();
            _responseBufferLength = 0;
            _serialPort.ReadTimeout = Timeout;
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
                // Expected exception to know "All data received" 
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
        
        protected virtual byte[] CheckResponse(byte[] response, int length)
        {
            if (length < 5)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram. Did not receive minimal length of 5 bytes.");
            if (IsModbusException(response[1]))
                throw ModbusException.GetModbusException(response[2]);
            if (!IsCRCValid(response, length))
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, CRC16-check failed");
            
            // Strip ADU header and copy response PDU into output buffer 
            byte[] responsePdu = new byte[length - 2];
            for (int i = 0; i < length - 2; i++)
                responsePdu[i] = response[i];

            return responsePdu;
        }

        private bool IsCRCValid(byte[] response, int length)
        {
            byte[] crc16 = CRC16.CalcCRC16(response, length - 2);
            return (response[length - 2] == crc16[0]) && (response[length - 1] == crc16[1]);
        }

        protected override byte[] BuildRequestAdu(byte[] requestPdu)
        {
            // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusRTU
            byte[] requestAdu = new byte[requestPdu.Length + 2];
            // Copy request PDU
            for (int i = 0; i < requestPdu.Length; ++i)
                requestAdu[i] = requestPdu[i];
            
            byte[] crc16 = CRC16.CalcCRC16(requestAdu, requestAdu.Length - 2);
            requestAdu[requestAdu.Length - 2] = crc16[0];
            requestAdu[requestAdu.Length - 1] = crc16[1];

            return requestAdu;
        }
    }
}
