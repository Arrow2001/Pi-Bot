using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace PiBot
{
    class Program
    {
        static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();
        public static DiscordSocketClient _client;
        private IServiceProvider services;

        public async Task StartAsync()
        {
            if (Config.bot.DiscordBotToken == "" || Config.bot.DiscordBotToken == null) return;
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 100,
                GatewayIntents = GatewayIntents.Guilds
               | GatewayIntents.GuildMembers
               | GatewayIntents.GuildMessages
               | GatewayIntents.MessageContent
               | GatewayIntents.DirectMessages
            });

            _client.Log += Log;
            await _client.LoginAsync(TokenType.Bot, Config.bot.DiscordBotToken);
            await _client.SetGameAsync("to the Pi", null, ActivityType.Listening);
            await _client.StartAsync();
            Console.WriteLine("Bot Started");
            var _handler = new Handlers.EventHandler(services);
            await _handler.InitialiseAsync(_client);
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            return Task.CompletedTask;
        }
    }
}