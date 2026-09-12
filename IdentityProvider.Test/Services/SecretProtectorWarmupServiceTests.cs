using IdentityProvider.Services;
using IdpUtilities.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IdentityProvider.Test.Services
{
    public class SecretProtectorWarmupServiceTests
    {
        private static SecretProtectorWarmupService Create(ISecretProtector protector, SecretProtectorWarmupState state)
            => new(protector, state, NullLogger<SecretProtectorWarmupService>.Instance);

        [Fact]
        public async Task 暗号化と復号が往復すれば_Warm_になる()
        {
            var protector = new Mock<ISecretProtector>(MockBehavior.Strict);
            protector.Setup(p => p.ProtectAsync("warm-up", It.IsAny<CancellationToken>()))
                .ReturnsAsync("kv:v1:xxxx");
            protector.Setup(p => p.UnprotectAsync("kv:v1:xxxx", It.IsAny<CancellationToken>()))
                .ReturnsAsync("warm-up");
            var state = new SecretProtectorWarmupState(enabled: true);
            Assert.Equal(SecretProtectorWarmupStatus.Cold, state.Status);

            var service = Create(protector.Object, state);
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;

            Assert.Equal(SecretProtectorWarmupStatus.Warm, state.Status);
            Assert.NotNull(state.DurationMs);
            protector.VerifyAll();
        }

        [Fact]
        public async Task 復号結果が一致しなければ_Failed_になる()
        {
            var protector = new Mock<ISecretProtector>();
            protector.Setup(p => p.ProtectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("kv:v1:xxxx");
            protector.Setup(p => p.UnprotectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("something-else");
            var state = new SecretProtectorWarmupState(enabled: true);

            var service = Create(protector.Object, state);
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;

            Assert.Equal(SecretProtectorWarmupStatus.Failed, state.Status);
        }

        [Fact]
        public async Task 例外が出ても伝播せず_Failed_になる()
        {
            // 本番は可用性優先。Key Vault に届かなくても起動を止めない。
            var protector = new Mock<ISecretProtector>();
            protector.Setup(p => p.ProtectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Key Vault unreachable"));
            var state = new SecretProtectorWarmupState(enabled: true);

            var service = Create(protector.Object, state);
            await service.StartAsync(CancellationToken.None);
            var ex = await Record.ExceptionAsync(() => service.ExecuteTask!);

            Assert.Null(ex);
            Assert.Equal(SecretProtectorWarmupStatus.Failed, state.Status);
        }

        [Fact]
        public async Task 平文フォールバック時は_Key_Vault_に触らない()
        {
            var protector = new Mock<ISecretProtector>(MockBehavior.Strict);
            var state = new SecretProtectorWarmupState(enabled: false);
            Assert.Equal(SecretProtectorWarmupStatus.NotApplicable, state.Status);

            var service = Create(protector.Object, state);
            await service.StartAsync(CancellationToken.None);
            await service.ExecuteTask!;

            Assert.Equal(SecretProtectorWarmupStatus.NotApplicable, state.Status);
            protector.VerifyNoOtherCalls();
        }
    }
}
