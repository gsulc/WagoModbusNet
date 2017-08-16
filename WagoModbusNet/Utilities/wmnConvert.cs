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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WagoModbusNet
{
    public static class wmnConvert
    {
        //Convert data from ushort[] into float[]
        public static float[] ToSingle(ushort[] buffer)
        {
            byte[] tmp = new byte[4];
            float[] outData = new float[buffer.Length / 2];
            for (int i = 0; i < outData.Length; i++)
            {
                tmp[2] = (byte)(buffer[(i * 2) + 1] & 0xFF);
                tmp[3] = (byte)(buffer[(i * 2) + 1] >> 8);
                tmp[0] = (byte)(buffer[i * 2] & 0xFF);
                tmp[1] = (byte)(buffer[i * 2] >> 8);
                outData[i] = BitConverter.ToSingle(tmp, 0);
            }
            return outData;
        }

        //Convert data from ushort[] into Int32[]
        public static int[] ToInt32(ushort[] buffer)
        {
            byte[] tmp = new byte[4];
            int[] outData = new int[buffer.Length / 2];
            for (int i = 0; i < outData.Length; i++)
            {
                tmp[2] = (byte)(buffer[(i * 2) + 1] & 0xFF);
                tmp[3] = (byte)(buffer[(i * 2) + 1] >> 8);
                tmp[0] = (byte)(buffer[i * 2] & 0xFF);
                tmp[1] = (byte)(buffer[i * 2] >> 8);
                outData[i] = BitConverter.ToInt32(tmp, 0);
            }
            return outData;
        }

        //Convert data from ushort[] into UInt32[]
        public static uint[] ToUInt32(ushort[] buffer)
        {
            byte[] tmp = new byte[4];
            uint[] outData = new uint[buffer.Length / 2];
            for (int i = 0; i < outData.Length; i++)
            {
                tmp[2] = (byte)(buffer[(i * 2) + 1] & 0xFF);
                tmp[3] = (byte)(buffer[(i * 2) + 1] >> 8);
                tmp[0] = (byte)(buffer[i * 2] & 0xFF);
                tmp[1] = (byte)(buffer[i * 2] >> 8);
                outData[i] = BitConverter.ToUInt32(tmp, 0);
            }
            return outData;
        }

        //Convert data from ushort[] into string
        public static string ToString(ushort[] buffer)
        {
            byte[] tmp = new byte[buffer.Length * 2];
            int count = 0;
            for (int i = 0, k = 0; i < buffer.Length; i++)
            {
                tmp[k] = (byte)(buffer[i] & 0xFF);
                if (tmp[k] == 0x00) { count = k; break; }
                tmp[k + 1] = (byte)(buffer[i] >> 8);
                if (tmp[k + 1] == 0x00) { count = k + 1; break; }
                k += 2;
            }
            return Encoding.ASCII.GetString(tmp, 0, count);
        }

        //Convert data from string into ushort[]
        public static ushort[] ToUInt16(string txt)
        {
            byte[] tmp = Encoding.ASCII.GetBytes(txt);
            int count = tmp.Length;
            ushort[] outData = new ushort[(count / 2) + 1];
            for (int i = 0; i < tmp.Length; i++)
            {
                outData[i / 2] = (i % 2 == 0) ? (ushort)(tmp[i]) : (ushort)(outData[i / 2] | tmp[i] << 8);
            }
            return outData;
        }

        //Convert data from float into ushort[]
        public static ushort[] ToUInt16(float value)
        {
            ushort[] outData = new ushort[2];
            byte[] tmp = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
            {
                outData[i / 2] = (i % 2 == 0) ? (ushort)(tmp[i]) : (ushort)(outData[i / 2] | tmp[i] << 8);
            }
            return outData;
        }


        //Convert data from float[] into ushort[]
        public static ushort[] ToUInt16(float[] values)
        {
            ushort[] outData = new ushort[values.Length * 2];
            int k = 0;
            foreach (float value in values)
            {
                byte[] tmp = BitConverter.GetBytes(value);
                outData[k] = (ushort)(tmp[0] | (tmp[1] << 8));
                outData[k + 1] = (ushort)(tmp[2] | (tmp[3] << 8));
                k += 2;
            }
            return outData;
        }

        //Convert data from Int32 into ushort[]
        public static ushort[] ToUInt16(int value)
        {
            ushort[] outData = new ushort[2];
            byte[] tmp = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
            {
                outData[i / 2] = (i % 2 == 0) ? (ushort)(tmp[i]) : (ushort)(outData[i / 2] | tmp[i] << 8);
            }
            return outData;
        }

        //Convert data from Int32[] into ushort[]
        public static ushort[] ToUInt16(int[] values)
        {
            ushort[] outData = new ushort[values.Length * 2];
            int k = 0;
            foreach (int value in values)
            {
                byte[] tmp = BitConverter.GetBytes(value);
                outData[k] = (ushort)(tmp[0] | (tmp[1] << 8));
                outData[k + 1] = (ushort)(tmp[2] | (tmp[3] << 8));
                k += 2;
            }
            return outData;
        }

        //Convert data from Int32 into ushort[]
        public static ushort[] ToUInt16(uint value)
        {
            ushort[] outData = new ushort[2];
            byte[] tmp = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
            {
                outData[i / 2] = (i % 2 == 0) ? (ushort)(tmp[i]) : (ushort)(outData[i / 2] | tmp[i] << 8);
            }
            return outData;
        }

        //Convert data from Int32[] into ushort[]
        public static ushort[] ToUInt16(uint[] values)
        {
            ushort[] outData = new ushort[values.Length * 2];
            int k = 0;
            foreach (uint value in values)
            {
                byte[] tmp = BitConverter.GetBytes(value);
                outData[k] = (ushort)(tmp[0] | (tmp[1] << 8));
                outData[k + 1] = (ushort)(tmp[2] | (tmp[3] << 8));
                k += 2;
            }
            return outData;
        }

    }
}
