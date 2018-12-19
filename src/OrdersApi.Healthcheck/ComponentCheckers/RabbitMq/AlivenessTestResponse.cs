using System;
using System.Collections.Generic;
using System.Text;

namespace OrdersApi.Healthcheck.ComponentCheckers.RabbitMq
{
    public class AlivenessTestResponse
    {
        public const string OK_RESPONSE = "Ok";

        public string Status { get; set; }

        public bool IsOk { get { return !string.IsNullOrEmpty(this.Status) && this.Status.ToLower() == OK_RESPONSE.ToLower(); } }
    }
}
