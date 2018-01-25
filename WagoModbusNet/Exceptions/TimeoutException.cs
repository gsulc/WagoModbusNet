using System;
using System.Collections.Generic;
using System.Text;

namespace WagoModbusNet
{
    public class TimeoutException : Exception
    {
        public TimeoutException()
            : base("Timeout error: Did not receive response whitin specified 'Timeout'.")
        {
        }
    }
}
