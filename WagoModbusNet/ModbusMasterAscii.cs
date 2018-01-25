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
using System.Text;

namespace WagoModbusNet
{
    public class ModbusMasterAscii : ModbusMasterRtu
    {

        public ModbusMasterAscii()
        {

        }

        protected override byte[] BuildRequestAdu(byte[] requestPDU)
        {
            byte[] requestADU = new byte[((requestPDU.Length + 1) * 2) + 3];  // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusASCII
            // Insert START_OF_FRAME_CHAR's
            requestADU[0] = 0x3A;                   // START_OF_FRAME_CHAR   

            // Convert nibbles to ASCII, insert nibbles into ADU and calculate LRC on the fly
            byte val;
            byte lrc = 0;
            for (int ii = 0, jj = 0; ii < (requestPDU.Length * 2); ii++)
            {
                //Example : Byte = 0x5B converted to Char1 = 0x35 ('5') and Char2 = 0x42 ('B') 
                val = ((ii % 2) == 0) ? val = (byte)((requestPDU[jj] >> 4) & 0x0F) : (byte)(requestPDU[jj] & 0x0F);
                requestADU[1 + ii] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
                if ((ii % 2) != 0)
                {
                    lrc += requestPDU[jj];
                    jj++;
                }
            }
            lrc = (byte)(lrc * (-1));
            // Convert LRC upper nibble to ASCII            
            val = (byte)((lrc >> 4) & 0x0F);
            // Insert ASCII coded upper LRC nibble into ADU
            requestADU[requestADU.Length - 4] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
            // Convert LRC lower nibble to ASCII   
            val = (byte)(lrc & 0x0F);
            // Insert ASCII coded lower LRC nibble into ADU
            requestADU[requestADU.Length - 3] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
            // Insert END_OF_FRAME_CHAR's
            requestADU[requestADU.Length - 2] = 0x0D;   // END_OF_FRAME_CHAR1
            requestADU[requestADU.Length - 1] = 0x0A;   // END_OF_FRAME_CHAR2

            return requestADU;
        }

        protected override wmnRet CheckResponse(byte[] respRaw, int respRawLength, out byte[] respPdu)
        {
            respPdu = null;
            // Check minimal response length 
            if (respRawLength < 13)
            {
                return new wmnRet(-501, "Error: Invalid response telegram, do not receive minimal length of 13 byte");
            }
            // Check "START_OF_FRAME_CHAR" and "END_OF_FRAME_CHAR's"
            if ((respRaw[0] != 0x3A) | (respRaw[respRawLength - 2] != 0x0D) | (respRaw[respRawLength - 1] != 0x0A))
            {
                return new wmnRet(-501, "Error: Invalid response telegram, could not find 'START_OF_FRAME_CHAR' or 'END_OF_FRAME_CHARs'");
            }
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
            {
                lrc += buffer[i];
            }
            lrc = (byte)(lrc * (-1));
            // Check LRC
            if (buffer[buffer.Length - 1] != lrc)
            {
                return new wmnRet(-501, "Error: Invalid response telegram, LRC check failed");
            }
            // Is response a "modbus exception response"
            if ((buffer[1] & 0x80) > 0)
            {
                return new wmnRet((int)respRaw[2], "Modbus exception received: " + ((ModbusExceptionCodes)buffer[2]).ToString());
            }
            // Strip LRC and copy response PDU into output buffer 
            respPdu = new byte[buffer.Length - 1];
            for (int i = 0; i < respPdu.Length; i++)
            {
                respPdu[i] = buffer[i];
            }
            return new wmnRet(0, "Successful executed");
        }
    }
}
