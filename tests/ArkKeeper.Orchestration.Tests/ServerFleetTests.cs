using ArkKeeper.Core.Profiles;
using Xunit;

namespace ArkKeeper.Orchestration.Tests;

public class ServerFleetTests
{
    [Fact]
    public async Task GetOrAdd_CalledTwiceForSameProfile_ReturnsTheSameInstance()
    {
        await using var fleet = new ServerFleet();
        var profile = new ServerProfile();

        var first = fleet.GetOrAdd(profile);
        var second = fleet.GetOrAdd(profile);

        Assert.Same(first, second);
        Assert.Single(fleet.Servers);
    }

    [Fact]
    public async Task GetOrAdd_ForDifferentProfiles_TracksBothIndependently()
    {
        await using var fleet = new ServerFleet();
        var profileA = new ServerProfile { ProfileName = "A" };
        var profileB = new ServerProfile { ProfileName = "B" };

        var serverA = fleet.GetOrAdd(profileA);
        var serverB = fleet.GetOrAdd(profileB);

        Assert.NotSame(serverA, serverB);
        Assert.Equal(2, fleet.Servers.Count);
    }

    [Fact]
    public async Task Find_ForUnknownProfileId_ReturnsNull()
    {
        await using var fleet = new ServerFleet();

        Assert.Null(fleet.Find(Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromFleet()
    {
        await using var fleet = new ServerFleet();
        var profile = new ServerProfile();
        fleet.GetOrAdd(profile);

        await fleet.RemoveAsync(profile.ProfileId);

        Assert.Null(fleet.Find(profile.ProfileId));
        Assert.Empty(fleet.Servers);
    }
}
