using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IContentTranslationService
{
    Task<ContentTranslation> UpsertTranslationAsync(
        string contentKey,
        string languageCode,
        string value,
        CancellationToken cancellationToken = default);

    Task<string?> GetTranslationAsync(
        string contentKey,
        string languageCode,
        string fallbackLanguageCode = "vi",
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ContentTranslation>> GetTranslationsByKeyAsync(
        string contentKey,
        CancellationToken cancellationToken = default);

    Task DeleteTranslationAsync(
        string contentKey,
        string languageCode,
        CancellationToken cancellationToken = default);
}
