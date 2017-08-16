namespace WagoModbusNet
{
    public enum ModbusExceptionCodes : byte
    {
        // Modbus specified exception codes 
        ec1_ILLEGAL_FUNCTION = 1,
        ec2_ILLEGAL_DATA_ADDRESS = 2,
        ec3_ILLEGAL_DATA_VALUE = 3,
        ec4_SLAVE_DEVICE_FAILURE = 4,
        ec5_ACKNOWLEDGE = 5,
        ec6_SLAVE_DEVICE_BUSY = 6,
        ec8_MEMORY_PARITY_ERROR = 8,
        ec10_GATEWAY_PATH_UNAVAILABLE = 10,
        ec11_GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 11,
    };
}
