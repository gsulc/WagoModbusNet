namespace WagoModbusNet
{
    public enum ModbusFunctionCodes : byte
    {
        fc1_ReadCoils = 1,
        fc2_ReadDiscreteInputs = 2,
        fc3_ReadHoldingRegisters = 3,
        fc4_ReadInputRegisters = 4,
        fc5_WriteSingleCoil = 5,
        fc6_WriteSingleRegister = 6,
        fc11_GetCommEventCounter = 11,
        fc15_WriteMultipleCoils = 15,
        fc16_WriteMultipleRegisters = 16,
        fc22_MaskWriteRegister = 22,
        fc23_ReadWriteMultipleRegisters = 23
    };
}
