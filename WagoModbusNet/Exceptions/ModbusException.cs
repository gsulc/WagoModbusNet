using System;

namespace WagoModbusNet
{
    public class ModbusException : Exception
    {
        public byte Code { get; protected set; }
        public string Name { get; protected set; }
        public string Meaning { get; protected set; }

        public ModbusException(byte code, string name, string meaning) 
            : base() 
        {
            Code = code;
            Name = name;
            Meaning = meaning;
        }

        public ModbusException(ModbusExceptionCodes code, string name, string meaning) : this((byte)code, name, meaning) { }
        public ModbusException(string message) : base(message) { }
        public ModbusException(string message, Exception innerException) : base(message, innerException) { }
        
        public static ModbusException GetModbusException(byte exceptionCode)
        {
            return GetModbusException((ModbusExceptionCodes)exceptionCode);
        }

        public static ModbusException GetModbusException(ModbusExceptionCodes code)
        {
            return code switch
            {
                ModbusExceptionCodes.IllegalFunction => new IllegalFunctionException(),
                ModbusExceptionCodes.IllegalDataAddress => new IllegalDataAddressException(),
                ModbusExceptionCodes.IllegalDataValue => new IllegalDataValueException(),
                ModbusExceptionCodes.SlaveDeviceFailure => new SlaveDeviceFailureException(),
                ModbusExceptionCodes.Acknowledge => new AcknowledgeException(),
                ModbusExceptionCodes.SlaveDeviceBusy => new SlaveDeviceBusyException(),
                ModbusExceptionCodes.NegativeAcknowledge => new NegativeAcknowledgeException(),
                ModbusExceptionCodes.MemoryParityError => new MemoryParityErrorException(),
                ModbusExceptionCodes.GatewayPathUnavailable => new GatewayPathUnavailableException(),
                ModbusExceptionCodes.GatewayTargetDeviceFailedToRespond => new GatewayTargetDeviceFailedToRespondException(),
                _ => new ModbusException(string.Format("Unspecified Modbus Exception Code = {0}", (byte)code)),
            };
        }

        public override string Message
        {
            get
            {
                if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Meaning))
                    return string.Format("Modbus Exception Code {0}: {1}. {2}", Code, Name, Meaning);
                else 
                    return base.Message;
            }
        }
    }
}
