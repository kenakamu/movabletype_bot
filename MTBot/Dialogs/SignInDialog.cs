using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MTBot.Models;
using MTBot.Services;
using System.Threading;
using System.Threading.Tasks;

namespace MTBot.Dialogs
{
    public class SignInDialog : CancelAndHelpDialog
    {
        private IMTDataAPIService dataService;

        public SignInDialog(IMTDataAPIService dataService)
            : base(nameof(SignInDialog))
        {
            this.dataService = dataService;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DomainStepAsync,
                UserNameStepAsync,
                PasswordStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> DomainStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                nameof(TextPrompt), 
                new PromptOptions 
                { 
                    Prompt = MessageFactory.Text("MT のドメイン名は？")
                }, 
                cancellationToken);
        }

        private async Task<DialogTurnResult> UserNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            userProfile.Domain = (string)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("ユーザー名は？")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PasswordStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            userProfile.Username = (string)stepContext.Result;

            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("パスワードは？")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userProfile = (UserProfile)stepContext.Options;
            userProfile.Password = (string)stepContext.Result;
            dataService.Initialize(userProfile);
            await dataService.AuthenticateAsync();
            return await stepContext.BeginDialogAsync(nameof(SelectSiteDialog), userProfile, cancellationToken);
        }

    }
}
