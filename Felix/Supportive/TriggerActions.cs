// Defines the actions done for various adaptive dialog triggers
// Used in Dialogs/RootDialog.cs

using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;

using CodeActionFunc = System.Func<Microsoft.Bot.Builder.Dialogs.DialogContext, System.Object, System.Threading.Tasks.Task<Microsoft.Bot.Builder.Dialogs.DialogTurnResult>>;

namespace MHBot
{
    public class Delay
    { // This class is used to add a delay between bot output to make it act more human
        private static int _shortDelayLength = 2000; // Short delay has no typing indicator
        private static int _longDelayLength = 2000; // Long delay has typing indicator
        public static async Task<DialogTurnResult> Long(DialogContext dc, System.Object options)
        {
            // Build a typing indicator
            Activity reply = dc.Context.Activity.CreateReply();
            reply.Type = ActivityTypes.Typing;
            reply.Text = null;

            // Show the typing indicator for certain amount of time
            await dc.Context.SendActivityAsync(reply);
            await Task.Delay(_longDelayLength).ContinueWith(t => { });
            return await dc.EndDialogAsync();
        }
        public static async Task<DialogTurnResult> Short(DialogContext dc, System.Object options)
        {
            await Task.Delay(_shortDelayLength).ContinueWith(t => { });
            return await dc.EndDialogAsync();
        }
    }

