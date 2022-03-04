﻿using System.Text.RegularExpressions;
using HLE.Strings;
using OkayegTeaTime.Spotify;
using OkayegTeaTime.Twitch.Bot;
using OkayegTeaTime.Twitch.Models;

namespace OkayegTeaTime.Twitch.Handlers;

public class WhisperHandler
{
    public TwitchBot TwitchBot { get; }

    private static readonly Regex _spotifyCodeUrlPattern = new(Pattern.SpotifyCodeUrl, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
    private static readonly Regex _codePattern = new(@"\?code=\S+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

    public WhisperHandler(TwitchBot twitchBot)
    {
        TwitchBot = twitchBot;
    }

    public void Handle(TwitchWhisperMessage whisperMessage)
    {
        if (_spotifyCodeUrlPattern.IsMatch(whisperMessage.Message))
        {
            Match match = _codePattern.Match(whisperMessage.Message);
            string code = match.Value.Remove("?code=");
            SpotifyController.GetNewAuthTokens(whisperMessage.Username, code).Wait();
        }
    }
}