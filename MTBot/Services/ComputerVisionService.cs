using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;

namespace MTBot.Services
{
    
    public class ComputerVisionService : IComputerVisionService
    {
        string subscriptionKey;
        string endpoint;
        private ComputerVisionClient client;

        public ComputerVisionService(IConfiguration configuration)
        {
            subscriptionKey = configuration["ComputerVisionSubscriptionKey"];
            endpoint = configuration["ComputerVisionEndPoint"];
        }

        public async Task<ImageAnalysis> Analyze(Stream image)
        {
            var client = GetClient();
            List<VisualFeatureTypes> features = new List<VisualFeatureTypes>()
            {
                VisualFeatureTypes.Description
            };
            return await client.AnalyzeImageInStreamAsync(image, features);
        }

        private ComputerVisionClient GetClient()
        {
            return client == null ?
                new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey)){ Endpoint = endpoint } : 
                client;
        }        
    }
}
