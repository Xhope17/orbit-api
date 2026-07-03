using Orbit.Application.Helpers;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Application.Services;

public class HashtagService : IHashtagService
{
    private readonly IGenericRepository<Hashtag> _hashtagRepo;
    private readonly IGenericRepository<PostHashtag> _postHashtagRepo;
    private readonly IGenericRepository<Post> _postRepo;

    public HashtagService(
        IGenericRepository<Hashtag> hashtagRepo,
        IGenericRepository<PostHashtag> postHashtagRepo,
        IGenericRepository<Post> postRepo)
    {
        _hashtagRepo = hashtagRepo;
        _postHashtagRepo = postHashtagRepo;
        _postRepo = postRepo;
    }

    public async Task ProcessPostHashtags(Guid postId, string? content)
    {
        var tags = HashtagHelper.ExtractHashtags(content);

        var existingLinks = await _postHashtagRepo.GetListAsync(ph => ph.PostId == postId);
        if (existingLinks.Count > 0)
        {
            foreach (var link in existingLinks)
                _postHashtagRepo.Remove(link);
        }

        if (tags.Count == 0)
        {
            await _postHashtagRepo.SaveChangesAsync();
            return;
        }

        var existingHashtags = await _hashtagRepo.GetListAsync(h => tags.Contains(h.Name));
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
                await _hashtagRepo.CreateAsync(hashtag);
                hashtagByName[tagName] = hashtag;
            }

            await _postHashtagRepo.CreateAsync(new PostHashtag
            {
                PostId = postId,
                HashtagId = hashtag.Id,
                CreatedAt = now,
            });
        }

        await _postHashtagRepo.SaveChangesAsync();
    }

    public async Task<List<TrendingHashtagResponse>> GetTrendingHashtagsAsync(int hours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var postHashtags = await _postHashtagRepo.GetListAsync(ph => ph.Post.CreatedAt >= since && ph.Post.IsActive);

        var grouped = postHashtags
            .GroupBy(ph => ph.HashtagId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToList();

        var hashtagIds = grouped.Select(g => g.Key).ToList();
        var hashtags = await _hashtagRepo.GetListAsync(h => hashtagIds.Contains(h.Id));
        var hashtagNames = hashtags.ToDictionary(h => h.Id, h => h.Name);

        var trending = grouped
            .Select(g => new TrendingHashtagResponse(
                hashtagNames.GetValueOrDefault(g.Key, "unknown"),
                g.Count()))
            .ToList();

        return trending;
    }
}
