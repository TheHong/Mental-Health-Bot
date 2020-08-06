// Adapted from https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/13.core-bot/CognitiveModels/FlightBooking.cs

using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace MyEchoBot
{
    public partial class UserMsg : IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent
        {
            Conversation,
            Resource,
            Urgent,
            None,
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            [JsonProperty("Keyword")]
            public string[] Keyword;
        }
        public _Entities Entities;

        // [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<UserMsg>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
