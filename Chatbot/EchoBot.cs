// Adapted from EchoBot .NET Template version v4.9.1

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MyEchoBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly Recognizer _luisRecognizer = new Recognizer();

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            string info;

            UserMsg userMsg = new UserMsg();

            // Call LUIS and gather insights from input. (Note the TurnContext has the response to the prompt.)
            if (_luisRecognizer.IsConfigured)
            {
                var luisResult = await _luisRecognizer.RecognizeAsync<UserMsg>(turnContext, cancellationToken);
                userMsg.Convert(luisResult);
                var topIntent = userMsg.TopIntent();
                string[] entities;

                if (userMsg.Entities.Keyword != null)
                {
                    entities = userMsg.Entities.Keyword;
                    info = $"top intent: {userMsg.TopIntent()}, entities: [{string.Join(", ", entities)}]";
                }
                else
                {
                    info = $"top intent: {userMsg.TopIntent()}, entities: N/A";
                }
            }
            else
            {
                info = "LUIS not found";
            }
            var replyText = $"You just typed: {text}. Info: {info}";

            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome! My name is MHBOT. THIS IS AWESOME";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        // private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        // {
        //     var card = new HeroCard();
        //     card.Title = "Welcome to Bot Framework!";
        //     card.Text = @"Welcome to Welcome Users bot sample! This Introduction card
        //             is a great way to introduce your Bot to the user and suggest
        //             some things to get them started. We use this opportunity to
        //             recommend a few next steps for learning more creating and deploying bots.";
        //     card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
        //     card.Buttons = new List<CardAction>()
        //     {
        //         new CardAction(ActionTypes.OpenUrl, "Get an overview", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
        //         new CardAction(ActionTypes.OpenUrl, "Ask a question", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
        //         new CardAction(ActionTypes.OpenUrl, "Learn how to deploy", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
        //     };

        //     var response = MessageFactory.Attachment(card.ToAttachment());
        //     await turnContext.SendActivityAsync(response, cancellationToken);
        // }
    }
}
