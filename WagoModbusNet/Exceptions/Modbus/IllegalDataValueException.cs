
namespace WagoModbusNet
{
    public class IllegalDataValueException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.ILLEGAL_DATA_VALUE;
        private const string _name = "Illegal Data Value";
        private const string _meaning = "Value is not accepted by slave.";

        public IllegalDataValueException() : base(_code, _name, _meaning) { }
    }
}
