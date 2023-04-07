using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using HLE.Emojis;
using HLE.Memory;
using HLE.Twitch;
using HLE.Twitch.Models;
using OkayegTeaTime.Database;
using OkayegTeaTime.Models.Formula1;
using OkayegTeaTime.Models.OpenWeatherMap;
using OkayegTeaTime.Twitch.Attributes;
using OkayegTeaTime.Twitch.Controller;
using OkayegTeaTime.Twitch.Models;
using OkayegTeaTime.Utils;

namespace OkayegTeaTime.Twitch.Commands;

[HandledCommand(CommandType.Formula1)]
public readonly ref struct Formula1Command
{
    public ChatMessage ChatMessage { get; }

    private readonly TwitchBot _twitchBot;
    private readonly ref MessageBuilder _response;
    private readonly ReadOnlySpan<char> _prefix;
    private readonly ReadOnlySpan<char> _alias;

    private static Race[]? _races;
    private static readonly TimeSpan _nonRaceLength = TimeSpan.FromHours(1);
    private static readonly TimeSpan _raceLength = TimeSpan.FromHours(2);

    public Formula1Command(TwitchBot twitchBot, ChatMessage chatMessage, ref MessageBuilder response, ReadOnlySpan<char> prefix, ReadOnlySpan<char> alias)
    {
        ChatMessage = chatMessage;
        _response = ref response;
        _twitchBot = twitchBot;
        _prefix = prefix;
        _alias = alias;
        _races ??= GetRaces();
    }

    public void Handle()
    {
        if (_races is null)
        {
            _response.Append(ChatMessage.Username, ", ", Messages.ApiError);
            return;
        }

        Race? race = GetNextOrCurrentRace(_races);
        if (race is null)
        {
            _response.Append(ChatMessage.Username, ", ", Messages.ThereIsNoNextRace);
            return;
        }

        Regex pattern = _twitchBot.RegexCreator.Create(_alias, _prefix, @"\sweather");
        if (pattern.IsMatch(ChatMessage.Message))
        {
            SendWeatherInformation(race);
            return;
        }

        SendRaceInformation(race);
    }

    private void SendRaceInformation(Race race)
    {
        Span<char> charBuffer = stackalloc char[250];
        DateTime now = DateTime.UtcNow;

        _response.Append(ChatMessage.Username, ", ", race.RaceSession.Start > now ? "Next" : "Current", " race: ", race.Name);
        _response.Append(" at the ", race.Circuit.Name, " in ", race.Circuit.Location.Name, ", ", race.Circuit.Location.Country, ". ");
        _response.Append(Emoji.RacingCar, " ");
        if (race.RaceSession.Start > now)
        {
            race.RaceSession.Start.TryFormat(charBuffer, out int length, "R");
            _response.Append("The ", race.RaceSession.Name, " will start ", charBuffer[..length]);

            TimeSpan timeBetweenNowAndRaceStart = race.RaceSession.Start - now;
            timeBetweenNowAndRaceStart.TryFormat(charBuffer, out _, "g");
            int indexOfDot = charBuffer.IndexOf('.');
            _response.Append(" (in ", charBuffer[..Unsafe.As<int, Index>(ref indexOfDot)], "). ", Emoji.CheckeredFlag);
        }
        else
        {
            race.RaceSession.Start.TryFormat(charBuffer, out int length, "t");
            _response.Append("The ", race.RaceSession.Name, " started ", charBuffer[..length], " GMT. ", Emoji.CheckeredFlag);
        }

        Session session = GetNextOrCurrentSession(race);
        if (ReferenceEquals(session, race.RaceSession))
        {
            return;
        }

        if (session.Start > now)
        {
            session.Start.TryFormat(charBuffer, out int length, "R");
            _response.Append("Next session: ", session.Name, ", starting ", charBuffer[..length], " (in ");

            TimeSpan timeBetweenNowAndRaceStart = session.Start - now;
            timeBetweenNowAndRaceStart.TryFormat(charBuffer, out _, "g");
            int indexOfDot = charBuffer.IndexOf('.');
            _response.Append(charBuffer[..indexOfDot], ").");
        }
        else if (session.Start + _nonRaceLength > now)
        {
            session.Start.TryFormat(charBuffer, out int length, "t");
            _response.Append("Current session: ", session.Name, ", started ", charBuffer[..length], " GMT.");
        }
    }

    private void SendWeatherInformation(Race race)
    {
        int latitude = (int)Math.Round(double.Parse(race.Circuit.Location.Latitude));
        int longitude = (int)Math.Round(double.Parse(race.Circuit.Location.Longitude));
        WeatherData? weatherData = _twitchBot.WeatherController.GetWeather(latitude, longitude, false);
        if (weatherData is null)
        {
            _response.Append(ChatMessage.Username, ", ", Messages.ApiError);
            return;
        }

        weatherData.CityName = race.Circuit.Location.Name;
        weatherData.Location.Country = race.Circuit.Location.Country;
        _response.Append(ChatMessage.Username, ", ");
        WeatherController.WriteResponse(weatherData, ref _response, false);
    }

    private static Race? GetNextOrCurrentRace(ReadOnlySpan<Race> races)
    {
        for (int i = 0; i < races.Length; i++)
        {
            Race race = races[i];
            if (race.RaceSession.Start + _raceLength > DateTime.UtcNow)
            {
                return race;
            }
        }

        return null;
    }

    private static Session GetNextOrCurrentSession(Race race)
    {
        using RentedArray<Session> sessions = ArrayPool<Session>.Shared.Rent(5);
        if (race.HasSprintRace)
        {
            sessions[0] = race.PracticeOneSession;
            sessions[1] = race.QualifyingSession;
            sessions[2] = race.PracticeTwoSession;
            sessions[3] = race.SprintSession;
            sessions[4] = race.RaceSession;
        }
        else
        {
            sessions[0] = race.PracticeOneSession;
            sessions[1] = race.PracticeTwoSession;
            sessions[2] = race.PracticeThreeSession;
            sessions[3] = race.QualifyingSession;
            sessions[4] = race.RaceSession;
        }

        for (int i = 0; i < 5; i++)
        {
            Session session = sessions[i];
            DateTime sessionEnd = ReferenceEquals(session, race.RaceSession) ? session.Start + _raceLength : session.Start + _nonRaceLength;
            if (sessionEnd > DateTime.UtcNow)
            {
                return session;
            }
        }

        throw new UnreachableException("There has to be a current or next session if this has been called");
    }

    private static Race[]? GetRaces()
    {
        try
        {
            HttpGet request = new("https://ergast.com/api/f1/current.json");
            if (request.Result is null)
            {
                return null;
            }

            JsonElement json = JsonSerializer.Deserialize<JsonElement>(request.Result);
            JsonElement jRaces = json.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");
            Race[]? races = JsonSerializer.Deserialize<Race[]?>(jRaces.GetRawText());
            if (races is null)
            {
                return null;
            }

            CreateSessionStartTimes(races, jRaces);
            return races;
        }
        catch (Exception ex)
        {
            DbController.LogException(ex);
            return null;
        }
    }

    private static void CreateSessionStartTimes(ReadOnlySpan<Race> races, JsonElement jRaces)
    {
        try
        {
            (string RaceProperty, string? JsonProperty, string SessionName)[] props =
            {
                (nameof(Race.PracticeOneSession), "FirstPractice", "Free practice 1"),
                (nameof(Race.PracticeTwoSession), "SecondPractice", "Free practice 2"),
                (nameof(Race.PracticeThreeSession), "ThirdPractice", "Free practice 3"),
                (nameof(Race.QualifyingSession), "Qualifying", "Qualifying"),
                (nameof(Race.SprintSession), "Sprint", "Sprint race"),
                (nameof(Race.RaceSession), null, "Grand Prix")
            };

            for (int i = 0; i < races.Length; i++)
            {
                foreach (var prop in props)
                {
                    JsonElement jProp;
                    if (prop.JsonProperty is null)
                    {
                        jProp = jRaces[i];
                    }
                    else if (!jRaces[i].TryGetProperty(prop.JsonProperty, out jProp))
                    {
                        continue;
                    }

                    JsonElement jDate = jProp.GetProperty("date");
                    JsonElement jTime = jProp.GetProperty("time");
                    string? date = jDate.GetString();
                    string? time = jTime.GetString();
                    if (date is null || time is null)
                    {
                        throw new InvalidOperationException($"{nameof(date)} is {date ?? "null"} and {nameof(time)} is {time ?? "null"}");
                    }

                    DateTime startTime = DateTime.Parse($"{date}T{time}").ToUniversalTime();
                    Session session = new(prop.SessionName, startTime);
                    typeof(Race).GetProperty(prop.RaceProperty)!.SetValue(races[i], session);
                }
            }
        }
        catch (Exception ex)
        {
            DbController.LogException(ex);
        }
    }
}
