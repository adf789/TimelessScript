using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Utility
{
    public static class Encryption
    {
        public static byte[] Encrypt(byte[] plainBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(GetByteKey());
                aes.IV = Encoding.UTF8.GetBytes(GetByteIV());

                using ICryptoTransform encryptor = aes.CreateEncryptor();
                return PerformCryptography(plainBytes, encryptor);
            }
        }

        public static byte[] Decrypt(byte[] encryptedBytes)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(GetByteKey());
            aes.IV = Encoding.UTF8.GetBytes(GetByteIV());

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return PerformCryptography(encryptedBytes, decryptor);
        }

        public static string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(GetStringKey());
            aes.IV = Encoding.UTF8.GetBytes(GetStringIV());

            ICryptoTransform encryptor = aes.CreateEncryptor();
            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new(cs)) sw.Write(plainText);
            return Convert.ToBase64String(ms.ToArray()).Replace("/", "_").Replace("+", "-");
        }

        public static string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(GetStringKey());
            aes.IV = Encoding.UTF8.GetBytes(GetStringIV());

            ICryptoTransform decryptor = aes.CreateDecryptor();
            // Base64 디코딩 전에 치환 복원
            string base64 = cipherText.Replace("_", "/").Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(base64);

            using MemoryStream ms = new(buffer);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }

        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static string GetByteKey()
        {
            return new string(new[] { 'n', 'e', 'r', 'd', 'y', 's', 't', 'a', 'r', '_', 'k', 'e', 'y', '_', 's', 'e', 'c', '_', '2', '5', '0', '4', '0', '8' });
        }

        public static string GetByteIV()
        {
            return new string(new[] { 'i', 'v', '_', 'n', 'd', 'y', 's', 't', '_', '8', '0', '4', '0', '5', '2', '!' });
        }

        public static string GetStringKey()
        {
            return new string(new[] { 'n', 'e', 'r', 'd', 'y', 's', 't', 'a', 'r', '_', 'k', 'e', 'y', '_', 's', 'e', 'c', '_', '2', '5', '0', '7', '1', '6' });
        }

        public static string GetStringIV()
        {
            return new string(new[] { 'i', 'v', '_', 'n', 'd', 'y', 's', 't', '_', '6', '1', '7', '0', '5', '2', '!' });
        }
    }
}
