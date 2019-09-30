using System;

namespace WagoModbusNet
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException()
            : base("Not Connected")
        {

        }
    }
}
