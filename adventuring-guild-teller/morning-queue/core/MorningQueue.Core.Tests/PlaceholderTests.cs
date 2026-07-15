using MorningQueue.Core;
using Xunit;

namespace MorningQueue.Core.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Ping_ReturnsPong()
    {
        var placeholder = new Placeholder();
        Assert.Equal("pong", placeholder.Ping());
    }
}
