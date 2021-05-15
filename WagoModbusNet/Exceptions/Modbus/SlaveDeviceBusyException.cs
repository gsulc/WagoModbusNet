
namespace WagoModbusNet
{
    public class SlaveDeviceBusyException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.SlaveDeviceBusy;
        private const string _name = "Slave Device Busy";
        private const string _meaning = "Slave is engaged in processing a long-duration command. Master should retry later.";

        public SlaveDeviceBusyException() : base(_code, _name, _meaning) { }
    }
}
