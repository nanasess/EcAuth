using IdentityProvider.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityProvider.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezinaaaaaaaag", "Bracing", "Chillyaaa", "Coolaaaaa", "Mild", "Warm", "Balmy", "Hotaaaa", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        private readonly EcAuthDbContext _context;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, EcAuthDbContext dbContext)
        {
            _logger = logger;
            _context = dbContext;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            Client client = _context.Clients.FirstOrDefault();
            if (client != null)
            {
                _logger.LogInformation("Client: {0}", client.AppName);
            }
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
            })
            .ToArray();
        }
    }
}
