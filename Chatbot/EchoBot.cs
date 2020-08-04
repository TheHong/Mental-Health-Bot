// Adapted from EchoBot .NET Template version v4.9.1
/*
Despite its name, it's not actually an echo bot. It is only based on the echo bot structure.
*/

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
            await SendIntroCardAsync(turnContext, cancellationToken);
            // var welcomeText = "Hello and welcome! My name is MHBOT. THIS IS AWESOME";
            // foreach (var member in membersAdded)
            // {
            //     if (member.Id != turnContext.Activity.Recipient.Id)
            //     {
            //         await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
            //     }
            // }
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard();
            card.Title = "Hi, my name is Felix!";
            card.Text = "I am a chatbot designed to talk to you about mental " +
                "health resources available at the University of Toronto. As a " +
                "chatbot, I am not human. As a result, please note that my conversation " +
                "skills might be limited, despite my responses being human-like. " +
                "At any time you have the option to speak with a human operator. " +
                "You may now type something are click on one of the following options to get started.";
            card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, "Tell me about yourself, Felix.", null, "Get an overview", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                new CardAction(ActionTypes.OpenUrl, "Please redirect me to an operator.", null, "Ask a question", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
                new CardAction(ActionTypes.OpenUrl, "How are you today, Felix?", null, "Learn how to deploy", "Learn how to deploy", "https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-howto-deploy-azure?view=azure-bot-service-4.0"),
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }
    }
}
