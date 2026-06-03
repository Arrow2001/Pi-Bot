using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace PiBot.Commands
{
    public class LastFMCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // gets the current or most recently played song (I should configure a database to store usernames)
        [Command("fm")]
        public async Task DisplayCurrenSong([Remainder] string user)
        {
            // http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user=rj&api_key=YOUR_API_KEY&format=json
            string recentTracksLink = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={Config.bot.LastFmApiKey}&format=json";
            string rawJsonRecentTracks = await httpClient.GetStringAsync(recentTracksLink);
            dynamic recentTracksJson = JsonConvert.DeserializeObject(rawJsonRecentTracks);

            EmbedBuilder bobTheBuilder = new EmbedBuilder();
            bobTheBuilder.WithAuthor(Context.User.Username, Context.User.GetAvatarUrl(), null);
            bobTheBuilder.AddField("Track:", (string)recentTracksJson.recenttracks.track[0].name, true);
            bobTheBuilder.AddField("Artist:", (string)recentTracksJson.recenttracks.track[0].artist["#text"], true);
            bobTheBuilder.ThumbnailUrl = (string)recentTracksJson.recenttracks.track[0].image[3]["#text"];
            await Context.Channel.SendMessageAsync("", false, bobTheBuilder.Build());

        }
    }
}
