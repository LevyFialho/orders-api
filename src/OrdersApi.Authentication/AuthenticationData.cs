using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Authentication
{
    public class AuthenticationData
    {
        public string ApplicationKey { get; internal set; }
        public string ClientApplicationKey { get; internal set; }
        public string EncryptedData { get; internal set; }
        public string RawData { get; internal set; }
    }
}
