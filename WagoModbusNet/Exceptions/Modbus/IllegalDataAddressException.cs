
namespace WagoModbusNet
{
    public class IllegalDataAddressException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.ILLEGAL_DATA_ADDRESS;
        private const string _name = "Illegal Data Address";
        private const string _meaning = "Data address of some or all the required entities are not allowed or do not exist in slave";

        public IllegalDataAddressException() : base(_code, _name, _meaning) { }
    }
}
