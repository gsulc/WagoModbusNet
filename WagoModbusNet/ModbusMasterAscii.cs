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
using System.Text;

namespace WagoModbusNet
{
    public class ModbusMasterAscii : ModbusMasterRtu
    {

        public ModbusMasterAscii()
        {

        }

        // TODO: confusing method
        protected override byte[] BuildRequestAdu(byte[] requestPdu)
        {
            byte[] requestAdu = new byte[((requestPdu.Length + 1) * 2) + 3];  // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusASCII
            // Insert START_OF_FRAME_CHAR's
            requestAdu[0] = 0x3A;                   // START_OF_FRAME_CHAR   

            // Convert nibbles to ASCII, insert nibbles into ADU and calculate LRC on the fly
            byte val;
            byte lrc = 0;
            for (int ii = 0, jj = 0; ii < (requestPdu.Length * 2); ii++)
            {
                //Example : Byte = 0x5B converted to Char1 = 0x35 ('5') and Char2 = 0x42 ('B') 
                val = ((ii % 2) == 0) ? val = (byte)((requestPdu[jj] >> 4) & 0x0F) : (byte)(requestPdu[jj] & 0x0F);
                requestAdu[1 + ii] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
                if ((ii % 2) != 0)
                {
                    lrc += requestPdu[jj];
                    jj++;
                }
            }
            lrc = (byte)(lrc * (-1));
            // Convert LRC upper nibble to ASCII            
            val = (byte)((lrc >> 4) & 0x0F);
            // Insert ASCII coded upper LRC nibble into ADU
            requestAdu[requestAdu.Length - 4] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
            // Convert LRC lower nibble to ASCII   
            val = (byte)(lrc & 0x0F);
            // Insert ASCII coded lower LRC nibble into ADU
            requestAdu[requestAdu.Length - 3] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
            // Insert END_OF_FRAME_CHAR's
            requestAdu[requestAdu.Length - 2] = 0x0D;   // END_OF_FRAME_CHAR1
            requestAdu[requestAdu.Length - 1] = 0x0A;   // END_OF_FRAME_CHAR2

            return requestAdu;
        }

        protected override byte[] CheckResponse(byte[] respRaw, int respRawLength)
        {
            byte[] responsePdu = null;
            // Check minimal response length 
            if (respRawLength < 13)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, did not receive minimal length of 13 bytes");
            
            // Check "START_OF_FRAME_CHAR" and "END_OF_FRAME_CHAR's"
            if ((respRaw[0] != 0x3A) | (respRaw[respRawLength - 2] != 0x0D) | (respRaw[respRawLength - 1] != 0x0A))
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, could not find 'START_OF_FRAME_CHAR' or 'END_OF_FRAME_CHARs'");

            // Convert ASCII telegram to binary
            byte[] buffer = new byte[(respRawLength - 3) / 2];
            byte high, low, val;
            for (int i = 0; i < buffer.Length; i++)
            {
                //Example : Char1 = 0x35 ('5') and Char2 = 0x42 ('B') compressed to Byte = 0x5B
                val = respRaw[(2 * i) + 1];
                high = (val <= 0x39) ? (byte)(val - 0x30) : (byte)(val - 0x37);
                val = respRaw[(2 * i) + 2];
                low = (val <= 0x39) ? (byte)(val - 0x30) : (byte)(val - 0x37);
                buffer[i] = (byte)((byte)(high << 4) | low);
            }
            // Calculate LRC
            byte lrc = 0;
            for (int i = 0; i < buffer.Length - 1; i++)
                lrc += buffer[i];

            lrc = (byte)(lrc * (-1));
            // Check LRC
            if (buffer[buffer.Length - 1] != lrc)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, LRC check failed");
            
            // Is response a "modbus exception response"
            if ((buffer[1] & 0x80) > 0)
                throw ModbusException.GetModbusException(buffer[2]);

            // Strip LRC and copy response PDU into output buffer 
            responsePdu = new byte[buffer.Length - 1];
            for (int i = 0; i < responsePdu.Length; i++)
                responsePdu[i] = buffer[i];

            return responsePdu;
        }
    }
}
