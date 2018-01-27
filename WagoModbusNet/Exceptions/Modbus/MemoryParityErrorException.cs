
namespace WagoModbusNet
{
    public class MemoryParityErrorException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.MEMORY_PARITY_ERROR;
        private const string _name = "Memory Parity Error";
        private const string _meaning = "Slave detected a parity error in memory. Master can retry the request, but service may be required on the slave device.";

        public MemoryParityErrorException() : base(_code, _name, _meaning) { }
    }
}
