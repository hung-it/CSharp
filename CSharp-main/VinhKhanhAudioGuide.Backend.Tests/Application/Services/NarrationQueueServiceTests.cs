using VinhKhanhAudioGuide.Backend.Application.Services;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class NarrationQueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_DeduplicatesByPoi()
    {
        var service = new NarrationQueueService();
        var userId = Guid.NewGuid();
        var poiId = Guid.NewGuid();

        var first = await service.EnqueueAsync(userId, poiId, "a.mp3", priority: 1);
        var second = await service.EnqueueAsync(userId, poiId, "a.mp3", priority: 1);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task TryStartNextAsync_UsesPriorityOrder()
    {
        var service = new NarrationQueueService();
        var userId = Guid.NewGuid();

        await service.EnqueueAsync(userId, Guid.NewGuid(), "low.mp3", priority: 1);
        await service.EnqueueAsync(userId, Guid.NewGuid(), "high.mp3", priority: 10);

        var current = await service.TryStartNextAsync(userId);

        Assert.NotNull(current);
        Assert.Equal("high.mp3", current!.AudioPath);
    }

    [Fact]
    public async Task CompleteCurrentAsync_AllowsStartingNextItem()
    {
        var service = new NarrationQueueService();
        var userId = Guid.NewGuid();

        await service.EnqueueAsync(userId, Guid.NewGuid(), "1.mp3", priority: 1);
        await service.EnqueueAsync(userId, Guid.NewGuid(), "2.mp3", priority: 1);

        var first = await service.TryStartNextAsync(userId);
        await service.CompleteCurrentAsync(userId);
        var second = await service.TryStartNextAsync(userId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Id, second!.Id);
    }
}
