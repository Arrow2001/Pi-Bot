using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PiBot.Handlers
{
    class EventHandler
    {
        DiscordSocketClient _client;
        CommandService _service;
        readonly IServiceProvider serviceProvider;
        private const int buttonPin = 17;
        public EventHandler(IServiceProvider services) => serviceProvider = services;

        public async Task InitialiseAsync(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            _client.MessageReceived += HandleCommandAsync;
            _service.Log += Log;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

       
        private async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage msg = s as SocketUserMessage;
            if (msg == null || msg.Author.IsBot) return;
            var context = new SocketCommandContext(_client, msg);

            int argPos = 0;
            if (msg.HasStringPrefix("??", ref argPos))
                await _service.ExecuteAsync(context, argPos, serviceProvider, MultiMatchHandling.Exception);

            string m = msg.Content.ToLower();

            if (msg.Channel is SocketDMChannel)
            {
                Console.WriteLine($"New DM from {msg.Author.Username.ToString()}: {msg.Content.ToString()}");
                var c = context.Client.GetGuild(632771528022425612).GetChannel(1272150974961553479) as SocketTextChannel;
            }
        }
    }
}