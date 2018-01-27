using System;
using System.Collections.Generic;
using System.Text;

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
