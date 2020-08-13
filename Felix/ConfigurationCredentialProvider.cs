// Adapted from https://github.com/microsoft/BotBuilder-Samples/blob/master/samples/csharp_dotnetcore/adaptive-dialog/03.core-bot/ConfigurationCredentialProvider.cs

using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;

namespace MHBot
{
    public class ConfigurationCredentialProvider : SimpleCredentialProvider
    {
        public ConfigurationCredentialProvider(IConfiguration configuration)
            : base(configuration["MicrosoftAppId"], configuration["MicrosoftAppPassword"])
        {
        }
    }
}
