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

namespace WagoModbusNet
{
    public abstract class ModbusMaster
    {
        protected int _timeout = 500;
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public abstract bool Connected { get; }

        public abstract void Connect();
        public abstract void Disconnect();


        /// <summary>
        /// FC1 - Read Coils
        /// WAGO coupler and controller do not differ between FC1 and FC2
        /// Digital outputs utilze a offset of 256. First coil start at address 256.
        /// Address 0 and follows returning status of digital inputs modules 
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="readCount"></param>
        /// <returns></returns>
        public bool[] ReadCoils(byte id, ushort startAddress, ushort readCount)
        {
            byte[] responsePdu = SendModbusRequest(id, ModbusFunctionCodes.ReadCoils, startAddress, readCount, 0, 0, null);

            //Strip PDU header and convert data into bool[]
            bool[] data = new bool[readCount];
            for (int i = 0, k = 0; i < readCount; i++)
            {
                data[i] = ((responsePdu[k + 3] & (byte)(0x01 << (i % 8))) > 0) ? true : false;
                k = (i + 1) / 8;
            }

            return data;
        }

        /// <summary>
        /// FC2 - Read Discrete Inputs
        /// WAGO coupler and controller do not differ between FC1 and FC2
        /// Address 0 and follows returning status of digital inputs modules
        /// Digital outputs utilze a offset of 256. First coil start at address 256.         
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="readCount"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool[] ReadDiscreteInputs(byte id, ushort startAddress, ushort readCount)
        {
            byte[] responsePdu = 
                SendModbusRequest(id, ModbusFunctionCodes.ReadDiscreteInputs, startAddress, readCount, 0, 0, null);

            //Strip PDU header and convert data into bool[]
            bool[] data = new bool[readCount];
            for (int i = 0, k = 0; i < readCount; i++)
            {
                data[i] = ((responsePdu[k + 3] & (byte)(0x01 << (i % 8))) > 0) ? true : false;
                k = (i + 1) / 8;
            }

            return data;
        }

        /// <summary>
        /// FC3 - Read Holding Registers
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="readCount"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ushort[] ReadHoldingRegisters(byte id, ushort startAddress, ushort readCount)
        {
            byte[] responsePdu = 
                SendModbusRequest(id, ModbusFunctionCodes.ReadHoldingRegisters, startAddress, readCount, 0, 0, null);
            //Strip PDU header and convert data into ushort[]
            byte[] tmp = new byte[2];
            int count = (responsePdu[2] / 2);
            ushort[] data = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                tmp[0] = responsePdu[4 + (2 * i)];
                tmp[1] = responsePdu[3 + (2 * i)];
                data[i] = BitConverter.ToUInt16(tmp, 0);
            }

            return data;
        }

        /// <summary>
        /// FC4 - Read Input Registers
        /// WAGO coupler and controller do not differ between FC3 and FC4
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="readCount"></param>
        /// <returns></returns>
        public ushort[] ReadInputRegisters(byte id, ushort startAddress, ushort readCount)
        {
            ushort[] data = null;
            byte[] responsePdu = 
                SendModbusRequest(id, ModbusFunctionCodes.ReadInputRegisters, startAddress, readCount, 0, 0, null);

            //Strip PDU header and convert data into ushort[]
            byte[] tmp = new byte[2];
            int count = (responsePdu[2] / 2);
            data = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                tmp[0] = responsePdu[4 + (2 * i)];
                tmp[1] = responsePdu[3 + (2 * i)];
                data[i] = BitConverter.ToUInt16(tmp, 0);
            }

