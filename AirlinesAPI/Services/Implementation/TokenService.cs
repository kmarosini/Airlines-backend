using AirlinesAPI.Models;
using AirlinesAPI.Services.Contracts;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AirlinesAPI.Services.Implementation
{
    public class TokenService : ITokenService
    {
        private readonly AmadeusConfiguration _amadeusConfig;
        private readonly AmadeusTokenConfiguration _amadeusTokenConfig;

        public TokenService(IOptions<AmadeusConfiguration> amadeusConfig, IOptions<AmadeusTokenConfiguration> amadeusTokenConfig)
        {
            _amadeusConfig = amadeusConfig.Value;
            _amadeusTokenConfig = amadeusTokenConfig.Value;
        }

        public async Task<string> GetTokenAsync()
        {
            var payload = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _amadeusConfig.ClientId),
                new KeyValuePair<string, string>("client_secret", _amadeusConfig.ClientSecret)
            });

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(_amadeusTokenConfig.TokenUrl, payload);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenJson = System.Text.Json.JsonDocument.Parse(responseContent);
                    var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
                    return accessToken;
                }
                else
                {
                    Console.WriteLine("Failed to acquire access token: " + await response.Content.ReadAsStringAsync());
                    return null;
                }
            }
        }
    }
}
