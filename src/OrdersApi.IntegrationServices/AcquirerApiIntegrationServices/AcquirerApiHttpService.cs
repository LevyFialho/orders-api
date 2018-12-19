using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Extensions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Repository;
using OrdersApi.Domain.IntegrationServices;
using OrdersApi.Domain.Model.ChargeAggregate;
using OrdersApi.Domain.Model.ProductAggregate;
using OrdersApi.Infrastructure.Resilience;
using OrdersApi.IntegrationServices.AcquirerApiIntegrationServices.Contracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Charge = OrdersApi.Domain.Model.ChargeAggregate.Charge;
#pragma warning disable S1075

namespace OrdersApi.IntegrationServices.AcquirerApiIntegrationServices
{
    public class AcquirerApiHttpService : IAcquirerApiService
    {
        protected readonly IHttpClient Client;
        protected readonly AggregateDataSource Repository;
        protected readonly AcquirerApiSettings Settings;
        protected readonly ILogger<AcquirerApiHttpService> Logger;

        public AcquirerApiHttpService(AggregateDataSource repository, IHttpClient client, AcquirerApiSettings settings, ILogger<AcquirerApiHttpService> logger)
        {
            Client = client;
            Settings = settings;
            Repository = repository;
            Logger = logger;
        }

        public async Task<IntegrationResult<bool>> CheckIfChargeOrderWasSent(AcquirerAccount account, string orderId)
        {
            var integrationResult = new IntegrationResult<bool>(Result.Error);
            if (account == null || string.IsNullOrWhiteSpace(orderId))
                return integrationResult;
            try
            { 
                var uri = BuildGetChargeUrl(orderId, account);
                var response = await Client.GetAsync(uri, Settings.AuthenticationToken);
                integrationResult.StatusCode = response.StatusCode;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    integrationResult.Result = Result.Sucess;
                    integrationResult.ReturnedObject = false; //Resource does not exist
                    return integrationResult;
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var chargeResponse = await response.GetJsonObjectFromHttpResponse<List<GetChargeResponse>>();
                    if (chargeResponse != null && chargeResponse.Any())
                        integrationResult.ReturnedObject = true; //Resource already exist

                    integrationResult.Result = Result.Sucess;
                    return integrationResult;
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, e.ToString());
                integrationResult.Result = Result.Error;
                integrationResult.StatusCode = HttpStatusCode.InternalServerError;
                integrationResult.Details = new List<string>() { e.ToString() };
            }
            return integrationResult;
        }

