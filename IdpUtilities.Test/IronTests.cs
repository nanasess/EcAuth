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
    }
}
