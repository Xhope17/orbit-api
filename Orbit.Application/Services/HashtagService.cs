using Orbit.Application.Helpers;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;

namespace Orbit.Application.Services;

public class HashtagService : IHashtagService
{
    private readonly IUnitOfWork _uow;

    public HashtagService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task ProcessPostHashtags(Guid postId, string? content)
    {
        var tags = HashtagHelper.ExtractHashtags(content);

        var existingLinks = await _uow.PostHashtagRepository.GetListAsync(ph => ph.PostId == postId);
        if (existingLinks.Count > 0)
        {
            foreach (var link in existingLinks)
                await _uow.PostHashtagRepository.Delete(link);
        }

        if (tags.Count == 0)
        {
            await _uow.SaveChangesAsync();
            return;
        }

        var existingHashtags = await _uow.HashtagRepository.GetListAsync(h => tags.Contains(h.Name));
        var hashtagByName = existingHashtags.ToDictionary(h => h.Name);

        var now = DateTime.UtcNow;

        foreach (var tagName in tags)
        {
            if (!hashtagByName.TryGetValue(tagName, out var hashtag))
            {
                hashtag = new Hashtag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName,
                    CreatedAt = now,
                };
                await _uow.HashtagRepository.Create(hashtag);
                hashtagByName[tagName] = hashtag;
            }

            await _uow.PostHashtagRepository.Create(new PostHashtag
            {
                PostId = postId,
                HashtagId = hashtag.Id,
                CreatedAt = now,
            });
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<List<TrendingHashtagResponse>> GetTrendingHashtagsAsync(int hours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var postHashtags = await _uow.PostHashtagRepository.GetListAsync(ph => ph.Post.CreatedAt >= since && ph.Post.IsActive);

        var grouped = postHashtags
            .GroupBy(ph => ph.HashtagId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToList();

        var hashtagIds = grouped.Select(g => g.Key).ToList();
        var hashtags = await _uow.HashtagRepository.GetListAsync(h => hashtagIds.Contains(h.Id));
        var hashtagNames = hashtags.ToDictionary(h => h.Id, h => h.Name);

        var trending = grouped
            .Select(g => new TrendingHashtagResponse(
                hashtagNames.GetValueOrDefault(g.Key, "unknown"),
                g.Count()))
            .ToList();

        return trending;
    }
}
