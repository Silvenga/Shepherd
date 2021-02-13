using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Shepherd.Core.Models;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
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

        public void AssertValidConfiguration(VaultAuthConfiguration authConfiguration)
        {
            CreateAuthMethod(authConfiguration);
        }

        public IVaultClient CreateClient(Uri address, VaultAuthConfiguration authConfiguration, string? expectedHostname)
        {
            return CreateCreate(address, CreateAuthMethod(authConfiguration), expectedHostname);
        }

        public IVaultClient CreateClient(Uri address, string? expectedHostname)
        {
            return CreateCreate(address, new TokenAuthMethodInfo(Guid.NewGuid().ToString()), expectedHostname);
        }

        private IVaultClient CreateCreate(Uri address, IAuthMethodInfo authMethodInfo, string? expectedHostname)
        {
            var settings = new VaultClientSettings(address.ToString(), authMethodInfo)
            {
                MyHttpClientProviderFunc = CreateHttpClientHandler(expectedHostname)
            };
            return new VaultClient(settings);
        }

        private IAuthMethodInfo CreateAuthMethod(VaultAuthConfiguration authConfiguration)
        {
            var provider = authConfiguration.Provider?.ToLower() ?? throw new ArgumentException("Key 'Auth:Provider' is invalid.");
            switch (provider)
            {
                case "approle":
                    var roleId = authConfiguration.AppRole.RoleId ?? throw new ArgumentException("Key 'Auth:AppRole:RoleId' is invalid.");
                    var secretId = authConfiguration.AppRole.SecretId ?? throw new ArgumentException("Key 'Auth:AppRole:SecretId' is invalid.");
                    var mountPoint = authConfiguration.AppRole.MountPath ?? throw new ArgumentException("Key 'Auth:AppRole:MountPath' is invalid.");
                    return new AppRoleAuthMethodInfo(mountPoint, roleId, secretId);
                default:
                    throw new ArgumentException($"Auth provider '{provider}' is invalid.");
            }
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
                var onlyHasNameValidationError = policy == SslPolicyErrors.RemoteCertificateNameMismatch;
                if (onlyHasNameValidationError && certificate != null)
                {
                    _logger.LogTrace($"Certificate is trusted, but the hostname does not match, validating for certificate hostname '{expectedHostname}'.");

                    var sans = X509SubjectAlternativeNameParser.ParseSubjectAlternativeNames(certificate).ToList();
                    var validCommonName = sans.Contains(expectedHostname, StringComparer.OrdinalIgnoreCase);
                    if (!validCommonName)
                    {
                        _logger.LogTrace($"Expected hostname '{expectedHostname}' to be in the list [{string.Join(", ", sans)}].");
                    }

                    return validCommonName;
                }

                var unconditionallyValid = policy == SslPolicyErrors.None;
                if (!unconditionallyValid)
                {
                    _logger.LogTrace($"Certificate '{certificate?.SubjectName.Name}' cannot be trusted (Reason: '{policy}').");
                }

                return unconditionallyValid;
            };
        }

        // Adapted from https://github.com/dotnet/wcf/blob/a9984490334fdc7d7382cae3c7bc0c8783eacd16/src/System.Private.ServiceModel/src/System/IdentityModel/Claims/X509CertificateClaimSet.cs
        // We don't have a strongly typed extension to parse Subject Alt Names, so we have to do a workaround 
        // to figure out what the identifier, delimiter, and separator is by using a well-known extension
        // If https://github.com/dotnet/corefx/issues/22068 ever goes anywhere, we can remove this
        private static class X509SubjectAlternativeNameParser
        {
            private const string SanOid = "2.5.29.17";

            private static readonly string PlatformIdentifier;
            private static readonly char PlatformDelimiter;
            private static readonly string PlatformSeparator;

            static X509SubjectAlternativeNameParser()
            {
                // Extracted a well-known X509Extension
                byte[] x509ExtensionBytes =
                {
                    48, 36, 130, 21, 110, 111, 116, 45, 114, 101, 97, 108, 45, 115, 117, 98, 106, 101, 99,
                    116, 45, 110, 97, 109, 101, 130, 11, 101, 120, 97, 109, 112, 108, 101, 46, 99, 111, 109
                };
                const string subjectName1 = "not-real-subject-name";

                X509Extension x509Extension = new(SanOid, x509ExtensionBytes, true);
                string x509ExtensionFormattedString = x509Extension.Format(false);

                // Each OS has a different dNSName identifier and delimiter
                // On Windows, dNSName == "DNS Name" (localizable), on Linux, dNSName == "DNS"
                // e.g.,
                // Windows: x509ExtensionFormattedString is: "DNS Name=not-real-subject-name, DNS Name=example.com"
                // Linux:   x509ExtensionFormattedString is: "DNS:not-real-subject-name, DNS:example.com"
                // Parse: <identifier><delimter><value><separator(s)>

                var delimiterIndex = x509ExtensionFormattedString.IndexOf(subjectName1, StringComparison.Ordinal) - 1;
                PlatformDelimiter = x509ExtensionFormattedString[delimiterIndex];

                // Make an assumption that all characters from the the start of string to the delimiter 
                // are part of the identifier
                PlatformIdentifier = x509ExtensionFormattedString.Substring(0, delimiterIndex);

                var separatorFirstChar = delimiterIndex + subjectName1.Length + 1;
                var separatorLength = 1;
                for (var i = separatorFirstChar + 1; i < x509ExtensionFormattedString.Length; i++)
                {
                    // We advance until the first character of the identifier to determine what the
                    // separator is. This assumes that the identifier assumption above is correct
                    if (x509ExtensionFormattedString[i] == PlatformIdentifier[0])
                    {
                        break;
                    }

                    separatorLength++;
                }

                PlatformSeparator = x509ExtensionFormattedString.Substring(separatorFirstChar, separatorLength);
            }

            public static IEnumerable<string> ParseSubjectAlternativeNames(X509Certificate2 cert)
            {
                return cert.Extensions
                           .Cast<X509Extension>()
                           .Where(ext => ext.Oid?.Value?.Equals(SanOid) == true) // Only use SAN extensions
                           .Select(ext => new AsnEncodedData(ext.Oid, ext.RawData).Format(false)) // Decode from ASN
                           // This is dumb but AsnEncodedData.Format changes based on the platform, so our static initialization code handles making sure we parse it correctly
                           .SelectMany(text => text.Split(PlatformSeparator, StringSplitOptions.RemoveEmptyEntries))
                           .Select(text => text.Split(PlatformDelimiter))
                           .Where(x => x[0] == PlatformIdentifier)
                           .Select(x => x[1]);
            }
        }
    }
}