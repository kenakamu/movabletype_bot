using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MTBot.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MTBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger Logger;
        private UserState userState;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(SignInDialog signInDialog, SelectSiteDialog selectSiteDialog, NewEntryDialog newEntryDialog, UploadImageDialog uploadImageDialog, ILogger<MainDialog> logger, UserState userState )
            : base(nameof(MainDialog))
        {
            Logger = logger;
            this.userState = userState;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(signInDialog);
            AddDialog(selectSiteDialog);
            AddDialog(newEntryDialog);
            AddDialog(uploadImageDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateAccessors = this.userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(stepContext.Context, () => new UserProfile());

            if (userProfile.Domain == null)
            {
                return await stepContext.BeginDialogAsync(nameof(SignInDialog), new UserProfile(), cancellationToken);
            }

            if (userProfile.Entry == null)
            {
                return await stepContext.BeginDialogAsync(nameof(SelectSiteDialog), userProfile, cancellationToken);
            }

            if (stepContext.Context.Activity?.Attachments?.Count > 0)
            {
                return await stepContext.BeginDialogAsync(nameof(UploadImageDialog), userProfile, cancellationToken);
            }

            else
            {
                switch (stepContext.Context.Activity.Text)
                {
                    case "対象記事変更":
                        return await stepContext.BeginDialogAsync(nameof(SelectSiteDialog), userProfile, cancellationToken);
                    case "新しい記事作成":
                        return await stepContext.BeginDialogAsync(nameof(NewEntryDialog), userProfile, cancellationToken);
                    default:
                        // Catch all for unhandled intents
                        var didntUnderstandMessageText = $"リッチメニューより選択してください。";
                        var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                        await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                        break;
                }
            }
            
            return await stepContext.NextAsync(null, cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is UserProfile result)
            {
                var userStateAccessors = this.userState.CreateProperty<UserProfile>(nameof(UserProfile));
                await userStateAccessors.SetAsync(stepContext.Context, result);
            }
            return await stepContext.EndDialogAsync();
        }
    }
}
