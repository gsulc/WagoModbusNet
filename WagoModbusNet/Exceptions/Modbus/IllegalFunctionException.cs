
namespace WagoModbusNet
{
    public class IllegalFunctionException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.ILLEGAL_FUNCTION;
        private const string _name = "Illegal Function";
        private const string _meaning = "Function code received in the query is not recognized or allowed by slave.";

        public IllegalFunctionException() : base(_code, _name, _meaning) { }
    }
}
