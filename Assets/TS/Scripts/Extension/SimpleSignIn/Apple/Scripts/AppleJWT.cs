using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.OpenSsl;

namespace Assets.SimpleSignIn.Apple.Scripts
{
    public static class AppleJWT
    {
        public static string CreateClientSecret(AppleAuthSettings settings)
        {
            var header = JsonConvert.SerializeObject(new { alg = "ES256", kid = settings.PrivateKeyId });
            // Apple may return "invalid_client" if you have inaccurate time, so we'll use max expiration time minus 1 hour.
            var payload = JsonConvert.SerializeObject(new { iss = settings.TeamId, iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 15777000 - 3600, aud = "https://appleid.apple.com", sub = settings.ClientId });
            var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(header));
            var payloadBasae64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
            var unsignedJwtData = $"{headerBase64}.{payloadBasae64}";
            var signature = GetSignature(unsignedJwtData, GetKeys(settings.PrivateKey));

            return $"{unsignedJwtData}.{Convert.ToBase64String(signature)}";
        }

        private static AsymmetricCipherKeyPair GetKeys(string data)
        {
            var byteArray = Encoding.ASCII.GetBytes(data);
            var stream = new MemoryStream(byteArray);

            using TextReader reader = new StreamReader(stream);
            var ecPrivateKeyParameters = (ECPrivateKeyParameters) new PemReader(reader).ReadObject();
            var q = ecPrivateKeyParameters.Parameters.G.Multiply(ecPrivateKeyParameters.D).Normalize();
            var ecPublicKeyParameters = new ECPublicKeyParameters(q, ecPrivateKeyParameters.Parameters);

            return new AsymmetricCipherKeyPair(ecPublicKeyParameters, ecPrivateKeyParameters);
        }

        private static byte[] GetSignature(string plainText, AsymmetricCipherKeyPair key)
        {
            var encoder = new UTF8Encoding();
            var inputData = encoder.GetBytes(plainText);

            var signer = SignerUtilities.GetSigner("SHA-256withPLAIN-ECDSA");
            signer.Init(true, key.Private);
            signer.BlockUpdate(inputData, 0, inputData.Length);

            return signer.GenerateSignature();
        }
    }
}