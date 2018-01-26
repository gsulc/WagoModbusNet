namespace WagoModbusNet
{
    public enum ModbusFunctionCodes : byte
    {
        ReadCoils = 1,
        ReadDiscreteInputs = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,
        WriteSingleCoil = 5,
        WriteSingleRegister = 6,
        GetCommEventCounter = 11,
        WriteMultipleCoils = 15,
        WriteMultipleRegisters = 16,
        MaskWriteRegister = 22,
        ReadWriteMultipleRegisters = 23
    };
}
