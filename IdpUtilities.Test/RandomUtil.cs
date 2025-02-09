namespace IdpUtilities.Test
{
    public class RandomUtilTests
    {
        [Fact]
        public void GenerateRandomBytes_ReturnsCorrectLength()
        {
            int length = 16;
            string result = RandomUtil.GenerateRandomBytes(length);
            // 各バイトは2文字の16進数で表されるため、結果の長さは length * 2 になります
            Assert.Equal(length * 2, result.Length);
        }

        [Fact]
        public void GenerateRandomBytes_ReturnsUniqueValues()
        {
            int length = 16;
            string result1 = RandomUtil.GenerateRandomBytes(length);
            string result2 = RandomUtil.GenerateRandomBytes(length);
            Assert.NotEqual(result1, result2);
        }
    }
}