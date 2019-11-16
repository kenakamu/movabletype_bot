// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.5.0

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MTBot.Models;

namespace MTBot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private const string ErrorMsgText = "Sorry, it looks like something went wrong.";
        private const string AuthFailMsgText = "�F�؂Ɏ��s���܂����B";

        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, UserState userState, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                if (exception is UnauthorizedAccessException)
                {
                    var userStateAccessors = userState.CreateProperty<UserProfile>(nameof(UserProfile));
                    await userStateAccessors.SetAsync(turnContext, new UserProfile());
                    await userState.SaveChangesAsync(turnContext);
                    var errorMessage = MessageFactory.SuggestedActions(
                        new List<CardAction>() { new CardAction(ActionTypes.ImBack, title: "�T�C���C��", value:"�T�C���C��") },
                        AuthFailMsgText, AuthFailMsgText, InputHints.ExpectingInput) ;

                    await turnContext.SendActivityAsync(errorMessage);

                    if (conversationState != null)
                    {
                        try
                        {
                            // Delete the conversationState for the current conversation to prevent the
                            // bot from getting stuck in a error-loop caused by being in a bad state.
                            // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                            await conversationState.DeleteAsync(turnContext);
                        }
                        catch (Exception e)
                        {
                            logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                        }
                    }

                }
                else
                {
                    // Log any leaked exception from the application.
                    logger.LogError($"Exception caught : {exception.Message}");

                    // Send a catch-all apology to the user.
                    var errorMessage = MessageFactory.Text(ErrorMsgText, ErrorMsgText, InputHints.ExpectingInput);
                    await turnContext.SendActivityAsync(errorMessage);

                    if (conversationState != null)
                    {
                        try
                        {
                            // Delete the conversationState for the current conversation to prevent the
                            // bot from getting stuck in a error-loop caused by being in a bad state.
                            // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                            await conversationState.DeleteAsync(turnContext);
                        }
                        catch (Exception e)
                        {
                            logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                        }
                    }
                }
            };
            MiddlewareSet.Use(new AutoSaveStateMiddleware(userState, conversationState));
        }
    }
}
