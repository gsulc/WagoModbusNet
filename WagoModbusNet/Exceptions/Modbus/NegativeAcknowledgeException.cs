
namespace WagoModbusNet
{
    public class NegativeAcknowledgeException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.NegativeAcknowledge;
        private const string _name = "Negative Acknowledge";
        private const string _meaning = "Slave cannot perform the programming functions. Master should request diagnostic or error information from slave.";

        public NegativeAcknowledgeException() : base(_code, _name, _meaning) { }
    }
}
