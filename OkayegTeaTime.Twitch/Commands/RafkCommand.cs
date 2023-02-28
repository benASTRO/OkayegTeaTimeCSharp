﻿using System.Diagnostics.CodeAnalysis;
using HLE;
using HLE.Twitch.Models;
using OkayegTeaTime.Database.Models;
using OkayegTeaTime.Models.Json;
using OkayegTeaTime.Twitch.Attributes;
using OkayegTeaTime.Twitch.Models;

namespace OkayegTeaTime.Twitch.Commands;

[HandledCommand(CommandType.Rafk)]
public readonly unsafe ref struct RafkCommand
{
    public ChatMessage ChatMessage { get; }

    public StringBuilder* Response { get; }

    private readonly TwitchBot _twitchBot;
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
    private readonly string? _prefix;
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members")]
    private readonly string _alias;

    public RafkCommand(TwitchBot twitchBot, ChatMessage chatMessage, StringBuilder* response, string? prefix, string alias)
    {
        ChatMessage = chatMessage;
        Response = response;
        _twitchBot = twitchBot;
        _prefix = prefix;
        _alias = alias;
    }

    public void Handle()
    {
        User? user = _twitchBot.Users.Get(ChatMessage.UserId, ChatMessage.Username);
        if (user is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.CantResumeYourAfkStatusBecauseYouNeverWentAfkBefore);
            return;
        }

        user.IsAfk = true;
        AfkCommand cmd = _twitchBot.CommandController[user.AfkType];
        Response->Append(new AfkMessage(user, cmd).Resuming);
    }
}
