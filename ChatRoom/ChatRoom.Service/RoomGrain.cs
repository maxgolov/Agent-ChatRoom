﻿using ChatRoom.Common;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Utilities;

namespace ChatRoom.Room;

public class RoomGrain : Grain, IRoomGrain
{
    private readonly ObserverManager<IRoomObserver> _roomObserver;
    private readonly List<AgentInfo> _members = new(100);
    private readonly List<ChannelInfo> _channels = new(100);

    public RoomGrain(ILogger<RoomGrain> logger)
    {
        _roomObserver = new ObserverManager<IRoomObserver>(TimeSpan.FromMinutes(1), logger);
    }

    public Task<AgentInfo[]> GetMembers() => Task.FromResult(_members.ToArray());

    public async Task Join(AgentInfo nickname)
    {
        if (_members.Any(x => x.Name == nickname.Name))
        {
            return;
        }

        _members.Add(nickname);
        var agentJoinMessage = new ChatMsg("System", $"{nickname.Name} joins the chat room.");
        await _roomObserver.Notify(x => x.Notification(agentJoinMessage));
        await _roomObserver.Notify(x => x.Join(nickname));
    }

    public async Task Leave(string nickname)
    {
        var agentInfo = _members.FirstOrDefault(x => x.Name == nickname);
        if (agentInfo is null)
        {
            return;
        }

        _members.Remove(agentInfo);
        var agentLeaveMessage = new ChatMsg("System", $"{nickname} leaves the chat room.");
        await _roomObserver.Notify(x => x.Notification(agentLeaveMessage));
        await _roomObserver.Notify(x => x.Leave(agentInfo));
    }

    public Task<ChannelInfo[]> GetChannels()
    {
        return Task.FromResult(_channels.ToArray());
    }

    public async Task CreateChannel(ChannelInfo channelInfo)
    {
        if (_channels.Any(x => x.Name == channelInfo.Name))
        {
            return;
        }

        _channels.Add(channelInfo);
    }

    public async Task DeleteChannel(string channelName)
    {
        if (_channels.All(x => x.Name != channelName))
        {
            return;
        }

        var channel = _channels.First(x => x.Name == channelName);
        _channels.Remove(channel);
    }

    public Task Subscribe(IRoomObserver observer)
    {
        _roomObserver.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public Task Unsubscribe(IRoomObserver observer)
    {
        _roomObserver.Unsubscribe(observer);

        return Task.CompletedTask;
    }

    public async Task AddAgentToChannel(ChannelInfo channelInfo, AgentInfo agentInfo)
    {
        if (_channels.All(x => x.Name != channelInfo.Name))
        {
            var channelNotFoundMessage = new ChatMsg("System", $"Channel '{channelInfo.Name}' not found.");
            await _roomObserver.Notify(x => x.Notification(channelNotFoundMessage));
        }

        var channel = _channels.First(x => x.Name == channelInfo.Name);

        if (_members.All(x => x.Name != agentInfo.Name))
        {
            var agentNotFoundMessage = new ChatMsg("System", $"Agent '{agentInfo.Name}' not found.");
            await _roomObserver.Notify(x => x.Notification(agentNotFoundMessage));
        }

        var agent = _members.First(x => x.Name == agentInfo.Name);

        await _roomObserver.Notify(x => x.AddMemberToChannel(channel, agent));
    }

    public async Task RemoveAgentFromChannel(ChannelInfo channelInfo, AgentInfo agentInfo)
    {
        if (_channels.All(x => x.Name != channelInfo.Name))
        {
            var channelNotFoundMessage = new ChatMsg("System", $"Channel '{channelInfo.Name}' not found.");
            await _roomObserver.Notify(x => x.Notification(channelNotFoundMessage));
        }

        var channel = _channels.First(x => x.Name == channelInfo.Name);

        if (_members.All(x => x.Name != agentInfo.Name))
        {
            var agentNotFoundMessage = new ChatMsg("System", $"Agent '{agentInfo.Name}' not found.");
            await _roomObserver.Notify(x => x.Notification(agentNotFoundMessage));
        }

        var agent = _members.First(x => x.Name == agentInfo.Name);

        await _roomObserver.Notify(x => x.RemoveMemberFromChannel(channel, agent));
    }
}
