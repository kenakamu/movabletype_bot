using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.IO;
using System.Threading.Tasks;

namespace MTBot.Services
{
    public interface IComputerVisionService
    {
        Task<ImageAnalysis> Analyze(Stream image);
    }
}