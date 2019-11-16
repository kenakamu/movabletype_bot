using MTBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MTBot.Services
{
    public class MTDataAPIService : IMTDataAPIService
    {
        private UserProfile userProfile;
        private string baseUri;

        public void Initialize(UserProfile userProfile)
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Domain == null)
                throw new ArgumentNullException(nameof(UserProfile.Domain));

            this.baseUri = $"https://{userProfile.Domain}.movabletype.io/.data-api/v4/";
            this.userProfile = userProfile;
        }

        private async Task<HttpClient> GetClientAsync(bool requireAuth = false)
        {
            if (string.IsNullOrEmpty(baseUri))
                throw new NullReferenceException(nameof(baseUri));

            var client = HttpClientFactory.Create();
            client.BaseAddress = new Uri(baseUri);
            if (requireAuth)
            {
                var token = await AuthenticateAsync();
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-MT-Authorization", $"MTAuth accessToken={token}");
            }
            return client;
        }

        public async Task<string> AuthenticateAsync()
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Username == null)
                throw new ArgumentNullException(nameof(UserProfile.Username));
            if (userProfile.Password == null)
                throw new ArgumentNullException(nameof(UserProfile.Password));

            var client = await GetClientAsync();
            var res = await client.PostAsync("authentication",
                    new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("username", userProfile.Username),
                        new KeyValuePair<string, string>("password", userProfile.Password),
                        new KeyValuePair<string, string>("clientId", "myclient_id")
                    }));
            if (res.IsSuccessStatusCode)
            {
                var result = JToken.Parse(await res.Content.ReadAsStringAsync());
                return result["accessToken"].ToString();
            }
            else
            {
                throw new UnauthorizedAccessException();
            }
        }

        public async Task<List<JToken>> GetSitesAsync()
        {
            var client = await GetClientAsync();
            var res = await client.GetAsync($"sites");
            if (res.IsSuccessStatusCode)
            {
                var sites = JToken.Parse(await res.Content.ReadAsStringAsync());
                return JArray.Parse(sites["items"].ToString()).ToList();
            }
            else
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task<List<JToken>> GetEntriesAsync()
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Site == null)
                throw new ArgumentNullException(nameof(UserProfile.Site));

            var client = await GetClientAsync(true);
            var res = await client.GetAsync($"sites/{userProfile.Site}/entries");
            if (res.IsSuccessStatusCode)
            {
                var sites = JToken.Parse(await res.Content.ReadAsStringAsync());
                return JArray.Parse(sites["items"].ToString()).ToList();
            }
            else
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task<JToken> UploadImageAsync(Stream stream, string filename, string contentType)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));
            if (contentType == null)
                throw new ArgumentNullException(nameof(contentType));

            var client = await GetClientAsync(true);
            // Create formData to upload image
            var formData = new MultipartFormDataContent();
            HttpContent content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formData.Add(content, "\"file\"", $"\"{filename}\"");
            content = new StringContent(userProfile.Site);
            formData.Add(content, "\"site_id\"");
            content = new StringContent("true");
            formData.Add(content, "\"autoRenameIfExists\"");
            content = new StringContent("true");
            formData.Add(content, "\"normalizeOrientation\"");

            // Upload image and store the result.
            var res = await client.PostAsync($"assets/upload", formData);
            if (res.IsSuccessStatusCode)
            {
                var createdImage = JToken.Parse(await res.Content.ReadAsStringAsync());
                return createdImage;
            }
            else
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task UpdateImageAsync(string assetId, string description)
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Site == null)
                throw new ArgumentNullException(nameof(UserProfile.Site));
            if (string.IsNullOrEmpty(assetId))
                throw new ArgumentNullException(nameof(assetId));
            if (string.IsNullOrEmpty(description))
                throw new ArgumentNullException(nameof(description));

            var client = await GetClientAsync(true);
            var res = await client.PutAsync($"sites/{userProfile.Site}/assets/{assetId}",
                   new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("asset", $"{{\"description\":\"{description}\"}}")
                   }));
            if (!res.IsSuccessStatusCode)
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task<JToken> GetEntryAsync()
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Site == null)
                throw new ArgumentNullException(nameof(UserProfile.Site));
            if (userProfile.Entry == null)
                throw new ArgumentNullException(nameof(UserProfile.Entry));

            var client = await GetClientAsync(true);
            var res = await client.GetAsync($"sites/{userProfile.Site}/entries/{userProfile.Entry}");
            if (res.IsSuccessStatusCode)
            {
                return JToken.Parse(await res.Content.ReadAsStringAsync());
            }
            else
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task<JToken> CreateEntryAsync(string title, bool publish)
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Site == null)
                throw new ArgumentNullException(nameof(UserProfile.Site));
          
            var client = await GetClientAsync(true);
            var entry = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("entry", $"{{\"title\":\"{title}\",\"status\":\"{(publish==true?"Publish":"Draft")}\"}}")
                   });
            var res = await client.PostAsync($"sites/{userProfile.Site}/entries", entry);
            if (res.IsSuccessStatusCode)
            {
                return JToken.Parse(await res.Content.ReadAsStringAsync());
            }
            else
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }

        public async Task UpdateEntryAsync(string body)
        {
            if (userProfile == null)
                throw new ArgumentNullException(nameof(UserProfile));
            if (userProfile.Site == null)
                throw new ArgumentNullException(nameof(UserProfile.Site));
            if (userProfile.Entry == null)
                throw new ArgumentNullException(nameof(UserProfile.Entry));

            var client = await GetClientAsync(true);
            var res = await client.PutAsync($"sites/{userProfile.Site}/entries/{userProfile.Entry}",
                   new FormUrlEncodedContent(new List<KeyValuePair<string, string>>() {
                        new KeyValuePair<string, string>("entry", $"{{\"body\":\"{body}\"}}")
                   }));
            if (!res.IsSuccessStatusCode)
            {
                throw new HttpRequestException(res.ReasonPhrase);
            }
        }
    }
}
