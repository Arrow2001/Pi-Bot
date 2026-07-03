using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PiBot.Commands
{
    public class Formula1 : ModuleBase<SocketCommandContext>
    {
        private static HttpClient formulaOneClient = new HttpClient();
        private readonly InteractiveService _service;

        public Formula1(InteractiveService interactive)
        {
            _service = interactive;
        }

        [Command("drivers")]
        [Alias("current drivers", "current driver")]
        public async Task CurrentDrivers()
        {
            string currentDriverGrid = await formulaOneClient.GetStringAsync("https://api.openf1.org/v1/drivers?session_key=latest");
            dynamic f1DriverContent = JsonConvert.DeserializeObject(currentDriverGrid);

            var driverPageList = new List<PageBuilder>();

            foreach (var driver in f1DriverContent)
            {
                driverPageList.Add(new PageBuilder()
                    .WithTitle(driver.full_name.ToString().ToUpper())
                    .WithDescription($"**Driver Number:** {driver.driver_number.ToString()}\n**Team:** {driver.team_name.ToString()}")
                    .WithThumbnailUrl(driver.headshot_url.ToString()));
            }

            var paginator = new StaticPaginatorBuilder()
                .WithPages(driverPageList)
                .WithUsers(Context.User)
                .WithFooter(PaginatorFooter.PageNumber)
                .Build();

            await _service.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(10));
        }

        /*[Command("grid")]
        public async Task StartingGrid()
        {
            string startingGridLink = await formulaOneClient.GetStringAsync("https://api.openf1.org/v1/starting_grid?session_key=latest");
            dynamic startingGrid = JsonConvert.DeserializeObject(startingGridLink);
            string currentDriverGrid = await formulaOneClient.GetStringAsync("https://api.openf1.org/v1/drivers?session_key=latest");
            dynamic f1DriverContent = JsonConvert.DeserializeObject(currentDriverGrid);
            int i = 0;
            string driverName = "";
            foreach (var driverNumber in startingGrid)
            {
                if (driverNumber.driver_number == f1DriverContent[0].driver_number)
                {
                    driverName = f1DriverContent[i].full_name.ToString();
                }
                i++;
                Console.WriteLine($"{driverNumber.driver_number.ToString()} = {driverName}\n");
            }

        }*/

        [Command("track weather")]
        public async Task GetLatestTrackWeather()
        {
            string trackweatherLink = await formulaOneClient.GetStringAsync("https://api.openf1.org/v1/weather?session_key=latest");
            dynamic TrackWeather = JsonConvert.DeserializeObject(trackweatherLink);

            EmbedBuilder weatherBuilder = new EmbedBuilder();
            weatherBuilder.WithTitle($"Track Weather");
            string date = TrackWeather[0].date;
            DateTime dt = DateTime.Parse(date);
            weatherBuilder.WithFooter($"Last recorded at: {dt.ToString("dd/MM/yyyy")}\n");

            string rainfall;
            if (TrackWeather[0].rainfall == 0)
                rainfall = "No Rain";
            else rainfall = "Raining";
            weatherBuilder.WithDescription($"**Air Temp:** {TrackWeather[0].air_temperature.ToString()}°C\n**Track Temp:** {TrackWeather[0].track_temperature}°C\n**Rainfall:** {rainfall}\n**Wind Speed:** {TrackWeather[0].wind_speed.ToString()} m/s" + 
                $"\n**Humidity:** {TrackWeather[0].humidity.ToString()}%");

            weatherBuilder.WithColor(Color.Blue);
            await Context.Channel.SendMessageAsync("", false, weatherBuilder.Build());

        }
    }
}