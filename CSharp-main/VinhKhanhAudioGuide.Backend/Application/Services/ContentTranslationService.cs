using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class ContentTranslationService(AudioGuideDbContext dbContext) : IContentTranslationService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<ContentTranslation> UpsertTranslationAsync(
        string contentKey,
        string languageCode,
        string value,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ContentTranslations
            .FirstOrDefaultAsync(x => x.ContentKey == contentKey && x.LanguageCode == languageCode, cancellationToken);

        if (existing is not null)
        {
            existing.Value = value;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var translation = new ContentTranslation
        {
            ContentKey = contentKey,
            LanguageCode = languageCode,
            Value = value
        };

        _dbContext.ContentTranslations.Add(translation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return translation;
    }

    public async Task<string?> GetTranslationAsync(
        string contentKey,
        string languageCode,
        string fallbackLanguageCode = "vi",
        CancellationToken cancellationToken = default)
    {
        var direct = await _dbContext.ContentTranslations
            .Where(x => x.ContentKey == contentKey && x.LanguageCode == languageCode)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (direct is not null)
        {
            return direct;
        }

        return await _dbContext.ContentTranslations
            .Where(x => x.ContentKey == contentKey && x.LanguageCode == fallbackLanguageCode)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ContentTranslation>> GetTranslationsByKeyAsync(
        string contentKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ContentTranslations
            .Where(x => x.ContentKey == contentKey)
            .OrderBy(x => x.LanguageCode)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteTranslationAsync(
        string contentKey,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ContentTranslations
            .FirstOrDefaultAsync(x => x.ContentKey == contentKey && x.LanguageCode == languageCode, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.ContentTranslations.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
