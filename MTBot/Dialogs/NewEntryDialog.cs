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
    public class NewEntryDialog : CancelAndHelpDialog
    {
        private string title;
        private IMTDataAPIService dataService;

        public NewEntryDialog(IMTDataAPIService dataService)
            : base(nameof(NewEntryDialog))
        {
            this.dataService = dataService;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SelectSiteStepAsync,
                EntryNameStepAsync,
                EntryPublishStepAsync,
                CreateEntryStepAsync,
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
                        Prompt = MessageFactory.Text("�T�C�g�́H"),
                        Choices = choices
                    },
                    cancellationToken);            
        }

        private async Task<DialogTurnResult> EntryNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                    nameof(TextPrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("�L���̖��O�́H")
                    },
                    cancellationToken);

        }

        private async Task<DialogTurnResult> EntryPublishStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            title = stepContext.Result.ToString();
            return await stepContext.PromptAsync(
                    nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("���J����H"),
                        Choices = ChoiceFactory.ToChoices(new List<string>() { "�͂�", "������" })
                    },
                    cancellationToken);;

        }

        private async Task<DialogTurnResult> CreateEntryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            var publish = ((FoundChoice)stepContext.Result).Value;
            dataService.Initialize(userProfile);

            var entry = await dataService.CreateEntryAsync(title, publish == "�͂�");
            userProfile.Entry = entry["id"].ToString();
            await stepContext.Context.SendActivityAsync($"�V�����L���� {entry["permalink"].ToString()} �������B�ʐ^�𑗂��Ă�");
            return await stepContext.EndDialogAsync(userProfile, cancellationToken);
        }
    }
}
