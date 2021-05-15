namespace WagoModbusNet
{
    public class ModbusMasterAscii : ModbusMasterRtu
    {
        private const byte StartOfFrame = 0x3A;
        private const byte EndOfFrame1 = 0x0D;
        private const byte EndOfFrame2 = 0x0A;

        public ModbusMasterAscii()
        {

        }

        // TODO: confusing method
        protected override byte[] BuildRequestAdu(byte[] requestPdu)
        {
            // Contains the modbus request protocol data unit(PDU) togehther with additional information for ModbusASCII
            byte[] requestAdu = new byte[((requestPdu.Length + 1) * 2) + 3];
            requestAdu[0] = StartOfFrame;

            // Convert nibbles to ASCII, insert nibbles into ADU and calculate LRC on the fly
            byte val;
            byte lrc = 0;
            for (int i = 0, j = 0; i < (requestPdu.Length * 2); i++)
            {
                //Example : Byte = 0x5B converted to Char1 = 0x35 ('5') and Char2 = 0x42 ('B') 
                val = ((i % 2) == 0) ? (byte)((requestPdu[j] >> 4) & 0x0F) : (byte)(requestPdu[j] & 0x0F);
                requestAdu[1 + i] = (val <= 0x09) ? (byte)(0x30 + val) : (byte)(0x37 + val);
                if ((i % 2) != 0)
                {
                    lrc += requestPdu[j];
                    j++;
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
            // Insert End of Frame
            requestAdu[requestAdu.Length - 2] = EndOfFrame1;
            requestAdu[requestAdu.Length - 1] = EndOfFrame2;

            return requestAdu;
        }

        protected override byte[] CheckResponse(byte[] respRaw, int respRawLength)
        {
            if (respRawLength < 13)
                throw new InvalidResponseTelegramException("Error: Invalid response telegram, did not receive minimal length of 13 bytes");
            
            if ((respRaw[0] != StartOfFrame) | (respRaw[respRawLength - 2] != EndOfFrame1) | (respRaw[respRawLength - 1] != EndOfFrame2))
                throw new InvalidResponseTelegramException("Error: Invalid response telegram. No Start of Frame or End of Frame.");

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
            
            if (IsModbusException(buffer[1]))
                throw ModbusException.GetModbusException(buffer[2]);

            // Strip LRC and copy response PDU into output buffer 
            byte[] responsePdu = new byte[buffer.Length - 1];
            for (int i = 0; i < responsePdu.Length; i++)
                responsePdu[i] = buffer[i];

            return responsePdu;
        }
    }
}
