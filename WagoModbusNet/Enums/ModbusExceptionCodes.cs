﻿namespace WagoModbusNet
{
    public enum ModbusExceptionCodes : byte
    {
        ILLEGAL_FUNCTION = 1,
        ILLEGAL_DATA_ADDRESS = 2,
        ILLEGAL_DATA_VALUE = 3,
        SLAVE_DEVICE_FAILURE = 4,
        ACKNOWLEDGE = 5,
        SLAVE_DEVICE_BUSY = 6,
        NEGATIVE_ACKNOWLEDGE = 7,
        MEMORY_PARITY_ERROR = 8,
        GATEWAY_PATH_UNAVAILABLE = 10,
        GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 11,
    };
}
