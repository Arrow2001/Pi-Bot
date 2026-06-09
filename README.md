# Pi_Bot
This is a wee personal project that I host on my Raspberry Pi.

# Current Features:
- Lets me check my Pi's temperature.
- Retrieves data from [Last.FM](https://last.fm/home) to display someone's most recent song/currently playing song.
- Lets me reboot my Pi remotely
- Connects to a locally stored SQLite database that stores Discord IDs, last.fm usernames and uses SQL paramaters to avoid SQL injections.

# Improvements Made:
- Added a function to connect to the database without repeating it (needs to be made into a globally accessiable function).

# Planned Improvements:
- A command to let me see my Pi's memory usage, uptime etc.
- Add more Last.FM commands that will display a user's top 10 songs/artists/albums (weekly, monthly, yearly or all time).
- Utilise Last.FM's API so you can retrieve data about a specific song.
- Encrypt the data that is stored in the database and decrypt when needed.
- Convert JSON data into C# classes for cleaner and safer code.

# System Architecture
[Last.FM] -> (JSON) -> [C# Backend] -> (Discord.NET) -> [Discord UI]
