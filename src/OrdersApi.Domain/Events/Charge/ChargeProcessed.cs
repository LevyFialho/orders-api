﻿using OrdersApi.Cqrs.Events;
using OrdersApi.Cqrs.Models; 

namespace OrdersApi.Domain.Events.Charge
{
    public class ChargeProcessed : Event
    {
        private static int _currrentTypeVersion = 1;

        public IntegrationResult Result { get; set; } 

        public ChargeProcessed(string aggregateKey, string correlationKey, string applicationKey, 
            string sagaProcessKey, short targetVersion,  IntegrationResult result)
            : base(aggregateKey, correlationKey, applicationKey, targetVersion, _currrentTypeVersion, sagaProcessKey)
        {
            Result = result;
        }
         
    }
}
