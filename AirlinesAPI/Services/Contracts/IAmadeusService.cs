using AirlinesAPI.Models;

namespace AirlinesAPI.Services.Contracts
{
    public interface IAmadeusService
    {
        Task<FlightOffers> GetFlightOffersAsync(FlightParameters flightParameters);
    }
}
