using System;
using System.Security.Cryptography;

namespace IdpUtilities
{
    public static class RandomUtil
    {
        /// <summary>
        /// 暗号学的に強度の高い疑似乱数バイト列から文字列を生成します。
        /// </summary>
        /// <param name="length">生成するバイト列の長さ</param>
        /// <returns>生成された文字列</returns>
        public static string GenerateRandomBytes(int length)
        {
            byte[] randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return BitConverter.ToString(randomBytes).Replace("-", string.Empty);
        }
    }
}

