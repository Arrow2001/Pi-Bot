using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PiBot.Handlers;
using Discord.WebSocket;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace PiBot.Commands
{
    public class LastFMCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // check FM exists function for DRY principle
        private async Task<string> CheckFmUserExists(ulong discordID)
        {
            using var connection = DatabaseHandler.getConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT last_fm_username FROM users WHERE id = @id";
            command.Parameters.AddWithValue("@id", discordID);
            var result = await command.ExecuteScalarAsync(); // still confused about the difference between certain sql asyncs
            return result?.ToString();

        }

        // gets the current or most recently played song (I should configure a database to store usernames)
        [Command("fm")]
        public async Task DisplayCurrenSong()
        {
            // http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=rj&api_key=YOUR_API_KEY&format=json
            // http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user=&api_key=YOUR_API_KEY&format=json

            string fmUser = await CheckFmUserExists(Context.User.Id);

            if (string.IsNullOrEmpty(fmUser))
            {
                await Context.Channel.SendMessageAsync($"Please set up your last.fm account");
                return;
            }

            string recentTracksLink = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={fmUser}&api_key={Config.bot.LastFmApiKey}&format=json";
            string rawJsonRecentTracks = await httpClient.GetStringAsync(recentTracksLink);
            dynamic recentTracksJson = JsonConvert.DeserializeObject(rawJsonRecentTracks);

            string totalUserPlaycount = $"http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user={fmUser}&api_key={Config.bot.LastFmApiKey}&format=json";
            string rawUserInfoJson = await httpClient.GetStringAsync(totalUserPlaycount);
            dynamic userInfoJson = JsonConvert.DeserializeObject(rawUserInfoJson);

            string footer;
            if (recentTracksJson.recenttracks.track[0]["@attr"] != null)
            {
                footer = "Now Playing";
            }
            else { footer = "Most Recent Track"; }

            EmbedBuilder bobTheBuilder = new EmbedBuilder();
            bobTheBuilder.WithAuthor(Context.User.Username, Context.User.GetAvatarUrl(), null);
            bobTheBuilder.AddField("Artist:", $"[{(string)recentTracksJson.recenttracks.track[0].artist["#text"]}]({"https://www.last.fm/music/" + (string)recentTracksJson.recenttracks.track[0].artist["#text"].ToString().Replace(" ", "+")})", true);
            bobTheBuilder.AddField("Track:", $"[{(string)recentTracksJson.recenttracks.track[0].name}]({(string)recentTracksJson.recenttracks.track[0].url})", true);
            bobTheBuilder.ThumbnailUrl = (string)recentTracksJson.recenttracks.track[0].image[3]["#text"];
            bobTheBuilder.WithColor(Color.Blue); // might try to get ColourThief again and make the colour similar to that of the album cover
            bobTheBuilder.WithFooter($"{footer} | Total Scrobbles: {(string)userInfoJson.user.playcount}"); // should add a total amount of scrobbles

            await Context.Channel.SendMessageAsync("", false, bobTheBuilder.Build());
        }
        


        // last.fm help
        [Command("fm help")]
        [Alias("helpfm", "fmhelp", "help fm")]
        public async Task displayFmHelp()
        {
            EmbedBuilder bobTheBuilder2 = new EmbedBuilder();
            bobTheBuilder2.Title = $"Last.FM Help Menu";
            bobTheBuilder2.WithDescription($"1. `lastfm set [username]` - Sets and stores your last.fm username" +
                $"\n2. `last.fm clear` - Removes your username from the bot." +
                $"\n3. `fm` - Displays your current or most recent song." +
                $"\n4. `fm arists [timeframe (7day, 1month, 3month, 6month, 1y, overall]` - Displays your top artists for the period selected." +
                $"\n5. `fm help` - Displays this help menu."); // there will be a better way but idk how yet
            bobTheBuilder2.WithColor(Color.Blue);
            bobTheBuilder2.WithFooter($"Disclaimer: by inputting your last.fm username, you hereby agree to have your Discord ID, Discord Username and Last.FM username stored securely in the bot.\nYou have the right to access any data stored.\nYou have the right to be delete all data stored about you.");
            await Context.Channel.SendMessageAsync("", false, bobTheBuilder2.Build());
        }
        
        // set last.fm username
        [Command("lastfm set")]
        public async Task SetLastFMUsername(string username)
        {
            // connect to the database (i should probably add a function for this)
            var connection = DatabaseHandler.getConnection();

            using var checkFmUserameExists = connection.CreateCommand();
            checkFmUserameExists.CommandText = @"SELECT last_fm_username FROM users WHERE id = @id";
            checkFmUserameExists.Parameters.AddWithValue("@id", Context.User.Id);
            var existingUser = await checkFmUserameExists.ExecuteScalarAsync();
            if (existingUser == null || string.IsNullOrWhiteSpace(existingUser.ToString()))
            {
                using var command = connection.CreateCommand();
                // rather messy all on one line, will fix it later.
                command.CommandText = @"INSERT INTO users (id, name, last_fm_username) 
                                        VALUES (@id, @name, @last_fm_username) 
                                        ON CONFLICT(id) 
                                        DO UPDATE 
                                        SET last_fm_username = excluded.last_fm_username";

                command.Parameters.AddWithValue("@id", Context.User.Id); // error because it already exists
                command.Parameters.AddWithValue("@name", Context.User.Username);
                command.Parameters.AddWithValue("@last_fm_username", username);
                await command.ExecuteScalarAsync();
                await Context.Channel.SendMessageAsync($"Your last.fm username has been stored.");
            }
            else
            {
                using var command = connection.CreateCommand();

                command.CommandText = @"
                    UPDATE users
                    SET last_fm_username = @last_fm_username
                    WHERE id = @id
                ";
                command.Parameters.AddWithValue("@id", Context.User.Id);
                command.Parameters.AddWithValue("@last_fm_username", username);

                await command.ExecuteNonQueryAsync();
                await Context.Channel.SendMessageAsync($"Added {username} to table.");
            }
        }

        [Command("lastfm clear")]
        public async Task clearLastFmUsername()
        {
            var connection = DatabaseHandler.getConnection();

            using var checkForName = connection.CreateCommand();
            checkForName.CommandText = @"SELECT last_fm_username FROM users WHERE id = @id";
            checkForName.Parameters.AddWithValue("@id", Context.User.Id);
            var checkingName = await checkForName.ExecuteScalarAsync();

            if (checkingName == null && string.IsNullOrEmpty(checkingName.ToString()))
            { 
                await Context.Channel.SendMessageAsync($"You don't have a name set up.");
                return;
            } 
            using var deleteName = connection.CreateCommand();
            deleteName.CommandText = @"UPDATE users SET last_fm_username = NULL WHERE id = @id";
            deleteName.Parameters.AddWithValue("@id", Context.User.Id);
            await deleteName.ExecuteNonQueryAsync();
            await Context.Channel.SendMessageAsync($"Your last.fm username has been reset.");
            
        }

        [Command("fm artists")]
        [Alias("ta", "top artists", "topartists")]
        public async Task getTopArtists(SocketGuildUser targetuser = null, string timeframe = "7day")
        {
            if (targetuser == null)
                targetuser = (SocketGuildUser)Context.User;

            var checkingFmExists = await CheckFmUserExists(targetuser.Id);
            
            StringBuilder topArtistsLink = new StringBuilder($"http://ws.audioscrobbler.com/2.0/?method=user.gettopartists&user={checkingFmExists}&api_key={Config.bot.LastFmApiKey}&format=json&period=");
            string timeframeForEmbed = "";
            // overall | 7day | 1month | 3month | 6month | 12month
            switch (timeframe)
            {
                case "7day":
                    topArtistsLink.Append("7day");
                    timeframeForEmbed = "Weekly";
                    break;
                case "overall":
                case "all":
                    topArtistsLink.Append("overall");
                    timeframeForEmbed = "All-time";
                    break;
                case "1month":
                case "1m":
                    topArtistsLink.Append($"1month");
                    timeframeForEmbed = "Monthly";
                        break;
                case "3month":
                case "3m":
                case "3 months":
                case "3months":
                    topArtistsLink.Append($"3month");
                    timeframeForEmbed = "Quarterly";
                    break;
                case "6month":
                case "6m":
                case "6 months":
                case "6months":
                    topArtistsLink.Append($"6month"); //hmm regex may work better here than typing each case, not sure
                    timeframeForEmbed = "Half-yearly";
                    break;
                case "12month":
                case "12m":
                case "12 months":
                case "12months":
                case "1 year":
                case "1year":
                case "1y":
                    topArtistsLink.Append($"12month");
                    timeframeForEmbed = "Yearly";
                    break;
            }

            if (checkingFmExists == null || string.IsNullOrEmpty(checkingFmExists.ToString()))
            {
                await Context.Channel.SendMessageAsync($"Please set up your last.fm in the bot by using `.lastfm set [username]`");
                return;
            } else
            {
                try
                {
                    string aristJson = await httpClient.GetStringAsync(topArtistsLink.ToString());
                    dynamic artistInfo = JsonConvert.DeserializeObject(aristJson);
                    EmbedBuilder topArtistEmbed = new EmbedBuilder();
                    topArtistEmbed.Title = $"Top 10 {timeframeForEmbed} Artists"; // gotta fix the timeframe to be readable on the embed

                    StringBuilder topTenArtists = new StringBuilder();
                    for (int i = 0; i < 10; i++)
                    {
                        topTenArtists.Append($"{i + 1}. [{artistInfo.topartists.artist[i].name}]({artistInfo.topartists.artist[i].url}) - {artistInfo.topartists.artist[i].playcount} plays\n");
                    }
                    topArtistEmbed.WithDescription(topTenArtists.ToString());
                    topArtistEmbed.WithColor(Color.Blue);
                    await Context.Channel.SendMessageAsync("", false, topArtistEmbed.Build());

                } catch (Exception ex) { Console.WriteLine($"Error: {ex.ToString()}"); }
            }

        }

        // get the top tracks
        [Command("top tracks")]
        [Alias("tt")]
        public async Task GetTopTracks(SocketGuildUser targetUser = null, string timeframe = "7day")
        {
            if (targetUser == null)
                targetUser = (SocketGuildUser)Context.User;

            var checkFmexists = CheckFmUserExists(targetUser.Id);
            if (checkFmexists == null || string.IsNullOrEmpty(checkFmexists.ToString()))
            {
                await Context.Channel.SendMessageAsync($"Please set up your last.fm in the bot. Use `.fm help` for more.");
                return;
            }
            string fmUser = await CheckFmUserExists(targetUser.Id);
            // http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user=iain2001&api_key=YOUR_API_KEY&format=json
            string topTracksLink = $"http://ws.audioscrobbler.com/2.0/?method=user.gettoptracks&user={fmUser}&api_key={Config.bot.LastFmApiKey}&period={timeframe}&format=json";
            StringBuilder topSongsString = new StringBuilder();
            try
            {
                string songJson = await httpClient.GetStringAsync(topTracksLink);
                dynamic songJsonRaw = JsonConvert.DeserializeObject(songJson);

                for (int i = 0; i < 10; i++)
                {
                    topSongsString.Append($"{i+1}. {songJsonRaw.toptracks.track[i].artist.name} - {songJsonRaw.toptracks.track[i].name}: {songJsonRaw.toptracks.track[i].playcount}");
                }
                EmbedBuilder songBuilder = new EmbedBuilder();
                songBuilder.WithTitle($"Top songs");
                songBuilder.WithDescription(songBuilder.ToString());
                songBuilder.WithColor(Color.Blue);
                await Context.Channel.SendMessageAsync("", false, songBuilder.Build());
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
