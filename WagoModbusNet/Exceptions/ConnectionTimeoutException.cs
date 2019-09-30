using System;

namespace WagoModbusNet
{
    public class ConnectionTimeoutException : Exception
    {
        public ConnectionTimeoutException() 
            : base("TIMEOUT-ERROR: Timeout expired while trying to connect.")
        {

        }
    }
}
