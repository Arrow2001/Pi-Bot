using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

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
            // http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user=&api_key=YOUR_API_KEY&format=json

            string recentTracksLink = $"http://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={user}&api_key={Config.bot.LastFmApiKey}&format=json";
            string rawJsonRecentTracks = await httpClient.GetStringAsync(recentTracksLink);
            dynamic recentTracksJson = JsonConvert.DeserializeObject(rawJsonRecentTracks);

            string totalUserPlaycount = $"http://ws.audioscrobbler.com/2.0/?method=user.getinfo&user={user}&api_key={Config.bot.LastFmApiKey}&format=json";
            string rawUserInfoJson = await httpClient.GetStringAsync(totalUserPlaycount);
            dynamic userInfoJson = JsonConvert.DeserializeObject(rawUserInfoJson);

            string footer = "";
            if (recentTracksJson.recenttracks.track[0]["@attr"] != null)
            {
                footer = "Now Playing";
            }
            else { footer = "Most Recent Track"; }

            EmbedBuilder bobTheBuilder = new EmbedBuilder();
            bobTheBuilder.WithAuthor(Context.User.Username, Context.User.GetAvatarUrl(), null);
            bobTheBuilder.AddField("Track:", $"[{(string)recentTracksJson.recenttracks.track[0].name}]({(string)recentTracksJson.recenttracks.track[0].url})", true);
            bobTheBuilder.AddField("Artist:", $"[{(string)recentTracksJson.recenttracks.track[0].artist["#text"]}]({"https://www.last.fm/music/" + (string)recentTracksJson.recenttracks.track[0].artist["#text"].ToString().Replace(" ", "+")})", true);
            bobTheBuilder.ThumbnailUrl = (string)recentTracksJson.recenttracks.track[0].image[3]["#text"];
            bobTheBuilder.WithColor(Color.Blue); // might try to get ColourThief again and make the colour similar to that of the album cover
            bobTheBuilder.WithFooter($"{footer} | Total Scrobbles: {(string)userInfoJson.user.playcount}"); // should add a total amount of scrobbles

            await Context.Channel.SendMessageAsync("", false, bobTheBuilder.Build());

        }
    }
}
