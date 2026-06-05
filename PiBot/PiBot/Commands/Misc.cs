using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PiBot.Commands
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task send() => await Context.Channel.SendMessageAsync("Pong!");

        // Check the temperature of the RPi
        [Command("temp")]
        public async Task CheckPiTemp()
        {
            decimal temp = decimal.Parse(File.ReadAllText(@"/sys/class/thermal/thermal_zone0/temp")) / 1000;
            await Context.Channel.SendMessageAsync($"Temp: {decimal.Round(temp).ToString()}°C");
        }

        [Command("reboot")]
        public async Task KillSwitch()
        {
            Process.Start("sudo", "reboot");
        }

        [Command("stats")]
        public async Task GetPiStats()
        {
            // Plan: check memory, uptime and whatever else i can think of
            // multiple lines in the file, will need to possible do a foreach to find the releveant and memory is in kb to i should probably convert that

            string memoryUsage = "";
            foreach (var line in File.ReadLines(@"/proc/meminfo"))
            {
                if (line.ToString().Contains("MemTotal")) {
                    memoryUsage = line.ToString();
                    await Context.Channel.SendMessageAsync($"Total Memory: {memoryUsage}"); // returns the full line, need to trim it to just the numbers, possibly a function to calculate kb,mb,gb
                    break;
                }
            }
        }

        // let users see their stored data (GDPR / Data Protection Act)
        [Command("userdata")]
        public async Task getUserData()
        {
            var db = new SqliteConnection("Data source=C:\\Users\\IainN\\userdata.db");
            db.Open();

            using var getData = db.CreateCommand();
            getData.CommandText = @"SELECT * FROM users WHERE id = @id";
            getData.Parameters.AddWithValue("@id", Context.User.Id);

            var userData = await getData.ExecuteNonQueryAsync(); // should look up the reader documentation
            await Context.Channel.SendMessageAsync($"Data stored on you: {userData.ToString()}");
        }

       
    }
}
