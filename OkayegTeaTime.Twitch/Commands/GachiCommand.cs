﻿using System.Diagnostics.CodeAnalysis;
using HLE;
using HLE.Collections;
using HLE.Emojis;
using HLE.Twitch.Models;
using OkayegTeaTime.Models.Json;
using OkayegTeaTime.Settings;
using OkayegTeaTime.Twitch.Attributes;
using OkayegTeaTime.Twitch.Models;

namespace OkayegTeaTime.Twitch.Commands;

[HandledCommand(CommandType.Gachi)]
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
[SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
public readonly unsafe ref struct GachiCommand
{
    public ChatMessage ChatMessage { get; }

    public StringBuilder* Response { get; }

    private readonly TwitchBot _twitchBot;
    private readonly string? _prefix;
    private readonly string _alias;

    public GachiCommand(TwitchBot twitchBot, ChatMessage chatMessage, StringBuilder* response, string? prefix, string alias)
    {
        ChatMessage = chatMessage;
        Response = response;
        _twitchBot = twitchBot;
        _prefix = prefix;
        _alias = alias;
    }

    public void Handle()
    {
        GachiSong? gachi = AppSettings.GachiSongs.Random();
        if (gachi is null)
        {
            Response->Append(Messages.CouldntFindASong);
            return;
        }

        Response->Append(Emoji.PointRight, StringHelper.Whitespace, gachi.Title, " || ", gachi.Url, " gachiBASS");
    }
}
