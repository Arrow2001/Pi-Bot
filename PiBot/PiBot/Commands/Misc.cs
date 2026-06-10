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
using PiBot.Handlers;
using System.Security.Cryptography;

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
                if (line.ToString().Contains("MemTotal"))
                {
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
            var db = DatabaseHandler.getConnection();
            db.Open();

            using var getData = db.CreateCommand();
            getData.CommandText = @"SELECT * FROM users WHERE id = @id";
            getData.Parameters.AddWithValue("@id", Context.User.Id);

            var userData = await getData.ExecuteNonQueryAsync(); // should look up the reader documentation
            using var reader = await getData.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                StringBuilder stringBuilder = new StringBuilder("Your Stored Data:\n\n");

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columName = reader.GetName(i);
                    object value = reader.GetValue(i);
                    stringBuilder.Append($"{columName}: {value}\n"); // should add in a way to handle null values just in case
                }
                await Context.Channel.SendMessageAsync(stringBuilder.ToString()); // needs to be a dm to be secure
            } else
            {
                await Context.Channel.SendMessageAsync($"No data is stored for: {Context.User.Mention}");
            }
        }

        [Command("delete userdata")]
        public async Task deleteUserdata()
        {
            var db = DatabaseHandler.getConnection();
            db.Open();

            using var deleteData = db.CreateCommand();
            deleteData.CommandText = @"DELETE FROM users WHERE id = @id";
            deleteData.Parameters.AddWithValue("@id", Context.User.Id);

            try
            {
                var deletedData = deleteData.ExecuteNonQueryAsync(); // need a better variable name
                Console.WriteLine($"{DateTime.Now}: Deleting {Context.User.Username}'s data from the database");
                await Context.Channel.SendMessageAsync($"Deleted userdata for {Context.User.Mention}");
                Console.WriteLine($"{DateTime.Now}: {Context.User.Username}'s data has been deleted from the database.");
                // plan on writing this into a text file, with username or id hashed
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        [Command("test")]
        public  async Task testEncryption([Remainder]string data)
        {
            byte[] masterkey = CryptographyHandler.DeriveAKey(Config.bot.password, Config.bot.saltKey);
            using Aes aes = Aes.Create();
            byte[] testIV = aes.IV;

            string encryptedText = CryptographyHandler.EncryptData(data, masterkey, testIV);
            Console.WriteLine($"Original: {data}\nEncrypted: {encryptedText}");
            try
            {
                string decryptedText = CryptographyHandler.DecryptData(encryptedText, masterkey, testIV);
                Console.WriteLine($"Decrypted fromL: {encryptedText} to {decryptedText}");
            } catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }            
        }
    }
}