    public class TriggerActions
    {
        public static List<Dialog> WelcomeUserActions() => new List<Dialog>()
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
                            },
                            new PropertyAssignment()
                            {
                                Property = "conversation.moreInfoToGive",
                                Value = "=0"
                            },
                        }
                    },
                }
            }
        };

        public static List<Dialog> QnAvsLUISActions(CodeActionFunc LanguageDetectFunc) => new List<Dialog>()
        {
            // Do language detection feature
            new CodeAction(LanguageDetectFunc),
            new EmitEvent("Language Change"),

            // Get the confidence scores from both LUIS and QnA
            new SetProperties()
            {
                Assignments = new List<PropertyAssignment>()
                {
                    new PropertyAssignment()
                    {
                        Property = "dialog.luisResult",
                        Value = $"=jPath(turn.recognized, \"$.candidates[?(@.id == 'LUIS_{nameof(RootDialog)}')]\")"
                    },
                    new PropertyAssignment()
                    {
                        Property = "dialog.qnaResult",
                        Value = $"=jPath(turn.recognized, \"$.candidates[?(@.id == 'QnA_{nameof(RootDialog)}')]\")"
                    },
                }
            },
            // new SendActivity("${ShowConfidence()}"), // For debug

            // Rule 1: If QnA is fairly confident and is more confident than LUIS, then QnA it is
            new IfCondition()
            {
                Condition = "dialog.qnaResult.score > dialog.luisResult.score && dialog.qnaResult.score > 0.75",
                Actions = new List<Dialog>()
                {
                    // By Emitting a recognized intent event with the recognition result from LUIS, adaptive dialog
                    // will evaluate all triggers with that recognition result.
                    new EmitEvent()
                    {
                        EventName = AdaptiveEvents.RecognizedIntent,
                        EventValue = "=dialog.qnaResult.result"
                    },
                    new BreakLoop()
                }
            },
            // Rule 2: If the other way around
            new IfCondition()
            {
                Condition = "dialog.luisResult.score > 0.6",
                Actions = new List<Dialog>()
                {
                    new EmitEvent()
                    {
                        EventName = AdaptiveEvents.RecognizedIntent,
                        EventValue = "=dialog.luisResult.result"
                    },
                    new BreakLoop()
                }
            },
            // Rule 3: If none works (acts as OnUnknownIntent)
            new SendActivity("${UnknownIntent()}")
        };

        public static List<Dialog> OnLanguageChangeActions(CodeActionFunc LanguageResetFunc) => new List<Dialog>()
        {
            new SendActivity("${ShowLanguageSample()}"),
            new CodeAction(LanguageResetFunc),
            new EndDialog(),
        };

        public static List<Dialog> OnQnAMatchActions() => new List<Dialog>()
        {
            new SendActivity()
            {
                Activity = new ActivityTemplate("${@answer}"),
            }
        };

        public static List<Dialog> OnHandoffIntentActions() => new List<Dialog>()
        {
            new SendActivity("${HandoffIntent()}")
        };

        public static List<Dialog> OnUrgentIntentActions() => new List<Dialog>()
        {
            new SendActivity("${UrgentIntent()}"),
            new ConfirmInput()
            {
                Prompt = new ActivityTemplate("${HandoffPrompt()}"),
                Property = "turn.handoff",
                AllowInterruptions = "false"
            },
            new IfCondition()
            {
                Condition = "turn.handoff == true",
                Actions = new List<Dialog>()
                {
                    new SendActivity("${HandoffIntent()}")
                },
                ElseActions = new List<Dialog>()
                {
                    new SendActivity("${HandoffIntentCancel()}")
                },
            },
        };

        public static List<Dialog> OnResourcesIntentActions(CodeActionFunc DisplayResourcesFunc, CodeActionFunc UpdateKeywordsFunc) => new List<Dialog>()
        {
            new IfCondition(){
                Condition = "conversation.moreInfoToGive == 0",
                Actions = new List<Dialog>{
                    new SendActivity("${ResourceIntent()}"),
                    new CodeAction(Delay.Short),

                    // Save any entities returned by LUIS.
                    new CodeAction(UpdateKeywordsFunc), // Gets list of entities
                    // new SetProperty() // <- Does not store the list of entities
                    // {
                    //     Property = "conversation.keywords",
                    //     Value = "=@Keyword" // i.e. turn.recognized.entities.Keyword
                    // },

                    // Converse with user
                    new ConfirmInput()
                    {
                        Prompt = new ActivityTemplate("${MoreInfoPrompt()}"),
                        Property = "conversation.moreInfoToGive",
                        AllowInterruptions = "false"
                    },
                    new IfCondition()
                    {
                        Condition = "conversation.moreInfoToGive == true",
                        Actions = new List<Dialog>()
                        {
                                new TextInput()
                                {
                                    Prompt = new ActivityTemplate("${AskForMoreInfo()}"),
                                    Property = "conversation.moreInfo",
                                    AllowInterruptions = "true" // Needs to be true so user input gets analyzed by LUIS again
                                },

                        },
                    },
                },
                ElseActions = new List<Dialog>{
                    new SetProperty(){ // Reset
                        Property = "conversation.moreInfoToGive",
                        Value = "=0"
                    },
                    new CodeAction(UpdateKeywordsFunc),
                    new SendActivity("${EndMoreInfo()}"),
                }
            },
            new SendActivity("${EndInfo()}"),
            new CodeAction(Delay.Long),
            new SendActivity("${DisplayResourcesPrompt()}"),
            new CodeAction(Delay.Short),
            new CodeAction(DisplayResourcesFunc),
            new IfCondition(){
                Condition = "conversation.resourcesFound == true",
                Actions = new List<Dialog>{
                    new CodeAction(Delay.Short),
                    new SendActivity("${DisplayResourcesFollowUp()}"),
                },
                ElseActions = new List<Dialog>{
                    new SendActivity("${NoResourcesFound()}"),
                    new ConfirmInput()
                    {
                        Prompt = new ActivityTemplate("${HandoffPrompt()}"),
                        Property = "turn.handoff",
                        AllowInterruptions = "false"
                    },
                    new IfCondition()
                    {
                        Condition = "turn.handoff == true",
                        Actions = new List<Dialog>()
                        {
                            new SendActivity("${HandoffIntent()}")
                        },
                        ElseActions = new List<Dialog>()
                        {
                            new SendActivity("${HandoffIntentCancel()}")
                        },
                    },
                }
            },
            new SetProperties()
            {
                Assignments = new List<PropertyAssignment>()
                {
                    new PropertyAssignment()
                    {
                        Property = "conversation.keywords",
                        Value = "=[]"
                    },
                    new PropertyAssignment()
                    {
                        Property = "conversation.currLanguage",
                        Value = "English"
                    },
                    new PropertyAssignment()
                    {
                        Property = "conversation.moreInfoToGive",
                        Value = "=0"
                    },
                }
            },
            new EndDialog()
        };

        public static List<Dialog> OnUnknownIntentActions(CodeActionFunc LanguageDetectFunc) => new List<Dialog>()
        {
            new CodeAction(LanguageDetectFunc),
            new EmitEvent("Language Change"),
            new SendActivity("${UnknownIntent()}")
        };
    }
}