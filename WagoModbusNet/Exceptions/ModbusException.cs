using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WagoModbusNet
{
    public class ModbusException : Exception
    {
        public int ExceptionCode { get; protected set; }
        public string ExceptionText { get; protected set; }
        public string ExceptionDetails { get; protected set; }

        public ModbusException(int exceptionCode, string exceptionText, string exceptionDetails) 
            : base() 
        {
            ExceptionCode = exceptionCode;
            ExceptionText = exceptionText;
            ExceptionDetails = exceptionDetails;
        }
        public ModbusException(string message) : base(message) { }
        public ModbusException(string message, Exception innerException) : base(message, innerException) { }

        // TODO: Factory method to turn Modbus Exception Codes into detailed exception objects.
        // See modbus.org for info on exception codes
        public static ModbusException GetModbusException(int exceptionCode)
        {
            return new ModbusException(string.Format("Programmer's note: Turn Modbus Exception Code = {0} into an actual exception."));
        }
    }
}
