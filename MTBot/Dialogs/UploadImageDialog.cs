using Microsoft.Bot.Builder.Dialogs;
using MTBot.Models;
using MTBot.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MTBot.Dialogs
{
    public class UploadImageDialog : CancelAndHelpDialog
    {
        IComputerVisionService computerVisionService;
        IMTDataAPIService dataService;

        public UploadImageDialog(IComputerVisionService computerVisionService, IMTDataAPIService dataService)
            : base(nameof(UploadImageDialog))
        {
            this.computerVisionService = computerVisionService;
            this.dataService = dataService;

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                UploadImageStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> UploadImageStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            dataService.Initialize(userProfile);
            // Get attachment info
            var attachment = stepContext.Context.Activity.Attachments.First();

            using (var client = HttpClientFactory.Create())
            {
                // Get attachment content
                var stream = await client.GetStreamAsync(attachment.ContentUrl);
                // Create formData to upload image
                var filename = attachment.Name != null ? attachment.Name : $"{Guid.NewGuid()}.{attachment.ContentType.Split('/')[1]}";
                var createdImage = await dataService.UploadImageAsync(stream, filename, attachment.ContentType);

                // Analyze Image and update image description
                stream = await client.GetStreamAsync(attachment.ContentUrl);
                var imageAnalysis = await computerVisionService.Analyze(stream);

                if (imageAnalysis.Description.Captions.Count > 0)
                {
                    await dataService.UpdateImageAsync(createdImage["id"].ToString(), imageAnalysis.Description.Captions.First().Text);
                }

                // Get body of current entry
                var body = (await dataService.GetEntryAsync())["body"].ToString().Replace("\"", "'");

                // Add new image and save.
                body += $"<p><img class='asset asset-image at-xid-1230255 mt-image-left' style='display: inline-block; float: left;' src='{createdImage["url"]}' alt='' width='120' height='120' /></p>";
             
                await dataService.UpdateEntryAsync(body);
            }            
            
            return await stepContext.EndDialogAsync(userProfile, cancellationToken);
        }
    }
}
