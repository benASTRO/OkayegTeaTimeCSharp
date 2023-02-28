﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HLE;
using HLE.Twitch.Models;
using OkayegTeaTime.Database;
using OkayegTeaTime.Database.Models;
using OkayegTeaTime.Settings;
using OkayegTeaTime.Spotify;
using OkayegTeaTime.Twitch.Attributes;
using OkayegTeaTime.Twitch.Models;
using OkayegTeaTime.Utils;
using StringHelper = HLE.StringHelper;

namespace OkayegTeaTime.Twitch.Commands;

[HandledCommand(CommandType.Listen)]
public readonly unsafe ref struct ListenCommand
{
    public ChatMessage ChatMessage { get; }

    public StringBuilder* Response { get; }

    private readonly TwitchBot _twitchBot;
    private readonly string? _prefix;
    private readonly string _alias;

    public ListenCommand(TwitchBot twitchBot, ChatMessage chatMessage, StringBuilder* response, string? prefix, string alias)
    {
        ChatMessage = chatMessage;
        Response = response;
        _twitchBot = twitchBot;
        _prefix = prefix;
        _alias = alias;
    }

    public void Handle()
    {
        if (!AppSettings.UserLists.SecretUsers.Contains(ChatMessage.UserId))
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, "this command is still being tested, you aren't allowed to use this command");
            return;
        }

        Regex pattern = _twitchBot.RegexCreator.Create(_alias, _prefix, @"\s((leave)|(stop))");
        if (pattern.IsMatch(ChatMessage.Message))
        {
            StopListening();
            return;
        }

        pattern = _twitchBot.RegexCreator.Create(_alias, _prefix, @"\ssync");
        if (pattern.IsMatch(ChatMessage.Message))
        {
            SyncListening();
            return;
        }

        pattern = _twitchBot.RegexCreator.Create(_alias, _prefix, @"\s\w+");
        if (pattern.IsMatch(ChatMessage.Message))
        {
            ListenToUser();
        }
    }

    private void ListenToUser()
    {
        SpotifyUser? listener = _twitchBot.SpotifyUsers[ChatMessage.Username];
        if (listener is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.YouCantListenToOtherUsersYouHaveToRegisterFirst);
            return;
        }

        using ChatMessageExtension messageExtension = new(ChatMessage);
        string hostUsername = new(messageExtension.LowerSplit[1]);
        SpotifyUser? host = _twitchBot.SpotifyUsers[hostUsername];
        if (host is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, "you can't listen to ", hostUsername, "'s music , they have to register first");
            return;
        }

        SpotifyItem item;
        try
        {
            Task<SpotifyItem> task = SpotifyController.ListenAlongWithAsync(listener, host);
            task.Wait();
            item = task.Result;
        }
        catch (SpotifyException ex)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, ex.Message);
            return;
        }
        catch (AggregateException ex)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace);
            if (ex.InnerException is null)
            {
                DbController.LogException(ex);
                Response->Append(Messages.ApiError);
                return;
            }

            Response->Append(ex.InnerException.Message);
            return;
        }

        Response->Append(ChatMessage.Username, Messages.CommaSpace, "now listening along with ", host.Username.Antiping(), " and playing ");
        switch (item)
        {
            case SpotifyTrack track:
            {
                string[] artists = track.Artists.Select(a => a.Name).ToArray();
                Span<char> joinBuffer = stackalloc char[250];
                int bufferLength = StringHelper.Join(artists, Messages.CommaSpace, joinBuffer);
                Response->Append(track.Name, " by ", joinBuffer[..bufferLength], " || ", track.IsLocal ? "local file" : track.Uri);
                break;
            }
            case SpotifyEpisode episode:
            {
                Response->Append(episode.Name, " by ", episode.Show.Name, " || ", episode.IsLocal ? "local file" : episode.Uri);
                break;
            }
            default:
            {
                Response->Append("an unknown item type monkaS");
                break;
            }
        }
    }

    private void SyncListening()
    {
        SpotifyUser? listener = _twitchBot.SpotifyUsers[ChatMessage.Username];
        if (listener is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.YouCantSyncYouHaveToRegisterFirst);
            return;
        }

        SpotifyUser? host = SpotifyController.GetListeningTo(listener);
        if (host is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.YouCantSyncBecauseYouArentListeningAlongWithAnybody);
            return;
        }

        SpotifyItem item;
        try
        {
            Task<SpotifyItem> task = SpotifyController.ListenAlongWithAsync(listener, host);
            task.Wait();
            item = task.Result;
        }
        catch (SpotifyException ex)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, ex.Message);
            return;
        }
        catch (AggregateException ex)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace);
            if (ex.InnerException is null)
            {
                DbController.LogException(ex);
                Response->Append(Messages.ApiError);
                return;
            }

            Response->Append(ex.InnerException.Message);
            return;
        }

        Response->Append(ChatMessage.Username, Messages.CommaSpace, "synced with ", host.Username.Antiping(), " and playing ");
        switch (item)
        {
            case SpotifyTrack track:
                string[] artists = track.Artists.Select(a => a.Name).ToArray();
                Span<char> joinBuffer = stackalloc char[250];
                int bufferLength = StringHelper.Join(artists, Messages.CommaSpace, joinBuffer);
                Response->Append(track.Name, " by ", joinBuffer[..bufferLength], " || ", track.IsLocal ? "local file" : track.Uri);
                break;
            case SpotifyEpisode episode:
                Response->Append(episode.Name, " by ", episode.Show.Name, " || ", episode.IsLocal ? "local file" : episode.Uri);
                break;
            default:
                Response->Append("an unknown item type monkaS");
                break;
        }
    }

    private void StopListening()
    {
        SpotifyUser? listener = _twitchBot.SpotifyUsers[ChatMessage.Username];
        if (listener is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.YouArentRegisteredYouHaveToRegisterFirst);
            return;
        }

        SpotifyUser? host = SpotifyController.GetListeningTo(listener);
        if (host is null)
        {
            Response->Append(ChatMessage.Username, Messages.CommaSpace, Messages.YouArentListeningAlongWithAnybody);
            return;
        }

        ListeningSession? listeningSession = SpotifyController.GetListeningSession(host);
        listeningSession?.Listeners.Remove(listener);
        Response->Append(ChatMessage.Username, Messages.CommaSpace, "stopped listening along with ", host.Username.Antiping());
    }
}
