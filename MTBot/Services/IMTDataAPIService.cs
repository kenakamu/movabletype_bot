using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MTBot.Models;
using Newtonsoft.Json.Linq;

namespace MTBot.Services
{
    public interface IMTDataAPIService
    {
        Task<string> AuthenticateAsync();
        Task<JToken> GetEntryAsync();
        Task<List<JToken>> GetSitesAsync();
        Task<List<JToken>> GetEntriesAsync();
        void Initialize(UserProfile userProfile);
        Task<JToken> CreateEntryAsync(string title, bool publish);
        Task UpdateEntryAsync(string body);
        Task UpdateImageAsync(string assetId, string description);
        Task<JToken> UploadImageAsync(Stream stream, string filename, string contentType);
    }
}