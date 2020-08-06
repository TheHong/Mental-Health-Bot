// Adapted from https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/13.core-bot/FlightBookingRecognizer.cs

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;

namespace MyEchoBot
{
    public class Recognizer : IRecognizer
    /*
    A next step would be to take advantage of appsettings.json.
    Currently using UserInfo.cs (gitignored) to provide necessary info for the 
    luis application. Those that would use the appsettings.json and IConfiguration
    are commented throughout.
    */
    {
        private readonly LuisRecognizer _recognizer;

        public Recognizer()//IConfiguration configuration)
        {
            // When using appsettings.json, will use the following line
            // var luisIsConfigured = !string.IsNullOrEmpty(configuration["LuisAppId"]) && !string.IsNullOrEmpty(configuration["LuisAPIKey"]) && !string.IsNullOrEmpty(configuration["LuisAPIHostName"]);
            var luisIsConfigured = true;
            if (luisIsConfigured)
            {
                // When using appsettings.json, will use the following lines
                // var luisApplication = new LuisApplication(
                //     configuration["LuisAppId"],
                //     configuration["LuisAPIKey"],
                //     "https://" + configuration["LuisAPIHostName"]);
                var luisApplication = new LuisApplication(UserInfo.i, UserInfo.k, UserInfo.h);

                // Set the recognizer options depending on which endpoint version you want to use.
                // More details can be found in https://docs.microsoft.com/en-gb/azure/cognitive-services/luis/luis-migration-api-v3
                var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication)
                {
                    PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions
                    {
                        IncludeInstanceData = true,
                    }
                };

                _recognizer = new LuisRecognizer(recognizerOptions);
            }
        }

        // Returns true if luis is configured in the appsettings.json and initialized.
        public virtual bool IsConfigured => _recognizer != null;

        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await _recognizer.RecognizeAsync(turnContext, cancellationToken);

        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
            => await _recognizer.RecognizeAsync<T>(turnContext, cancellationToken);
    }
}
