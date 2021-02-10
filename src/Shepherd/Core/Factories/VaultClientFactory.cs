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
        private readonly ShepherdConfiguration _configuration;

        public VaultClientFactory(ILogger<VaultClientFactory> logger, ShepherdConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IVaultClient CreateHaClient()
        {
            var token = new TokenAuthMethodInfo(_configuration.VaultToken);
            var settings = new VaultClientSettings(_configuration.VaultHaAddress.ToString(), token)
            {
                MyHttpClientProviderFunc = CreateHttpClient
            };
            return new VaultClient(settings);
        }

        public IVaultClient CreateSpecificClient(Uri uri)
        {
            var token = new TokenAuthMethodInfo(_configuration.VaultToken);
            var settings = new VaultClientSettings(uri.ToString(), token)
            {
                MyHttpClientProviderFunc = CreateHttpClient
            };
            return new VaultClient(settings);
        }

        private HttpClient CreateHttpClient(HttpClientHandler innerHandler)
        {
            if (_configuration.ExpectedVaultCommonName != null)
            {
                innerHandler.ServerCertificateCustomValidationCallback += ServerCertificateCustomValidationCallback;
            }

            return new HttpClient(innerHandler);
        }

        private bool ServerCertificateCustomValidationCallback(HttpRequestMessage request,
                                                               X509Certificate2? certificate,
                                                               X509Chain? certificateChain,
                                                               SslPolicyErrors policy)
        {
            var onlyHashNameValidationError = (policy & SslPolicyErrors.RemoteCertificateNameMismatch)
                                              == SslPolicyErrors.RemoteCertificateNameMismatch;

            if (onlyHashNameValidationError && certificate != null)
            {
                _logger.LogTrace("Certificate is trusted, but the hostname does not match, validating hostname.");

                var expectedCommonName = _configuration.ExpectedVaultCommonName;
                var sans = GetExtensions(certificate).Where(x => x.Oid?.Value == "2.5.29.17")
                                                     .Select(x => x.Format(true))
                                                     .SelectMany(x => x.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                                     .Select(x => x.Split('=')[1])
                                                     .ToList();
                var validCommonName = sans.Contains(expectedCommonName, StringComparer.OrdinalIgnoreCase);
                return validCommonName;
            }

            return policy == SslPolicyErrors.None;
        }

        private IEnumerable<AsnEncodedData> GetExtensions(X509Certificate2 certificate)
        {
            foreach (var extension in certificate.Extensions)
            {
                yield return new AsnEncodedData(extension.Oid, extension.RawData);
            }
        }
    }
}