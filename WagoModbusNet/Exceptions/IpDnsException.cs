using System;

namespace WagoModbusNet
{
    public class IpDnsException : Exception
    {
        public IpDnsException(string hostname) 
            : base("DNS error: Could not resolve Ip-Address for " + hostname) 
        {

        }
    }
}
