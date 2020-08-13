// Partially adapted from https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/adaptive-dialog/08.todo-bot-luis-qnamaker/Dialogs/RootDialog/RootDialog.cs
// Contains the methods used to set up usage of Azure's Cognitive Services

using System;
using System.Collections.Generic;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA.Recognizers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;

namespace MHBot
{
    public class CognitiveServices
    {
        public static TextAnalyticsClient GetTextAnalyticsClient(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["textanalytics:Endpoint"]) || string.IsNullOrEmpty(configuration["textanalytics:APIKey"]))
            {
                throw new Exception("NOTE: TextAnalytics is not configured. Check the appsettings.json file.");
            }
            return new TextAnalyticsClient(
                new Uri(configuration["textanalytics:Endpoint"]),
                new AzureKeyCredential(configuration["textanalytics:APIKey"])
            );
        }
        public static Recognizer GetCrossTrainedRecognizer(IConfiguration configuration)
        {
            return new CrossTrainedRecognizerSet()
            {
                Recognizers = new List<Recognizer>()
                {
                    GetQnARecognizer(configuration),
                    GetLuisRecognizer(configuration)
                }
            };
        }
        private static Recognizer GetLuisRecognizer(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["luis:AppId"]) || string.IsNullOrEmpty(configuration["luis:APIKey"]) || string.IsNullOrEmpty(configuration["luis:APIHostName"]))
            {
                throw new Exception("NOTE: LUIS is not configured. Check the appsettings.json file.");
            }
            return new LuisAdaptiveRecognizer()
            {
                ApplicationId = configuration["luis:AppId"],
                EndpointKey = configuration["luis:APIKey"],
                Endpoint = configuration["luis:APIHostName"],
                Id = $"LUIS_{nameof(RootDialog)}" // Needs to be LUIS_<dialogName> for cross-trained recognizer to work.
            };
        }
        private static Recognizer GetQnARecognizer(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["qna:KnowledgeBaseId"]) || string.IsNullOrEmpty(configuration["qna:HostName"]) || string.IsNullOrEmpty(configuration["qna:EndpointKey"]))
            {
                throw new Exception("NOTE: QnA Maker is not configured for RootDialog. Check the appsettings.json file.");
            }
            return new QnAMakerRecognizer()
            {
                HostName = configuration["qna:HostName"],
                EndpointKey = configuration["qna:EndpointKey"],
                KnowledgeBaseId = configuration["qna:KnowledgeBaseId"],
                Context = "dialog.qnaContext", // property path that holds qna context
                QnAId = "turn.qnaIdFromPrompt", // property path where previous qna id is set. This is required to have multi-turn QnA working.
                LogPersonalInformation = false, // disable teletry logging
                IncludeDialogNameInMetadata = false, // disable to automatically including dialog name as meta data filter on calls to QnA Maker. Needs to be disabled.
                Id = $"QnA_{nameof(RootDialog)}" // Needs to be QnA_<dialogName> for cross-trained recognizer to work.
            };
        }
    }
}