            return data;
        }

        /// <summary>
        /// FC5 - Write Single Coil
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void WriteSingleCoil(byte id, ushort startAddress, bool data)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeData = new byte[1];
            writeData[0] = (data) ? (byte)0xFF : (byte)0x00;
            SendModbusRequest(id, ModbusFunctionCodes.WriteSingleCoil, 0, 0, startAddress, 1, writeData);
        }

        /// <summary>
        /// FC6 - Write Single Register
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void WriteSingleRegister(byte id, ushort startAddress, ushort data)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeData = BitConverter.GetBytes(data);
            SendModbusRequest(id, ModbusFunctionCodes.WriteSingleRegister, 0, 0, startAddress, 1, writeData);
        }

        // TODO: What is status? out arguments are generally bad practice.

        /// <summary>
        /// FC11 - Get Comm Event Counter
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="status"> return 0 for ready to process next requst or 0xFFFF when device busy</param>
        /// <param name="eventCount">number of successful processed Modbus-Requests</param>
        /// <returns></returns>
        public ushort GetCommEventCounter(byte id, out ushort status)
        {
            status = 0;
            byte[] responsePdu = SendModbusRequest(id, ModbusFunctionCodes.GetCommEventCounter, 0, 0, 0, 0, null);

            //Strip PDU header and convert data into ushort[]
            byte[] tmp = new byte[2];
            //Extract status
            tmp[0] = responsePdu[3];
            tmp[1] = responsePdu[2];
            status = BitConverter.ToUInt16(tmp, 0);
            //Extract eventCount
            tmp[0] = responsePdu[5];
            tmp[1] = responsePdu[4];
            ushort eventCount = BitConverter.ToUInt16(tmp, 0);

            return eventCount;
        }

        /// <summary>
        /// FC15 - Write Multiple Coils
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void WriteMultipleCoils(byte id, ushort startAddress, bool[] data)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeData = ((data.Length % 8) == 0) ? new byte[data.Length / 8] : new byte[(data.Length / 8) + 1];
            for (int i = 0, k = 0; i < data.Length; i++)
            {
                if ((i > 0) && ((i % 8) == 0)) k++;
                if (data[i]) writeData[k] = (byte)(writeData[k] | (byte)(0x01 << (i % 8)));
            }
            
            SendModbusRequest(id, ModbusFunctionCodes.WriteMultipleCoils, 0, 0, startAddress, (ushort)data.Length, writeData);
        }

        /// <summary>
        /// FC16 - Write Multiple Registers
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="writeCount"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void WriteMultipleRegisters(byte id, ushort startAddress, ushort[] data)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeData = new byte[data.Length * 2];
            byte[] tmp;
            for (int i = 0, k = 0; i < data.Length; i++)
            {
                tmp = BitConverter.GetBytes(data[i]);
                writeData[k] = tmp[1];
                writeData[k + 1] = tmp[0];
                k += 2;
            }
            
            SendModbusRequest(id, ModbusFunctionCodes.WriteMultipleRegisters, 0, 0, startAddress, (ushort)data.Length, writeData);
        }

        // TODO: confusing method

        /// <summary>
        /// FC22 - Mask Write Register
        /// Modify single bits in a register
        /// Result = (CurrentContent AND andMask) OR (orMask AND (NOT andMask))
        /// If the orMask value is zero, the result is simply the logical ANDing of the current contents and andMask. 
        /// If the andMask value is zero, the result is equal to the orMask value.
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="startAddress"></param>
        /// <param name="andMask"></param>
        /// <param name="orMask"></param>
        /// <returns></returns>
        public void MaskWriteRegister(byte id, ushort startAddress, ushort andMask, ushort orMask)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeData = new byte[4];
            byte[] tmp;
            tmp = BitConverter.GetBytes(andMask);
            writeData[0] = tmp[0];
            writeData[1] = tmp[1];
            tmp = BitConverter.GetBytes(orMask);
            writeData[2] = tmp[0];
            writeData[3] = tmp[1];
            SendModbusRequest(id, ModbusFunctionCodes.MaskWriteRegister, 0, 0, startAddress, 4, writeData);
        }

        /// <summary>
        /// FC23 - Read Write Multiple Registers
        /// </summary>
        /// <param name="id">Unit-Id or Slave-Id depending underlaying transport layer</param>
        /// <param name="readAddress"></param>
        /// <param name="readCount"></param>
        /// <param name="writeAddress"></param>
        /// <param name="writeData"></param>
        /// <param name="readData"></param>
        /// <returns></returns>
        public ushort[] ReadWriteMultipleRegisters(byte id, ushort readAddress, ushort readCount, ushort writeAddress, ushort[] writeData)
        {
            // Convert data to write into array of byte with the correct byteorder
            byte[] writeBuffer = new byte[writeData.Length * 2];
            byte[] tmp;
            for (int i = 0, k = 0; i < writeData.Length; i++)
            {
                tmp = BitConverter.GetBytes(writeData[i]);
                writeBuffer[k] = tmp[1];
                writeBuffer[k + 1] = tmp[0];
                k += 2;
            }

            byte[] responsePdu = 
                SendModbusRequest(id, ModbusFunctionCodes.ReadWriteMultipleRegisters, readAddress, readCount, writeAddress, (ushort)writeData.Length, writeBuffer);

            //Strip PDU header and convert data into ushort[]
            byte[] buf = new byte[2];
            int count = (responsePdu[2] / 2);
            ushort[] readData = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                buf[0] = responsePdu[4 + (2 * i)];
                buf[1] = responsePdu[3 + (2 * i)];
                readData[i] = BitConverter.ToUInt16(buf, 0);
            }

            return readData;
        }


        // TODO: too many arguments
        // Build common part of modbus request, decorate it with transport layer specific header, send request and get response PDU back 
        public byte[] SendModbusRequest(byte id, ModbusFunctionCodes functionCode, ushort readAddress, ushort readCount, ushort writeAddress, ushort writeCount, byte[] writeData)
        {
            byte[] responsePdu = null;
            // Build common part of modbus request
            byte[] requestPdu = BuildRequestPdu(id, functionCode, readAddress, readCount, writeAddress, writeCount, writeData);

            // Decorate common part of modbus request with transport layer specific header
            byte[] requestAdu = BuildRequestAdu(requestPdu);

            // Send modbus request and return response 
            responsePdu = Query(requestAdu);
            return responsePdu;
        }


        // Decorate common part of modbus request with transport layer specific header
        protected abstract byte[] BuildRequestAdu(byte[] requestPdu);

        // Send modbus request transport layer specific and return response PDU
        protected abstract byte[] Query(byte[] requestAdu);

        // TODO: Refactor this. Too many lines and arguments; ugly switch statement. Hard to follow. Break out a new class.
        // Build common part of modbus request
        private byte[] BuildRequestPdu(byte id, ModbusFunctionCodes functionCode, ushort readAddress, ushort readCount, ushort writeAddress, ushort writeCount, byte[] writeData)
        {
            byte[] help; // Used to convert ushort into bytes
            byte[] requestPdu = null;

            switch (functionCode)
            {
                case ModbusFunctionCodes.ReadCoils:
                case ModbusFunctionCodes.ReadDiscreteInputs:
                case ModbusFunctionCodes.ReadHoldingRegisters:
                case ModbusFunctionCodes.ReadInputRegisters:
                    requestPdu = new byte[6];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = (byte)functionCode;         // Modbus-Function-Code
                    help = BitConverter.GetBytes(readAddress);
                    requestPdu[2] = help[1];					// Start read address -Hi
                    requestPdu[3] = help[0];					// Start read address -Lo
                    help = BitConverter.GetBytes(readCount);
                    requestPdu[4] = help[1];				    // Number of coils or register to read -Hi
                    requestPdu[5] = help[0];				    // Number of coils or register to read -Lo  
                    break;

                case ModbusFunctionCodes.WriteSingleCoil:
                    requestPdu = new byte[6];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x05;                       // Modbus-Function-Code: fc5_WriteSingleCoil
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[2] = help[1];					// Address of coil to force -Hi
                    requestPdu[3] = help[0];					// Address of coil to force -Lo
                    // Copy data
                    requestPdu[4] = writeData[0];				// Output value -Hi  ( 0xFF or 0x00 )
                    requestPdu[5] = 0x00;				        // Output value -Lo  ( const: 0x00  ) 
                    break;

                case ModbusFunctionCodes.WriteSingleRegister:
                    requestPdu = new byte[6];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x06;                       // Modbus-Function-Code: fc6_WriteSingleRegister
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[2] = help[1];					// Address of register to force -Hi
                    requestPdu[3] = help[0];					// Address of register to force -Lo
                    requestPdu[4] = writeData[1];				// Output value -Hi  
                    requestPdu[5] = writeData[0];				// Output value -Lo  
                    break;

                case ModbusFunctionCodes.GetCommEventCounter:
                    requestPdu = new byte[2];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x0B;                       // Modbus-Function-Code: fc11_GetCommEventCounter
                    break;

                case ModbusFunctionCodes.WriteMultipleCoils:
                    byte byteCount = (byte)(writeCount / 8);
                    if ((writeCount % 8) > 0)
                    {
                        byteCount += 1;
                    }
                    requestPdu = new byte[7 + byteCount];
                    // Build request header
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x0F;                       // Modbus-Function-Code: fc15_WriteMultipleCoils
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[2] = help[1];					// Start address of coils to force -Hi
                    requestPdu[3] = help[0];					// Start address of coils to force -Lo
                    help = BitConverter.GetBytes(writeCount);
                    requestPdu[4] = help[1];				    // Number of coils to write -Hi 
                    requestPdu[5] = help[0];				    // Number of coils to write -Lo  
                    requestPdu[6] = byteCount;				    // Number of bytes to write                    
                    // Copy data
                    for (int i = 0; i < byteCount; i++)
                    {
                        requestPdu[7 + i] = writeData[i];
                    }
                    break;

                case ModbusFunctionCodes.WriteMultipleRegisters:
                    requestPdu = new byte[7 + (writeCount * 2)];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x10;                       // Modbus-Function-Code: fc16_WriteMultipleRegisters
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[2] = help[1];					// Start address of coils to force -Hi
                    requestPdu[3] = help[0];					// Start address of coils to force -Lo
                    help = BitConverter.GetBytes(writeCount);
                    requestPdu[4] = help[1];				    // Number of register to write -Hi 
                    requestPdu[5] = help[0];				    // Number of register to write -Lo  
                    requestPdu[6] = (byte)(writeCount * 2);		// Number of bytes to write                    
                    // Copy data
                    for (int i = 0; i < (writeCount * 2); i++)
                    {
                        requestPdu[7 + i] = writeData[i];
                    }
                    break;


                case ModbusFunctionCodes.MaskWriteRegister:
                    requestPdu = new byte[8];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x16;                       // Modbus-Function-Code: fc22_MaskWriteRegister
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[2] = help[1];					// Address of register to force -Hi
                    requestPdu[3] = help[0];					// Address of register to force -Lo
                    requestPdu[4] = writeData[1];				// And_Mask -Hi  
                    requestPdu[5] = writeData[0];				// And_Mask -Lo  
                    requestPdu[6] = writeData[3];				// Or_Mask -Hi  
                    requestPdu[7] = writeData[2];				// Or_Mask -Lo  
                    break;

                case ModbusFunctionCodes.ReadWriteMultipleRegisters:
                    requestPdu = new byte[11 + (writeCount * 2)];
                    // Build request header 
                    requestPdu[0] = id;                         // ID: SlaveID(RTU/ASCII) or UnitID(TCP/UDP)
                    requestPdu[1] = 0x17;                       // Modbus-Function-Code: fc23_ReadWriteMultipleRegisters
                    help = BitConverter.GetBytes(readAddress);
                    requestPdu[2] = help[1];					// Start read address -Hi
                    requestPdu[3] = help[0];					// Start read address -Lo
                    help = BitConverter.GetBytes(readCount);
                    requestPdu[4] = help[1];				    // Number of register to read -Hi
                    requestPdu[5] = help[0];				    // Number of register to read -Lo           
                    help = BitConverter.GetBytes(writeAddress);
                    requestPdu[6] = help[1];				    // Start write address -Hi
                    requestPdu[7] = help[0];				    // Start write address -Lo
                    help = BitConverter.GetBytes(writeCount);
                    requestPdu[8] = help[1];				    // Number of register to write -Hi
                    requestPdu[9] = help[0];				    // Number of register to write -Lo
                    requestPdu[10] = (byte)(writeCount * 2);     // Number of bytes to write
                    // Copy data
                    for (int i = 0; i < (writeCount * 2); i++)
                    {
                        requestPdu[11 + i] = writeData[i];
                    }
                    break;
            } // switch

            return requestPdu;
        }
    }
}
