namespace WagoModbusNet
{
    public enum ModbusExceptionCodes : byte
    {
        IllegalFunction = 1,
        IllegalDataAddress = 2,
        IllegalDataValue = 3,
        SlaveDeviceFailure = 4,
        Acknowledge = 5,
        SlaveDeviceBusy = 6,
        NegativeAcknowledge = 7,
        MemoryParityError = 8,
        GatewayPathUnavailable = 10,
        GatewayTargetDeviceFailedToRespond = 11,
    };
}
