// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Azure.AI.TextAnalytics;
using Azure;


using wordVecDistance;

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
            _textAnalyticsClient = GetTextAnalyticsClient(configuration);
            _inputProcessor = new InputProcessing();
            _inputProcessor.load_resources("Data/UofT Mental Health Resources.txt");


            // Create instance of adaptive dialog. 
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // There are no steps associated with this dialog.
                // This dialog will react to user input using its own Recognizer's output and Rules.

                // Add a recognizer to the adaptive dialog.
                Recognizer = GetCrossTrainedRecognizer(configuration),

                // Add rules to respond to different events of interest
                Generator = new TemplateEngineLanguageGenerator(_templates),
                Triggers = new List<OnCondition>()
                {
                    // Add a rule to welcome user
                    new OnConversationUpdateActivity()
                    {
                        Actions = WelcomeUserSteps()
                    },

                    // With QnA Maker set as a recognizer on a dialog, you can use the OnQnAMatch trigger to render the answer.
                    new OnQnAMatch()
                    {
                        Actions = new List<Dialog>()
                        {
                            new SendActivity()
                            {
                                Activity = new ActivityTemplate("${@answer}"),
                            }
                        }
                    },

                    // The intents here are based on intents defined in RootDialog.LU file
                    // (however, the .lu file is just for reference and the main functionality come from the LUIS resource)
                    //TODO: Add .lu file for completion
                    
                    new OnDialogEvent(){
                        // Language change is just for demo purposes. 
                        // In future, can use translate to translate foreign languages to English before using LUIS
                        // Of course, the more nuanced approach would be to have a knowledge base for each language
                        Event = "Language Change",
                        Condition = "conversation.currLanguage != conversation.prevLanguage",
                        Actions = new List<Dialog> ()
                        {
                            new SendActivity("${ShowLanguageSample()}"),
                            new CodeAction(ResetLanguage),
                            new EndDialog(),
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "Urgent",
                        Condition = "#Urgent.Score >= 0.6",
                        Actions = new List<Dialog> ()
                        {
                            new SendActivity("${UrgentIntent()}")
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "Conversation",
                        Condition = "#Conversation.Score >= 0.6",
                        Actions = new List<Dialog> ()
                        {
                            new CodeAction(DetectLanguage),
                            new EmitEvent("Language Change"),
                            new SendActivity("${ConversationIntent()}") // TODO: Replace with QnA
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>() {
                            new CodeAction(DetectLanguage),
                            new EmitEvent("Language Change"),
                            new SendActivity("${UnknownIntent()}"),
                        }
                    },
                    new OnIntent()
                    {
                        Intent = "Resource",
                        Condition = "#Resource.Score >= 0.6",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("${ResourceIntent()}"),

                            // Save any entities returned by LUIS.
                            new CodeAction(UpdateKeywords), // Gets list of entities
                            // new SetProperty() // <- Does not store the list of entities
                            // {
                            //     Property = "conversation.keywords",
                            //     Value = "=@Keyword" // i.e. turn.recognized.entities.Keyword
                            // },

                            // Converse with user
                            new ConfirmInput()
                            {
                                Prompt = new ActivityTemplate("${MoreInfoPrompt()}"),
                                Property = "turn.moreInfoToGive",
                                AllowInterruptions = "false"
                            },
                            new IfCondition()
                            {
                                Condition = "turn.moreInfoToGive == true",
                                Actions = new List<Dialog>()
                                {
                                        new TextInput()
                                        {
                                            Prompt = new ActivityTemplate("${AskForMoreInfo()}"),
                                            Property = "conversation.moreInfo",
                                            AllowInterruptions = "false"
                                        },
                                        new CodeAction(UpdateKeywords),
                                        new SendActivity("${EndMoreInfo()}"),
                                },
                            },
                            new SendActivity("${EndInfo()}"),
                            new CodeAction(ProcessKeywords),
                            new SendActivity("${DisplayResources()}"),
                            new EndDialog(),
                        }
                    }
            }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(AdaptiveDialog);
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
        private static async Task<DialogTurnResult> ProcessKeywords(DialogContext dc, System.Object options)
        {
            // All hail https://stackoverflow.com/questions/62861153/how-to-convert-from-xml-to-json-within-adaptive-dialog-httprequest/62924035#62924035
            Console.WriteLine("\n\nProcessing\n\n");
            var keywords = dc.State.GetValue("conversation.keywords", () => new string[0]);
            string resourcesAsStr;

            // TODO: INSERT KEYWORDS->TAGS->RESOURCES CODE HERE
            Console.WriteLine(string.Join(", ", keywords));
            List<List<WordProb>> tags = _inputProcessor.GetTags(keywords);
            Console.WriteLine("Got {0}", tags);
            foreach (List<WordProb> wpL in tags)
            {
                foreach (WordProb wp in wpL)
                {
                    Console.WriteLine(wp.ToStr());
                }
            }
            List<Resource> resources = _inputProcessor.GetResources(tags);
            Console.WriteLine("Got resources");
            List<string> resourceStrings = new List<string>();
            foreach (Resource r in resources)
            {
                resourceStrings.Add(r.ToStr());
            }
            resourcesAsStr = string.Join("\n", resourceStrings);
            ///////////////////////////////////

            // TODO: Delete this, as this will be temprorary
            // resourcesAsStr = string.Join(", ", keywords);
            /////////////////////////////////////////

            dc.State.SetValue("conversation.result", resourcesAsStr);
            _inputProcessor.resetCurrEmb();
            return await dc.EndDialogAsync();
        }

        private static async Task<DialogTurnResult> UpdateKeywords(DialogContext dc, System.Object options)
        {
            Console.WriteLine("\n\nUpdating Keywords\n\n");
            var currKeywords = dc.State.GetValue("conversation.keywords", () => new string[0]);
            var incomingKeywords = dc.State.GetValue("turn.recognized.entities.Keyword", () => new string[0]);
            var keywords = currKeywords.Concat(incomingKeywords).Distinct().ToArray();
            dc.State.SetValue("conversation.keywords", keywords);
            return await dc.EndDialogAsync();
        }
        private static List<Dialog> WelcomeUserSteps()
        {
            return new List<Dialog>()
            {
                // Iterate through membersAdded list and greet user added to the conversation.
                new Foreach()
                {
                    ItemsProperty = "turn.activity.membersAdded",
                    Actions = new List<Dialog>()
                    {
                        // Note: Some channels send two conversation update events - one for the Bot added to the conversation and another for user.
                        // Filter cases where the bot itself is the recipient of the message. 
                        new IfCondition()
                        {
                            Condition = "$foreach.value.name != turn.activity.recipient.name",
                            Actions = new List<Dialog>()
                            {
                                new SendActivity("${WelcomeCard()}")
                            }
                        },
                        new SetProperties()
                        {
                            Assignments = new List<PropertyAssignment>()
                            {
                                new PropertyAssignment()
                                {
                                    Property = "conversation.prevLanguage",
                                    Value = "English"
                                },
                                new PropertyAssignment()
                                {
                                    Property = "conversation.currLanguage",
                                    Value = "English"
                                }
                            }
                        },
                    }
                }
            };
        }

        private static TextAnalyticsClient GetTextAnalyticsClient(IConfiguration configuration)
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
        private static Recognizer GetCrossTrainedRecognizer(IConfiguration configuration)
        {
            return new CrossTrainedRecognizerSet()
            {
                Recognizers = new List<Recognizer>()
                {
                    GetLuisRecognizer(configuration),
                    GetQnARecognizer(configuration)
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

                // Id needs to be LUIS_<dialogName> for cross-trained recognizer to work.
                Id = $"LUIS_{nameof(RootDialog)}"
            };
        }
        private static Recognizer GetQnARecognizer(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["qna:KnowledgeBaseId"]) || string.IsNullOrEmpty(configuration["qna:HostName"]) || string.IsNullOrEmpty(configuration["qna:EndpointKey"]))
            {
                throw new Exception("NOTE: QnA Maker is not configured for RootDialog. Check the appsettings.json file.");
            }

            var recognizer = new QnAMakerRecognizer()
            {
                HostName = configuration["qna:HostName"],
                EndpointKey = configuration["qna:EndpointKey"],
                KnowledgeBaseId = configuration["qna:KnowledgeBaseId"],

                // property path that holds qna context
                Context = "dialog.qnaContext",

                // Property path where previous qna id is set. This is required to have multi-turn QnA working.
                QnAId = "turn.qnaIdFromPrompt",

                // Disable teletry logging
                LogPersonalInformation = false,

                // Disable to automatically including dialog name as meta data filter on calls to QnA Maker.
                IncludeDialogNameInMetadata = false,

                // Id needs to be QnA_<dialogName> for cross-trained recognizer to work.
                Id = $"QnA_{nameof(RootDialog)}"
            };

            return recognizer;
        }

    }
}
