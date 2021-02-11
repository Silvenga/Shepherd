using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Shepherd.Core.Factories
{
    public class VaultClientFactory
    {
        private readonly ILogger<VaultClientFactory> _logger;

        public VaultClientFactory(ILogger<VaultClientFactory> logger)
        {
            _logger = logger;
        }

        public IVaultClient CreateClient(Uri address, string token, string? expectedHostname)
        {
            var settings = new VaultClientSettings(address.ToString(), new TokenAuthMethodInfo(token))
            {
                MyHttpClientProviderFunc = CreateHttpClientHandler(expectedHostname)
            };
            return new VaultClient(settings);
        }

        public IVaultClient CreateClient(Uri address, string? expectedHostname)
        {
            return CreateClient(address, Guid.NewGuid().ToString(), expectedHostname);
        }

        private Func<HttpClientHandler, HttpClient> CreateHttpClientHandler(string? expectedHostname)
        {
            if (expectedHostname != null)
            {
                return innerHandler =>
                {
                    innerHandler.ServerCertificateCustomValidationCallback += CreateCustomValidationCallback(expectedHostname);
                    return new HttpClient(innerHandler);
                };
            }

            return innerHandler => new HttpClient(innerHandler);
        }

        private Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CreateCustomValidationCallback(string expectedHostname)
        {
            return (_, certificate, _, policy) =>
            {
                var onlyHashNameValidationError = (policy & SslPolicyErrors.RemoteCertificateNameMismatch)
                                                  == SslPolicyErrors.RemoteCertificateNameMismatch;

                if (onlyHashNameValidationError && certificate != null)
                {
                    _logger.LogTrace("Certificate is trusted, but the hostname does not match, validating hostname.");

                    var sans = GetExtensions(certificate).Where(x => x.Oid?.Value == "2.5.29.17")
                                                         .Select(x => x.Format(true))
                                                         .SelectMany(x => x.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                                         .Select(x => x.Split('=')[1])
                                                         .ToList();
                    var validCommonName = sans.Contains(expectedHostname, StringComparer.OrdinalIgnoreCase);
                    return validCommonName;
                }

                return policy == SslPolicyErrors.None;
            };
        }

        private static IEnumerable<AsnEncodedData> GetExtensions(X509Certificate2 certificate)
        {
            foreach (var extension in certificate.Extensions)
            {
                yield return new AsnEncodedData(extension.Oid, extension.RawData);
            }
        }
    }
}