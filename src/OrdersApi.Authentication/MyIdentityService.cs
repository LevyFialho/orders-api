using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

namespace OrdersApi.Authentication
{
    public class MyIdentityService : IIdentityService
    {
        private static readonly string[] RequiredKeys = { "id", "ts", "mac" };
        private const int AuthenticationTimestampSkewInSeconds = 300;

        private readonly IOptionsMonitor<AuthenticationOptions> _gimAuthenticationOptions;

        public MyIdentityService(IOptionsMonitor<AuthenticationOptions> options)
        {
            this._gimAuthenticationOptions = options;
        }

        public virtual Task<AuthenticationTicket> GetAuthenticationTicket(AuthenticationScheme scheme, HttpRequest request)
        {
            var authenticationHeader = AuthenticationHeaderValue.Parse(request.Headers[HeaderNames.Authorization]);

            var applicationKey = this._gimAuthenticationOptions.Get(scheme.Name).ApplicationKey;

            var authenticationData = GetAuthenticationDataFromHeader(applicationKey, authenticationHeader.Parameter, GetUrlWithoutQuerystring(request), request.Method);

            if (authenticationData == null)
            {
                throw new InvalidOperationException();
            }

            var clientApplicationKey = GetClientApplicationKeyFromHeader(authenticationHeader);

            var claims = new List<Claim>() { new Claim(ClaimTypes.System, clientApplicationKey) };


            var identity = new ClaimsIdentity(claims, scheme.Name);

            return Task.FromResult(BuildAuthenticationTicket(identity));

        }

        protected virtual AuthenticationTicket BuildAuthenticationTicket(ClaimsIdentity identity)
        {
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, identity.AuthenticationType);

            return ticket;
        }

        private static string GetClientApplicationKeyFromHeader(AuthenticationHeaderValue authorizationHeader)
        {
            var clientApplicationKeyRegex = new Regex(@"id=\""(?<clientApplicationKey>[0-9A-Fa-f]{8}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{4}\-[0-9A-Fa-f]{12})\""");

            var result = clientApplicationKeyRegex.Match(authorizationHeader.Parameter);

            return result.Success ? result.Groups["clientApplicationKey"].Value : null;
        }

        private static AuthenticationData GetAuthenticationDataFromHeader(string applicationKey, string authorization, string url, string method)
        {
            var values = ParseAttributes(authorization);

            ValidateKeys(AuthenticationTimestampSkewInSeconds, values);

            ValidateTimestamp(AuthenticationTimestampSkewInSeconds, values);

            var clientApplicationKey = values["id"];
            var mac = values["mac"];
            var macAsBytes = Convert.FromBase64String(mac);
            var macDecodedString = Encoding.UTF8.GetString(macAsBytes);

            var globalIdentityParameters = new AuthenticationData
            {
                ApplicationKey = applicationKey,
                ClientApplicationKey = clientApplicationKey,
                EncryptedData = macDecodedString,
                RawData = GenerateRawData(clientApplicationKey, method, url, values["ts"])
            };

            return globalIdentityParameters;
        }

        private static NameValueCollection ParseAttributes(string authorization)
        {
            var nameValueCollection = new NameValueCollection();

            foreach (var current in authorization.Split(','))
            {
                var length = current.IndexOf('=');

                if (length > 0)
                {
                    var name = current.Substring(0, length).Trim();
                    var value = current.Substring(length + 1).Trim();

                    if (value.StartsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    nameValueCollection.Add(name, value);
                }
            }

            return nameValueCollection;
        }

        private static bool CheckTimestamp(string ts, int timestampSkewSec, out double diff)
        {
            if (!double.TryParse(ts, out double unixTimestamp)) throw new ArgumentException("Invalid parameter ts in the authorization header", "ts");

            double currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  

            diff = Math.Abs(unixTimestamp - currentTimestamp);

            return diff <= timestampSkewSec;
        }

        private static void ValidateTimestamp(int timestampSkewSec, NameValueCollection attributes)
        {
            double diff = 0;

            var validTimestamp = CheckTimestamp(attributes["ts"], timestampSkewSec, out diff);

            if (!validTimestamp)
                throw new ArgumentOutOfRangeException($"Request took too long. '{diff}' seconds");
        }

        private static void ValidateKeys(int timestampSkewSec, NameValueCollection attributes)
        {
            var missingKeys = RequiredKeys.Where(requiredKey => !attributes.AllKeys.Any(currentKey => string.Equals(currentKey, requiredKey, StringComparison.OrdinalIgnoreCase)))
                                            .Select(key => key);

            if (missingKeys.Any())
                throw new ArgumentException($"Missing keys on authorization header: {string.Format(", ", missingKeys)}.");
        }

        private static string GenerateRawData(string clientApplicationKey, string method, string url, string timestamp)
        {
            return $"gim.{clientApplicationKey}.{method.ToUpper()}.{url}.{timestamp}";
        }

        private static string GetUrlWithoutQuerystring(HttpRequest request)
        {
            var uri = new Uri(request.GetDisplayUrl());

            var scheme = uri.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out var originRequestScheme) && string.Equals(originRequestScheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                scheme = originRequestScheme;
            }

            var urlWithoutQuerystring = $"{scheme}://{uri.Authority}{uri.AbsolutePath}";

            return urlWithoutQuerystring;
        }
    }
}

