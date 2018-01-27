
namespace WagoModbusNet
{
    public class GatewayPathUnavailableException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.GATEWAY_PATH_UNAVAILABLE;
        private const string _name = "Gateway Path Unavailable";
        private const string _meaning = "Specialized for Modbus gateways. Indicates a misconfigured gateway.";

        public GatewayPathUnavailableException() : base(_code, _name, _meaning) { }
    }
}
