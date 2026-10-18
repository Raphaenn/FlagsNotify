using FlagsNotify.Domain;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Enuns;

namespace FlagsNotify.Domain.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_TrimsFields()
    {
        Notification notification = Notification.Create(
            " event-1 ",
            " user-1 ",
            " sender-1 ",
            " Sender ",
            EventTypes.HandleLike,
            " Title ",
            " Body ",
            " /(tabs)/(home)/home ",
            DateTimeOffset.UtcNow);

        Assert.Equal("event-1", notification.EventId);
        Assert.Equal("user-1", notification.RecipientUserId);
        Assert.Equal("sender-1", notification.SenderUserId);
        Assert.Equal("Sender", notification.SenderUserName);
        Assert.Equal("Title", notification.Title);
        Assert.Equal("Body", notification.Body);
        Assert.Equal("/(tabs)/(home)/home", notification.Route);
        Assert.False(notification.IsRead);

        notification.MarkAsRead(DateTimeOffset.UtcNow);

        Assert.True(notification.IsRead);
    }

    [Fact]
    public void Create_WithoutTitle_ThrowsDomainValidation()
    {
        Assert.Throws<DomainValidationException>(
            () => Notification.Create(
                "event-1",
                "user-1",
                null,
                null,
                EventTypes.HandleLike,
                string.Empty,
                "Body",
                null,
                DateTimeOffset.UtcNow));
    }
}
