using System;
using System.Collections.Generic;
using System.Text;

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
