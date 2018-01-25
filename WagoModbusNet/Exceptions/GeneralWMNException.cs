using System;
using System.Collections.Generic;
using System.Text;

namespace WagoModbusNet
{
    // TODO: Stand-in exception to be broken-out and depriciated.
    public class GeneralWMNException : Exception
    {
        public GeneralWMNException(string message) : base(message) { }
    }
}
