using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Bot.Builder.AI.Luis;
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

namespace MHBot
{
    public class RootDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        private Templates _templates;

        public RootDialog(IConfiguration configuration)
           : base(nameof(RootDialog))
        {
            Configuration = configuration;
            _templates = Templates.ParseFile(Path.Combine(".", "Dialogs", "RootDialog.lg"));

            // Create instance of adaptive dialog. 
            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // There are no steps associated with this dialog.
                // This dialog will react to user input using its own Recognizer's output and Rules.

                // Add a recognizer to the adaptive dialog.
                Recognizer = CreateRecognizer(configuration),

                // Add rules to respond to different events of interest
                Generator = new TemplateEngineLanguageGenerator(_templates),
                Triggers = new List<OnCondition>()
                {
                    // Add a rule to welcome user
                    new OnConversationUpdateActivity()
                    {
                        Actions = WelcomeUserSteps()
                    },
                    // The intents here are based on intents defined in RootDialog.LU file
                    // (however, the .lu file is just for reference and the main functionality come from the LUIS resource)
                    //TODO: Add .lu file for completion
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
                            new SendActivity("${ConversationIntent()}") // TODO: Replace with QnA
                        }
                    },
                    new OnUnknownIntent()
                    {
                        Actions = new List<Dialog>() {
                            new SendActivity("${UnknownIntent()}")
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

        private static async Task<DialogTurnResult> ProcessKeywords(DialogContext dc, System.Object options)
        {
            // All hail https://stackoverflow.com/questions/62861153/how-to-convert-from-xml-to-json-within-adaptive-dialog-httprequest/62924035#62924035
            Console.WriteLine("\n\nProcessing\n\n");
            var keywords = dc.State.GetValue("conversation.keywords", () => new string[0]);
            string resourcesAsStr;

            // TODO: INSERT KEYWORDS->TAGS->RESOURCES CODE HERE
            // ...
            ///////////////////////////////////

            // TODO: Delete this, as this will be temprorary
            resourcesAsStr = string.Join(", ", keywords);
            /////////////////////////////////////////

            dc.State.SetValue("conversation.result", resourcesAsStr);
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
                                new SendActivity("Let's go") // To be replaced with welcome card
                            }
                        }
                    }
                }
            };
        }

        private static Recognizer CreateRecognizer(IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration["LuisAppId"]) || string.IsNullOrEmpty(configuration["LuisAPIKey"]) || string.IsNullOrEmpty(configuration["LuisAPIHostName"]))
            {
                throw new Exception("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.");
            }

            return new LuisAdaptiveRecognizer()
            {
                ApplicationId = configuration["LuisAppId"],
                EndpointKey = configuration["LuisAPIKey"],
                Endpoint = configuration["LuisAPIHostName"]
            };
        }
    }
}
