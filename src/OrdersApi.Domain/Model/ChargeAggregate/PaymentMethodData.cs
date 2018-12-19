using System;
using OrdersApi.Cqrs.Models;
using Newtonsoft.Json;

namespace OrdersApi.Domain.Model.ChargeAggregate
{
    public class PaymentMethodData : ValueObject<PaymentMethodData>
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };

        public string DataType { get; set; }

        public string Data { get; set; }

        public PaymentMethodData()
        {

        }

        public PaymentMethodData(IPaymentMethod method)
        {
            DataType = method.GetType().AssemblyQualifiedName;
            Data = JsonConvert.SerializeObject(method, SerializerSettings);
        }         

        public virtual IPaymentMethod GetData()  
        {
            return (IPaymentMethod)JsonConvert.DeserializeObject(Data, Type.GetType(DataType), SerializerSettings);
        }
    }
}
