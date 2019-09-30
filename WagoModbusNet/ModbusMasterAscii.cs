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
            byte[] responsePdu = new byte[buffer.Length - 1];
            for (int i = 0; i < responsePdu.Length; i++)
                responsePdu[i] = buffer[i];

            return responsePdu;
        }
    }
}