        public async Task<IntegrationResult<DateTime?>> GetSettlementDate(AcquirerAccount account, string orderId)
        {
            var integrationResult = new IntegrationResult<DateTime?>(Result.Error);
            if (account == null || string.IsNullOrWhiteSpace(orderId))
                return integrationResult;
            try
            {
                var uri = BuildGetChargeUrl(orderId, account);
                var response = await Client.GetAsync(uri, Settings.AuthenticationToken);
                integrationResult.StatusCode = response.StatusCode;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    integrationResult.Result = Result.Sucess; 
                    return integrationResult;
                }
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var chargeResponse = await response.GetJsonObjectFromHttpResponse<List<GetChargeResponse>>();
                    var chargeData = chargeResponse?.FirstOrDefault();
                    if(chargeData?.IsSettled == true)
                        integrationResult.ReturnedObject = DateTime.UtcNow;

                    integrationResult.Result = Result.Sucess;
                    return integrationResult;
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, e.ToString());
                integrationResult.Result = Result.Error;
                integrationResult.StatusCode = HttpStatusCode.InternalServerError;
                integrationResult.Details = new List<string>() { e.ToString() };
            }
            return integrationResult;
        }

        public async Task<IntegrationResult> SendChargeOrder(Charge charge)
        {
            IntegrationResult integrationResult = new IntegrationResult(Result.Error);
            if (!(charge.PaymentMethodData.GetData() is AcquirerAccount acquirerAccount))
                return integrationResult;
            try
            {
                var uri = Settings.ApplicationUri + "/v1/acquirers/" + acquirerAccount.AcquirerKey + "/merchants/"
                          + acquirerAccount.MerchantKey + "/charges";
                var product = await Repository.GetByIdAsync<Product>(charge.OrderDetails.ProductInternalKey);

                var body = CreatePostCharge(charge, acquirerAccount, product);

                var response = await Client.PostAsync(uri, body, Settings.AuthenticationToken);
                integrationResult.StatusCode = response.StatusCode;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    integrationResult.Result = Result.Sucess;
                    return integrationResult;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    integrationResult.Result = Result.Sucess;
                    if (response.Content != null)
                    {
                        var data = await response.GetJsonObjectFromHttpResponse<ErrorResponse>();
                        integrationResult.Details = GetErrorMessageFromBadRequestResponse(data);
                    }
                    return integrationResult;
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, e.ToString());
                integrationResult.Result = Result.Error;
                integrationResult.StatusCode = HttpStatusCode.InternalServerError;
                integrationResult.Details = new List<string>() { e.ToString() };
            }
            return integrationResult;
        }

        public async Task<IntegrationResult> SendReversalOrder(Charge charge, string reversalKey)
        {
            IntegrationResult integrationResult = new IntegrationResult(Result.Error);
            var reversal = charge.Reversals.FirstOrDefault(x => x.ReversalKey == reversalKey);
            if (!(charge.PaymentMethodData.GetData() is AcquirerAccount acquirerAccount) || reversal == null)
                return integrationResult;
            try
            {
                var uri = Settings.ApplicationUri + "/v1/acquirers/" + acquirerAccount.AcquirerKey + "/merchants/"
                          + acquirerAccount.MerchantKey + "/charges/" + charge.AggregateKey + "/reversals";

                var body = CreateReversalRequest(reversal);
               

                var response = await Client.PostAsync(uri, body, Settings.AuthenticationToken);
                integrationResult.StatusCode = response.StatusCode;
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    integrationResult.Result = Result.Sucess;
                    return integrationResult;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    integrationResult.Result = Result.Sucess;
                    if (response.Content != null)
                    {
                        var data = await response.GetJsonObjectFromHttpResponse<ErrorResponse>();
                        integrationResult.Details = GetErrorMessageFromBadRequestResponse(data);
                    }
                    return integrationResult;
                }
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, e.ToString());
                integrationResult.Result = Result.Error;
                integrationResult.StatusCode = HttpStatusCode.InternalServerError;
                integrationResult.Details = new List<string>() { e.ToString() };
            }
            return integrationResult;
        }

        public virtual List<string> GetErrorMessageFromBadRequestResponse(ErrorResponse response)
        {
            if (response != null)
            {
                return response?.Errors?.Select(x => x.Code + " - " + x.Message).ToList();
            }
            return new List<string>();
        }

        public virtual CreateChargeRequest CreatePostCharge(Charge charge, AcquirerAccount acquirerAccount, Product product)
        {
            int accountTypeId; 
            try
            {
                accountTypeId = Convert.ToInt32(product.AcquirerConfigurations
                    .FirstOrDefault(x => x.AcquirerKey == acquirerAccount.AcquirerKey && x.CanCharge)?.AccountKey);
            }
            catch
            {
                accountTypeId = Settings.DefaultAccountType;
            }
            
            var post = new CreateChargeRequest()
            { 
                ExternalKey = charge.AggregateKey,
                ChargeDate = charge.OrderDetails.ChargeDate,
                ChargeAmount = new ChargeAmount()
                {
                    Amount = charge.OrderDetails.Amount,
                    Currency = Settings.DefaultCurrencyCode
                },
                ChargeType = Settings.DefaultChargeTypeCode,
                AccountType = accountTypeId
            };

            if (product?.ExternalKey == Settings.PosRentKey)
                post.ChargeType = Settings.PosRentChargeTypeCode;

            if (product?.ExternalKey == Settings.ExternalPosRentKey)
                post.ChargeType = Settings.ExternalPosRentChargeTypeCode;

            return post;
        }

        public virtual CreateReversalRequest CreateReversalRequest(Reversal reversal)
        {
            var post = new CreateReversalRequest()
            { 
                ExternalKey = reversal.ReversalKey, 
                ChargeAmount = new ChargeAmount()
                {
                    Amount = reversal.Amount,
                    Currency = Settings.DefaultCurrencyCode
                }, 
            };
            return post;
        }

        public virtual string BuildGetChargeUrl(string orderId, AcquirerAccount acquirerAccount)
        {
           return Settings?.ApplicationUri + "/v1/acquirers/" + acquirerAccount?.AcquirerKey + "/charges/?externalKey=" + orderId;
        }

    }
}
