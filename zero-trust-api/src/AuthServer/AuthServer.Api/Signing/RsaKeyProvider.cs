using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Api.Signing;
public class RsaKeyProvider
{
    private readonly RSA _rsa;

    public string KeyId { get; }

    public RsaKeyProvider()
    {
        _rsa = RSA.Create(2048);
        KeyId = Guid.NewGuid().ToString("N")[..12];
    }

    public RsaSecurityKey GetPrivateKey() =>
        new(_rsa) { KeyId = KeyId };

    public RsaSecurityKey GetPublicKey() =>
        new(RSA.Create(_rsa.ExportParameters(false))) { KeyId = KeyId };
}