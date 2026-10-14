using FlagsNotify.Domain;
using FlagsNotify.Domain.Entities;

namespace FlagsNotify.Domain.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_TrimsFields()
    {
        Notification notification = Notification.Create(
            " event-1 ",
            " user-1 ",
            " Title ",
            " Body ",
            DateTimeOffset.UtcNow);

        Assert.Equal("event-1", notification.EventId);
        Assert.Equal("user-1", notification.RecipientUserId);
        Assert.Equal("Title", notification.Title);
        Assert.Equal("Body", notification.Body);
    }

    [Fact]
    public void Create_WithoutTitle_ThrowsDomainValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => Notification.Create(
                "event-1",
                "user-1",
                string.Empty,
                "Body",
                DateTimeOffset.UtcNow));
    }
}
