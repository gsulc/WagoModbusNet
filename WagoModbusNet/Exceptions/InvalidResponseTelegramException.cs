using System;
using System.Collections.Generic;
using System.Text;

namespace WagoModbusNet
{
    public class InvalidResponseTelegramException : Exception
    {
        public InvalidResponseTelegramException(string message) 
            : base(message)
        {

        }
    }
}
