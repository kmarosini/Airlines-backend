﻿using AirlinesAPI.Models;
using AirlinesAPI.Services.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace AirlinesAPI.Services.Implementation
{
    public class AmadeusService : IAmadeusService
    {
        private readonly TokenService _tokenService;
        private readonly string _connectionString;
        private readonly AmadeusConfiguration _amadeusConfig;



        public AmadeusService(TokenService tokenService, IOptions<AmadeusConfiguration> amadeusConfig, IConfiguration configuration)
        {
            _tokenService = tokenService;
            _amadeusConfig = amadeusConfig.Value;
            _connectionString = configuration.GetConnectionString("FlightOffersDB");
        }

        public async Task<FlightOffers> GetFlightOffersAsync(FlightParameters flightParameters)
        {
            // Check if the search parameters already exist in the database
            var existingOffers = GetFlightOffersFromDatabase(flightParameters);
            if (existingOffers != null)
            {
                return existingOffers;
            }

            // Retrieve the access token using the TokenService instance
            string accessToken = await _tokenService.GetTokenAsync();

            if (accessToken == null)
            {
                Console.WriteLine("Failed to get access token.");
                return null;
            }

            // Constructing the query parameters
            var queryString = $"?originLocationCode={flightParameters.DepartureAirport}" +
                              $"&destinationLocationCode={flightParameters.DestinationAirport}" +
                              $"&departureDate={flightParameters.DepartureDate}" +
                              $"&returnDate={flightParameters.ReturnDate}" +
                              $"&adults={flightParameters.NumberOfPassengers}" +
                              $"&currencyCode={flightParameters.Currency}" +
                              $"&max={flightParameters.Max}";

            // Constructing the full URL with query parameters
            string urlWithQuery = _amadeusConfig.BaseUrl + queryString;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(urlWithQuery);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var flightOffers = JsonSerializer.Deserialize<FlightOffers>(responseContent);

                    if (flightOffers != null)
                    {
                        SaveFlightOffersToDatabase(flightParameters, flightOffers);
                    }

                    return flightOffers;
                }
                else
                {
                    Console.WriteLine("Failed to fetch flight offers: " + await response.Content.ReadAsStringAsync());
                    return null;
                }
            }
        }

        private FlightOffers GetFlightOffersFromDatabase(FlightParameters flightParameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var command = new SqlCommand(
                    "SELECT DepartureAirport, DestinationAirport, DepartureDate, ReturnDate, " +
                    "OutboundStops, InboundStops, NumberOfPassengers, Currency, TotalPrice " +
                    "FROM FlightOffersData " +
                    "WHERE DepartureAirport = @DepartureAirport " +
                    "AND DestinationAirport = @DestinationAirport " +
                    "AND DepartureDate = @DepartureDate " +
                    "AND (ReturnDate IS NULL OR ReturnDate = @ReturnDate) " +
                    "AND NumberOfPassengers = @NumberOfPassengers " +
                    "AND Currency = @Currency", connection);

                command.Parameters.AddWithValue("@DepartureAirport", flightParameters.DepartureAirport);
                command.Parameters.AddWithValue("@DestinationAirport", flightParameters.DestinationAirport);
                command.Parameters.AddWithValue("@DepartureDate", flightParameters.DepartureDate);
                command.Parameters.AddWithValue("@ReturnDate", (object)flightParameters.ReturnDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@NumberOfPassengers", flightParameters.NumberOfPassengers);
                command.Parameters.AddWithValue("@Currency", flightParameters.Currency);

                // Log the SQL command and parameter values
                Console.WriteLine("Executing SQL Command: {0}", command.CommandText);
                foreach (SqlParameter parameter in command.Parameters)
                {
                    Console.WriteLine("Parameter {0}: {1}", parameter.ParameterName, parameter.Value);
                }

                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        var flightOffers = new FlightOffers
                        {
                            data = new List<FlightOffers.Data>()
                        };

                        while (reader.Read())
                        {
                            DateTime departureDate;
                            DateTime.TryParse(reader["DepartureDate"].ToString(), out departureDate);

                            DateTime returnDate;
                            DateTime.TryParse(reader["ReturnDate"].ToString(), out returnDate);

                            var offer = new FlightOffers.Data
                            {
                                itineraries = new List<FlightOffers.Itinerary>
                        {
                            new FlightOffers.Itinerary
                            {
                                segments = new List<FlightOffers.Segment>
                                {
                                    new FlightOffers.Segment
                                    {
                                        departure = new FlightOffers.Departure
                                        {
                                            iataCode = reader["DepartureAirport"].ToString(),
                                            at = departureDate // Set the parsed departure date
                                        },
                                        arrival = new FlightOffers.Arrival
                                        {
                                            iataCode = reader["DestinationAirport"].ToString(),
                                            at = returnDate // Set the parsed return date
                                        }
                                    }
                                },
                                duration = "N/A" // Duration is not available in the table
                            }
                        },
                                price = new FlightOffers.Price
                                {
                                    currency = reader["Currency"].ToString(),
                                    total = reader["TotalPrice"].ToString()
                                },
                                numberOfBookableSeats = (int)reader["NumberOfPassengers"]
                            };

                            flightOffers.data.Add(offer);
                        }

                        return flightOffers;
                    }
                }
            }

            return null;
        }



        private void SaveFlightOffersToDatabase(FlightParameters flightParameters, FlightOffers flightOffers)
        {
            // Set the current thread's culture to the invariant culture
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Insert search parameters
                    var searchParamsCommand = new SqlCommand(
                        "INSERT INTO SearchParams (DepartureAirport, DestinationAirport, DepartureDate, ReturnDate, NumberOfPassengers, Currency, Max) " +
                        "OUTPUT INSERTED.Id " +
                        "VALUES (@DepartureAirport, @DestinationAirport, @DepartureDate, @ReturnDate, @NumberOfPassengers, @Currency, @Max)", connection);

                    searchParamsCommand.Parameters.AddWithValue("@DepartureAirport", flightParameters.DepartureAirport);
                    searchParamsCommand.Parameters.AddWithValue("@DestinationAirport", flightParameters.DestinationAirport);
                    searchParamsCommand.Parameters.AddWithValue("@DepartureDate", flightParameters.DepartureDate);
                    searchParamsCommand.Parameters.AddWithValue("@ReturnDate", (object)flightParameters.ReturnDate ?? DBNull.Value);
                    searchParamsCommand.Parameters.AddWithValue("@NumberOfPassengers", flightParameters.NumberOfPassengers);
                    searchParamsCommand.Parameters.AddWithValue("@Currency", flightParameters.Currency);
                    searchParamsCommand.Parameters.AddWithValue("@Max", (object)flightParameters.Max ?? DBNull.Value);

                    var searchParamsId = (int)searchParamsCommand.ExecuteScalar();

                    // Insert flight offers
                    foreach (var offer in flightOffers.data)
                    {
                        foreach (var itinerary in offer.itineraries)
                        {
                            var departureSegment = itinerary.segments.First();
                            var returnSegment = itinerary.segments.Last();

                            var command = new SqlCommand(
                                "INSERT INTO FlightOffersData (DepartureAirport, DestinationAirport, DepartureDate, ReturnDate, OutboundStops, InboundStops, NumberOfPassengers, Currency, TotalPrice, SearchParamsId) " +
                                "VALUES (@DepartureAirport, @DestinationAirport, @DepartureDate, @ReturnDate, @OutboundStops, @InboundStops, @NumberOfPassengers, @Currency, @TotalPrice, @SearchParamsId)", connection);

                            command.Parameters.AddWithValue("@SearchParamsId", searchParamsId);
                            command.Parameters.AddWithValue("@DepartureAirport", departureSegment.departure.iataCode);
                            command.Parameters.AddWithValue("@DestinationAirport", returnSegment.arrival.iataCode);
                            command.Parameters.AddWithValue("@DepartureDate", departureSegment.departure.at);
                            command.Parameters.AddWithValue("@ReturnDate", returnSegment.arrival.at);
                            command.Parameters.AddWithValue("@OutboundStops", itinerary.segments.Count - 1);
                            command.Parameters.AddWithValue("@InboundStops", itinerary.segments.Count - 1);
                            command.Parameters.AddWithValue("@NumberOfPassengers", offer.travelerPricings.Count);
                            command.Parameters.AddWithValue("@Currency", offer.price.currency);
                            command.Parameters.AddWithValue("@TotalPrice", Decimal.Parse(offer.price.total));

                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
