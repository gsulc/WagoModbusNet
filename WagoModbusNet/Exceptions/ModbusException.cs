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
            ModbusExceptionCodes code = (ModbusExceptionCodes)exceptionCode;
            return GetModbusException(code);
        }

        public static ModbusException GetModbusException(ModbusExceptionCodes code)
        {
            switch (code)
            {
                case ModbusExceptionCodes.ILLEGAL_FUNCTION:
                    return new IllegalFunctionException();
                case ModbusExceptionCodes.ILLEGAL_DATA_ADDRESS:
                    return new IllegalDataAddressException();
                case ModbusExceptionCodes.ILLEGAL_DATA_VALUE:
                    return new IllegalDataValueException();
                case ModbusExceptionCodes.SLAVE_DEVICE_FAILURE:
                    return new SlaveDeviceFailureException();
                case ModbusExceptionCodes.ACKNOWLEDGE:
                    return new AcknowledgeException();
                case ModbusExceptionCodes.SLAVE_DEVICE_BUSY:
                    return new SlaveDeviceBusyException();
                case ModbusExceptionCodes.NEGATIVE_ACKNOWLEDGE:
                    return new NegativeAcknowledgeException();
                case ModbusExceptionCodes.MEMORY_PARITY_ERROR:
                    return new MemoryParityErrorException();
                case ModbusExceptionCodes.GATEWAY_PATH_UNAVAILABLE:
                    return new GatewayPathUnavailableException();
                case ModbusExceptionCodes.GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND:
                    return new GatewayTargetDeviceFailedToRespondException();
                default: // If the code can't be cast, it will fall through to here.
                    return new ModbusException(string.Format("Unspecified Modbus Exception Code = {0}", (byte)code));
            }
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
