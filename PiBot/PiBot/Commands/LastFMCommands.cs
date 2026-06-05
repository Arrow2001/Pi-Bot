using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace PiBot.Commands
{
    public class LastFMCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // gets the current or most recently played song (I should configure a database to store usernames)
        [Command("fm")]
        public async Task DisplayCurrenSong()
        {
            // http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=rj&api_key=YOUR_API_KEY&format=json
            // http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user=&api_key=YOUR_API_KEY&format=json
            var connection = new SqliteConnection("Data source=C:\\Users\\IainN\\userdata.db");
            connection.Open();

            using var checkUserExists = connection.CreateCommand();
            checkUserExists.CommandText = @"
                SELECT last_fm_username
                FROM users
                WHERE id = @id
            ";
            checkUserExists.Parameters.AddWithValue("@id", Context.User.Id);
            var existingUser = await checkUserExists.ExecuteScalarAsync();

            if (existingUser == null || string.IsNullOrEmpty(existingUser.ToString()))
            {
                await Context.Channel.SendMessageAsync($"Please set up your last.fm account");
                return;
            }
            else
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT last_fm_username
                FROM users
                WHERE id = @id
            ";
                command.Parameters.AddWithValue("@id", Context.User.Id);
                var fmuser = await command.ExecuteScalarAsync();

                string recentTracksLink = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={fmuser}&api_key={Config.bot.LastFmApiKey}&format=json";
                string rawJsonRecentTracks = await httpClient.GetStringAsync(recentTracksLink);
                dynamic recentTracksJson = JsonConvert.DeserializeObject(rawJsonRecentTracks);

                string totalUserPlaycount = $"http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user={fmuser}&api_key={Config.bot.LastFmApiKey}&format=json";
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

        }

        
        // set last.fm username
        [Command("lastfm set")]
        public async Task SetLastFMUsername(string username)
        {
            // connect to the database (i should probably add a function for this)
            var connection = new SqliteConnection("Data source=C:\\Users\\IainN\\userdata.db");
            connection.Open();

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
            var connection = new SqliteConnection("Data source=C:\\Users\\IainN\\userdata.db");
            connection.Open(); // gotta be a better way for this stuff

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

    }
}
