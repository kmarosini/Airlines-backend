namespace AirlinesAPI.Services.Contracts
{
    public interface ITokenService
    {
        Task<string> GetTokenAsync();

    }
}
