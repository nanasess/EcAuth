using System.Diagnostics;
using IdpUtilities.Security;

namespace IdentityProvider.Services
{
    /// <summary>
    /// Key Vault 経路の温まり具合。<c>/healthz</c> が返し、デプロイ後の verify がこれを見て
    /// E2E の開始を待つ（<c>production.yml</c> の Warm up endpoints）。
    /// </summary>
    public enum SecretProtectorWarmupStatus
    {
        /// <summary>Key Vault を使っていない（Development / 鍵未配線のフォールバック）。</summary>
        NotApplicable,
        /// <summary>起動直後で、まだ Key Vault へ一度も往復していない。</summary>
        Cold,
        /// <summary>暗号化→復号を 1 往復済み。MSI トークン・鍵・復号クライアントがキャッシュ済み。</summary>
        Warm,
        /// <summary>往復に失敗した。リクエスト経路は動くが初回は遅い。</summary>
        Failed,
    }

    /// <summary>
    /// <see cref="SecretProtectorWarmupService"/> の結果を保持する singleton。
    /// </summary>
    public sealed class SecretProtectorWarmupState
    {
        private volatile SecretProtectorWarmupStatus _status;

        public SecretProtectorWarmupState(bool enabled)
        {
            _status = enabled ? SecretProtectorWarmupStatus.Cold : SecretProtectorWarmupStatus.NotApplicable;
        }

        public SecretProtectorWarmupStatus Status => _status;
        public long? DurationMs { get; private set; }

        internal void MarkWarm(long durationMs)
        {
            DurationMs = durationMs;
            _status = SecretProtectorWarmupStatus.Warm;
        }

        internal void MarkFailed(long durationMs)
        {
            DurationMs = durationMs;
            _status = SecretProtectorWarmupStatus.Failed;
        }
    }

    /// <summary>
    /// 起動直後に <see cref="ISecretProtector"/> の暗号化→復号を 1 往復し、Key Vault 経路を温める。
    ///
    /// <para>
    /// 新しいインスタンスでは初回の復号が「鍵 GET で 401 → MSI トークン取得 → 鍵 GET → decrypt」を
    /// 通り、本番実測で 8 秒超かかった（MSI トークン取得だけで 5.5 秒。EcAuth#532 の本番 verify）。
    /// これが <c>register/verify</c> の初回応答を 23 秒に押し上げ、E2E のタイムアウトを招いた。
    /// <see cref="ISecretProtector"/> は singleton で、暗号化クライアント・鍵バージョン別の復号クライアント・
    /// MSI トークンをそれぞれキャッシュするため、起動時に 1 往復しておけばプロセスの寿命の間は
    /// 初回リクエストがこのコストを払わない。
    /// </para>
    /// <para>
    /// 起動はブロックしない（<see cref="BackgroundService"/> は host の start を待たせない）。失敗しても
    /// 起動は継続する。本番は可用性優先（<c>Program.cs</c> のシード失敗時と同じ方針）で、温まらなくても
    /// 遅いだけで機能は損なわれないため。
    /// </para>
    /// </summary>
    public sealed class SecretProtectorWarmupService : BackgroundService
    {
        // 往復させる平文。値に意味はない。復号結果のキャッシュ（decrypt-once）にこの 1 件が載るだけ。
        private const string Probe = "warm-up";

        private readonly ISecretProtector _protector;
        private readonly SecretProtectorWarmupState _state;
        private readonly ILogger<SecretProtectorWarmupService> _logger;

        public SecretProtectorWarmupService(
            ISecretProtector protector,
            SecretProtectorWarmupState state,
            ILogger<SecretProtectorWarmupService> logger)
        {
            _protector = protector;
            _state = state;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_state.Status == SecretProtectorWarmupStatus.NotApplicable)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var stored = await _protector.ProtectAsync(Probe, stoppingToken);
                var restored = await _protector.UnprotectAsync(stored, stoppingToken);
                sw.Stop();

                if (!string.Equals(restored, Probe, StringComparison.Ordinal))
                {
                    // 復号は通ったが値が食い違う。鍵配線の異常なので温まった扱いにはしない。
                    _state.MarkFailed(sw.ElapsedMilliseconds);
                    _logger.LogWarning(
                        "SecretProtector warm-up: 復号結果が一致しません ({ElapsedMs}ms)", sw.ElapsedMilliseconds);
                    return;
                }

                _state.MarkWarm(sw.ElapsedMilliseconds);
                _logger.LogInformation(
                    "SecretProtector warm-up: Key Vault 経路を温めました ({ElapsedMs}ms)", sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // シャットダウンに伴うキャンセルは正常終了。
            }
            catch (Exception ex)
            {
                sw.Stop();
                _state.MarkFailed(sw.ElapsedMilliseconds);
                _logger.LogWarning(ex,
                    "SecretProtector warm-up: 失敗しました。初回の復号が遅くなります ({ElapsedMs}ms)", sw.ElapsedMilliseconds);
            }
        }
    }
}
