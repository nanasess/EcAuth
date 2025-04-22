using System;
using System.Threading.Tasks;
using Xunit;

namespace IdpUtilities.Test
{
    public class IronTests
    {
        private readonly string password = "<strong_password_string_of_at_least_32_characters>";
        private readonly Iron.Options options = new Iron.Options();

        public class TestModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public async Task SealAndUnseal_ReturnsOriginalObject()
        {
            var model = new TestModel { Name = "John Doe", Age = 30 };

            var sealedData = await Iron.Seal(model, password, options);
            var unsealedModel = await Iron.Unseal<TestModel>(sealedData, password, options);

            Assert.Equal(model.Name, unsealedModel.Name);
            Assert.Equal(model.Age, unsealedModel.Age);
        }

        [Fact]
        public async Task Seal_ThrowsException_WhenPasswordIsEmpty()
        {
            var model = new TestModel { Name = "John Doe", Age = 30 };

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await Iron.Seal(model, "", options);
            });
        }

        [Fact]
        public async Task Unseal_ThrowsException_WhenDataIsTampered()
        {
            var model = new TestModel { Name = "John Doe", Age = 30 };

            var sealedData = await Iron.Seal(model, password, options);
            var tamperedData = sealedData.Replace("Fe26.2", "Fe26.3");

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await Iron.Unseal<TestModel>(tamperedData, password, options);
            });
        }

        [Fact]
        public async Task Unseal_ThrowsException_WhenPasswordIsIncorrect()
        {
            var model = new TestModel { Name = "John Doe", Age = 30 };

            var sealedData = await Iron.Seal(model, password, options);

            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await Iron.Unseal<TestModel>(sealedData, "wrongpassword", options);
            });
        }

        [Fact]
        public async Task SealAndUnseal_WorksWithSpecialCharacters()
        {
            // データに特殊文字（Base64エンコードで+や/になる文字）を含める
            var model = new TestModel {
                Name = "特殊文字テスト+/=?&%$#@!{}[]",
                Age = 42
            };

            var sealedData = await Iron.Seal(model, password, options);
            var unsealedModel = await Iron.Unseal<TestModel>(sealedData, password, options);

            Assert.Equal(model.Name, unsealedModel.Name);
            Assert.Equal(model.Age, unsealedModel.Age);
        }

        [Fact]
        public async Task Seal_GeneratesUrlSafeString()
        {
            // 複雑なデータを作成して確実にBase64エンコード時に特殊文字が含まれるようにする
            var complexData = new byte[1000];
            new Random(123).NextBytes(complexData); // 固定シードで再現性を確保
            var model = new TestModel {
                Name = Convert.ToBase64String(complexData),
                Age = 100
            };

            var sealedData = await Iron.Seal(model, password, options);

            // シールされた文字列にURL安全でない文字が含まれていないことを確認
            Assert.DoesNotContain('+', sealedData);
            Assert.DoesNotContain('/', sealedData);
            Assert.DoesNotContain('=', sealedData);

            // アンシールも正常に動作することを確認
            var unsealedModel = await Iron.Unseal<TestModel>(sealedData, password, options);
            Assert.Equal(model.Name, unsealedModel.Name);
            Assert.Equal(model.Age, unsealedModel.Age);
        }
    }
}
