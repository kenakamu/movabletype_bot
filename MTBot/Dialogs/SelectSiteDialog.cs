using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using MTBot.Models;
using MTBot.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MTBot.Dialogs
{
    public class SelectSiteDialog : CancelAndHelpDialog
    {
        private IMTDataAPIService dataService;

        public SelectSiteDialog(IMTDataAPIService dataService)
            : base(nameof(SelectSiteDialog))
        {
            this.dataService = dataService;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SelectSiteStepAsync,
                SelectEntryStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectSiteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            dataService.Initialize(userProfile);
            var choices = new List<Choice>();
           
            var sites = await dataService.GetSitesAsync();
            sites.ForEach(x => choices.Add(new Choice($"{x["name"].ToString()}:{x["id"].ToString()}")));
            return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("サイトは？"),
                        Choices = choices
                    },
                    cancellationToken);            
        }

        private async Task<DialogTurnResult> SelectEntryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            userProfile.Site = ((FoundChoice)stepContext.Result).Value.Split(":")[1];
            dataService.Initialize(userProfile);

            var choices = new List<Choice>();
     
            var entries = await dataService.GetEntriesAsync();
            entries.ForEach(x => choices.Add(new Choice($"{x["title"].ToString()}:{x["id"].ToString()}")));
            return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("記事は？"),
                        Choices = choices
                    },
                    cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            userProfile.Entry = ((FoundChoice)stepContext.Result).Value.Split(":")[1];
            await stepContext.Context.SendActivityAsync("写真を送ってね");
            return await stepContext.EndDialogAsync(userProfile, cancellationToken);
        }
    }
}
