using System;

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
