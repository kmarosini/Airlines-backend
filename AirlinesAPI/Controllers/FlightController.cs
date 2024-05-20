using AirlinesAPI.Models;
using AirlinesAPI.Services.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AirlinesAPI.Controllers
{
    [Route("api/GetFlightOffers")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private readonly AmadeusService _amadeusService;

        public FlightController(AmadeusService amadeusService)
        {
            _amadeusService = amadeusService;
        }

        [HttpPost("GetFlightOffers")]
        public async Task<IActionResult> GetFlightOffers([FromBody] FlightParameters flightParameters)
        {
            var flightOffers = await _amadeusService.GetFlightOffersAsync(flightParameters);

            if (flightOffers != null)
            {
                
                return Ok(flightOffers); // Return the flight offers if retrieval was successful
            }
            else
            {
                return StatusCode(500); // Return a server error if retrieval failed
            }
        }
    }
}
