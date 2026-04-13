using CountryBlockerAPI.Repository;

namespace CountryBlockerAPI.BackgroundServices
{
    public class TemporalBlockCleanupService : BackgroundService
    {
        private readonly ICountryRepository _repo;
        private readonly ILogger<TemporalBlockCleanupService> _logger;

        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public TemporalBlockCleanupService(
            ICountryRepository repo,
            ILogger<TemporalBlockCleanupService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "TemporalBlockCleanupService started. Cleanup interval: {Interval} minutes.",
                Interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Interval, stoppingToken);

                    _logger.LogInformation(
                        "Running temporal block cleanup at {Time}", DateTime.UtcNow);

                    _repo.RemoveExpiredTemporalBlocks();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during temporal block cleanup.");
                }
            }

            _logger.LogInformation("TemporalBlockCleanupService stopped.");
        }
    }
}
