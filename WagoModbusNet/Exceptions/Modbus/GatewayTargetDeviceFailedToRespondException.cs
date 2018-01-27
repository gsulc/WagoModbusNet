﻿
namespace WagoModbusNet
{
    public class GatewayTargetDeviceFailedToRespondException : ModbusException
    {
        private const ModbusExceptionCodes _code = ModbusExceptionCodes.GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND;
        private const string _name = "Gateway Target Device Failed to Respond";
        private const string _meaning = "Specialized for Modbus gateways. Sent when slave fails to respond.";

        public GatewayTargetDeviceFailedToRespondException() : base(_code, _name, _meaning) { }
    }
}
