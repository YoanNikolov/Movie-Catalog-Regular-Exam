using Movie_Catalog.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Movie_Catalog
{
    public class Tests
    {
        private RestClient client;
        private static string movieId;

        [SetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("ioannikolov97@gmail.com", "exam123");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:5000")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:5000");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateNewMovie_WithRequiredFields()
        {
            MovieDto movie = new MovieDto()
            {
                Title = "Die Hard",
                Description = "An NYPD officer battles terrorists inside a Los Angeles skyscraper on Christmas Eve."
            };

            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content)!;

            movieId = readyResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditMovie_WithCreatedMovieId_ShouldSuccess()
        {
            var editedMovie = new
            {
                title = "Die Hard 2",
                description = "John McClane faces terrorists at a Washington airport.",
                posterUrl = "https://example.com/diehard2.jpg",
                trailerLink = "https://www.youtube.com/watch?v=1TQ-pOvI6Xo",
                isWatched = true
            };

            RestRequest request = new RestRequest($"/api/Movie/Edit?movieId={movieId}", Method.Put);
            request.AddJsonBody(editedMovie);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content)!;

            Assert.That(readyResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            RestRequest request = new RestRequest("/api/Catalog/All", Method.Get);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var movies = JsonSerializer.Deserialize<List<MovieDto>>(response.Content)!;

            Assert.That(movies, Is.Not.Null);
            Assert.That(movies.Count, Is.GreaterThan(0));
        }

        [Order(4)]
        [Test]
        public void DeleteMovie_WithCreatedMovieId_ShouldSuccess()
        {
            RestRequest request = new RestRequest($"/api/Movie/Delete?movieId={movieId}", Method.Delete);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content)!;

            Assert.That(readyResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldFail()
        {
            var movie = new
            {
                title = "",
                description = ""
            };

            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldFail()
        {
            var editedMovie = new
            {
                title = "Fake",
                description = "Fake desc",
                posterUrl = "https://example.com/test.jpg",
                trailerLink = "https://youtube.com/watch?v=test",
                isWatched = true
            };

            RestRequest request = new RestRequest($"/api/Movie/Edit?movieId=invalid-id", Method.Put);
            request.AddJsonBody(editedMovie);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content)!;

            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldFail()
        {
            RestRequest request = new RestRequest($"/api/Movie/Delete?movieId=invalid-id", Method.Delete);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content)!;

            Assert.That(readyResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}