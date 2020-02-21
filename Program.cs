using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ElevatorBot
{
    class Program
    {
        public DiscordClient bot;
        public CommandsNextExtension Commands { get; set; }
        public VoiceNextExtension Voice { get; set; }

        public static Program instance = new Program();
        static void Main(string[] args)
        {
            instance.startBotAsync().GetAwaiter().GetResult();
        }

        private Task botReady(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "ElevatorBot", "Bot is ready", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task botGuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "ElevatorBot", $"Guild Available: {e.Guild.Name}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task botErrorHandler(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "ElevatorBot", $"Exception occurred: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task commandsExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "ElevatorBot", $"{e.Context.User.Username} issued the command '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task commandsError(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "ElevatorBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                DiscordEmoji emoji = DiscordEmoji.FromName(e.Context.Client, ":x:");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "",
                    Description = $"{emoji} You do not have permission to use this command.",
                    Color = new DiscordColor(0xff0000)
                };

                await e.Context.RespondAsync("", embed: embed);
            }
        }

        public async Task startBotAsync()
        {
            //load json
            JConf cjason;
            JToken tjason;
            using (StreamReader r = new StreamReader("config.json"))
            {
                var json = r.ReadToEnd();
                cjason = JsonConvert.DeserializeObject<JConf>(json);
            }
            using (StreamReader r = new StreamReader("token.json"))
            {
                var json = r.ReadToEnd();
                tjason = JsonConvert.DeserializeObject<JToken>(json);
            }

            var config = new DiscordConfiguration
            {
                Token = tjason.token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true
            };

            this.bot = new DiscordClient(config);

            this.bot.Ready += this.botReady;
            this.bot.GuildAvailable += this.botGuildAvailable;
            this.bot.ClientErrored += this.botErrorHandler;

            var commandConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] { cjason.prefix },

                EnableMentionPrefix = true
            };

            this.Commands = this.bot.UseCommandsNext(commandConfig);

            this.Commands.CommandExecuted += this.commandsExecuted;
            this.Commands.CommandErrored += this.commandsError;

            this.Commands.RegisterCommands<ElevatorBotCommands>();
            this.Commands.RegisterCommands<ElevatorBotVoice>();
            
            var voiceConfig = new VoiceNextConfiguration
            {
                AudioFormat = AudioFormat.Default
            };

            this.Voice = this.bot.UseVoiceNext(voiceConfig);

            await this.bot.ConnectAsync();

            //prevent bot from stopping
            await Task.Delay(-1);
        }
    }
    class JConf
    {
        // class and variables for json configs
        public string prefix = "";
    }
    class JToken
    {
        public string token = "";
    }
}
