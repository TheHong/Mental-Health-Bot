// Partially adapted from https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/adaptive-dialog/08.todo-bot-luis-qnamaker/Dialogs/RootDialog/RootDialog.cs

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.AI.TextAnalytics;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;

using WordVectors;

namespace MHBot
{
    public class RootDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        private Templates _templates;
        private static TextAnalyticsClient _textAnalyticsClient;
        private static InputProcessing _inputProcessor;

        public RootDialog(IConfiguration configuration)
           : base(nameof(RootDialog))
        {
            Configuration = configuration;
            _templates = Templates.ParseFile(Path.Combine(".", "Dialogs", "RootDialog.lg"));
            _textAnalyticsClient = CognitiveServices.GetTextAnalyticsClient(configuration);
            _inputProcessor = new InputProcessing();
            _inputProcessor.LoadResources();

            // Create instance of adaptive dialog. 
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                Recognizer = CognitiveServices.GetCrossTrainedRecognizer(configuration),
                Generator = new TemplateEngineLanguageGenerator(_templates),
                Triggers = new List<OnCondition>()
                {
                    // STARTING ===============================================
                    new OnConversationUpdateActivity()
                    {
                        Actions = TriggerActions.WelcomeUserActions()
                    },

                    // QnA RESPONSE ===========================================
                    new OnQnAMatch()
                    {
                        Actions = TriggerActions.OnQnAMatchActions()
                    },

                    // LANGUAGE DETECTION DEMO ================================
                    // Language change is just for demo purposes. 
                    // In future, can use translate to translate foreign languages to English before using LUIS
                    // Of course, the more nuanced approach would be to have a knowledge base for each language
                    new OnDialogEvent(){
                        Event = "Language Change",
                        Condition = "conversation.currLanguage != conversation.prevLanguage",
                        Actions = TriggerActions.OnLanguageChangeActions(ResetLanguage)
                    },

                    // LUIS - - - - - - -
                    // The intents here are based on intents defined in LUIS-KnowledgeBase.LU file
                    // (however, the .lu file is just for reference and the main functionality come from the LUIS resource)

                    // INTENT:URGENT ==========================================
                    new OnIntent()
                    {
                        Intent = "Urgent",
                        Condition = "#Urgent.Score >= 0.9",
                        Actions = TriggerActions.OnUrgentIntentActions()
                    },

                    // INTENT:HANDOFF =========================================
                    new OnIntent()
                    {
                        Intent = "Handoff",
                        Condition = "#Handoff.Score >= 0.6",
                        Actions = TriggerActions.OnHandoffIntentActions()
                    },

                    // INTENT:RESOURCE ========================================
                    new OnIntent()
                    {
                        Intent = "Resource",
                        Condition = "#Resource.Score >= 0.6",
                        Actions = TriggerActions.OnResourcesIntentActions(DisplayResources, UpdateKeywords)
                    },

                    // QnA vs. LUIS ===========================================
                    new OnChooseIntent()
                    {
                        Actions = TriggerActions.QnAvsLUISActions(DetectLanguage)
                    },

                    // INTENT:NONE ============================================
                    new OnUnknownIntent()
                    {
                        Actions = TriggerActions.OnUnknownIntentActions(DetectLanguage)
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
        }

        private static async Task<DialogTurnResult> UpdateKeywords(DialogContext dc, System.Object options)
        {
            var currKeywords = dc.State.GetValue("conversation.keywords", () => new string[0]);
            var incomingKeywords = dc.State.GetValue("turn.recognized.entities.Keyword", () => new string[0]);
            var keywords = currKeywords.Concat(incomingKeywords).Distinct().ToArray();
            dc.State.SetValue("conversation.keywords", keywords);
            return await dc.EndDialogAsync();
        }

        private static async Task<DialogTurnResult> ResetLanguage(DialogContext dc, System.Object options)
        {
            dc.State.SetValue("conversation.currLanguage", "English");
            return await dc.EndDialogAsync();
        }

        private static async Task<DialogTurnResult> DetectLanguage(DialogContext dc, System.Object options)
        {
            string incomingText = dc.State.GetValue("turn.activity.text", () => "");
            DetectedLanguage language = _textAnalyticsClient.DetectLanguage(incomingText);
            if (language.ConfidenceScore > 0.8)
            {
                dc.State.SetValue("conversation.currLanguage", language.Name);
            }
            Console.WriteLine($"Detected language {language.Name} with confidence score {language.ConfidenceScore}.");
            return await dc.EndDialogAsync();
        }

        private static async Task<DialogTurnResult> DisplayResources(DialogContext dc, System.Object options)
        {
            /* 
            All hail https://stackoverflow.com/questions/62861153/how-to-convert-from-xml-to-json-within-adaptive-dialog-httprequest/62924035#62924035
            */
            // FIRST: PROCESS KEYWORDS
            Console.WriteLine("Processing");
            var keywords = dc.State.GetValue("conversation.keywords", () => new string[0]);

            // Displaying results ----------------------------------------------------------
            Console.WriteLine($"> Keywords are: [{string.Join(", ", keywords)}]");
            List<List<WordProb>> tags = _inputProcessor.GetTags(keywords);
            if (tags.Any())
            {
                Console.WriteLine("> Tags are as follows:");
                foreach (List<WordProb> wpL in tags)
                {
                    Console.WriteLine(" ");
                    foreach (WordProb wp in wpL)
                    {
                        Console.WriteLine($"->{wp.ToStr()}");
                    }
                }
                List<Resource> resources = _inputProcessor.GetResources(tags);
                Console.WriteLine("Got resources");
                // -------------------------------------------------------------------------

                // THEN: DISPLAY RESULTS
                var attachments = new List<Attachment>();
                foreach (Resource resource in resources)
                {
                    attachments.Add(resource.ToCard().ToAttachment());
                }

                // Reply to the activity we received with an activity.
                var reply = MessageFactory.Attachment(attachments);
                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                await dc.Context.SendActivityAsync(reply);

                // Reset the embedding to be used for next resource intent
                _inputProcessor.resetCurrEmbedding();

                dc.State.SetValue("conversation.resourcesFound", true);
            }
            else
            {
                dc.State.SetValue("conversation.resourcesFound", false);
            }
            return await dc.EndDialogAsync();
        }
    }
}
