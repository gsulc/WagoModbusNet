
namespace WagoModbusNet
{
    public class SlaveDeviceFailureException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.SlaveDeviceFailure;
        private const string _name = "Slave Device Failure";
        private const string _meaning = "Unrecoverable error occurred while slave was attempting to perform requested action.";
        public SlaveDeviceFailureException() : base(_code, _name, _meaning) { }
    }
}
