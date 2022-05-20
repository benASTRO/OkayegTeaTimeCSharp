﻿using HLE.Emojis;
using OkayegTeaTime.Twitch.Bot;
using OkayegTeaTime.Twitch.Models;

namespace OkayegTeaTime.Twitch.Handlers;

public class PajaAlertHandler : PajaHandler
{
    protected override Regex Pattern { get; } = new($@"^\s*pajaS\s+{Emoji.RotatingLight}\s+ALERT\s*$", RegexOptions.Compiled);
    protected override string Message => $"/me pajaStare {Emoji.RotatingLight} OBACHT";

    public PajaAlertHandler(TwitchBot twitchBot) : base(twitchBot)
    {
    }

    public override void Handle(TwitchChatMessage chatMessage)
    {
        if (chatMessage.ChannelId != _pajaChannelId || chatMessage.UserId != _pajaAlertUserId || !Pattern.IsMatch(chatMessage.Message))
        {
            return;
        }

        _twitchBot.TwitchClient.SendMessage(_pajaAlertChannel, Message);
        _twitchBot.TwitchClient.SendMessage(AppSettings.OfflineChatChannel, $"{AppSettings.DefaultEmote} {Emoji.RotatingLight}");
    }
}
