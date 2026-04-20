using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IShopQRCodeService
{
    Task<ShopQRCode> CreateOrGetQRCodeAsync(Guid shopId, Guid poiId, string? poiCode = null, CancellationToken cancellationToken = default);
    Task<ShopQRCode?> GetQRCodeByIdAsync(Guid qrCodeId, CancellationToken cancellationToken = default);
    Task<ShopQRCode?> GetQRCodeByPoiAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopQRCode>> GetQRCodesByShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopQRCode>> GetQRCodesByShopExportAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task UpdateQRCodeImageAsync(Guid qrCodeId, string qrImageUrl, CancellationToken cancellationToken = default);
    Task DeactivateQRCodeAsync(Guid qrCodeId, CancellationToken cancellationToken = default);
    Task ActivateQRCodeAsync(Guid qrCodeId, CancellationToken cancellationToken = default);
}

public sealed class ShopQRCodeService : IShopQRCodeService
{
    private readonly AudioGuideDbContext _dbContext;

    public ShopQRCodeService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopQRCode> CreateOrGetQRCodeAsync(Guid shopId, Guid poiId, string? poiCode = null, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ShopQRCodes
            .FirstOrDefaultAsync(q => q.ShopId == shopId && q.PoiId == poiId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        // Get POI to confirm it belongs to this shop
        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null || poi.ShopId != shopId)
        {
            throw new InvalidOperationException("POI does not belong to this shop.");
        }

        var code = poiCode ?? poi.Code;
        var qrPayload = $"QR:{code}";

        var qrCode = new ShopQRCode
        {
            ShopId = shopId,
            PoiId = poiId,
            QRPayload = qrPayload,
            IsActive = true
        };

        _dbContext.ShopQRCodes.Add(qrCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return qrCode;
    }

    public async Task<ShopQRCode?> GetQRCodeByIdAsync(Guid qrCodeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopQRCodes.FindAsync(new object[] { qrCodeId }, cancellationToken: cancellationToken);
    }

    public async Task<ShopQRCode?> GetQRCodeByPoiAsync(Guid shopId, Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopQRCodes
            .FirstOrDefaultAsync(q => q.ShopId == shopId && q.PoiId == poiId, cancellationToken);
    }

    public async Task<IEnumerable<ShopQRCode>> GetQRCodesByShopAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopQRCodes
            .Where(q => q.ShopId == shopId)
            .Include(q => q.Poi)
            .OrderBy(q => q.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopQRCode>> GetQRCodesByShopExportAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopQRCodes
            .Where(q => q.ShopId == shopId && q.IsActive)
            .Include(q => q.Poi)
            .OrderBy(q => q.Poi!.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateQRCodeImageAsync(Guid qrCodeId, string qrImageUrl, CancellationToken cancellationToken = default)
    {
        var qrCode = await _dbContext.ShopQRCodes.FindAsync(new object[] { qrCodeId }, cancellationToken: cancellationToken);
        if (qrCode is null)
        {
            throw new KeyNotFoundException("QR Code not found.");
        }

        qrCode.QRImageUrl = qrImageUrl;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateQRCodeAsync(Guid qrCodeId, CancellationToken cancellationToken = default)
    {
        var qrCode = await _dbContext.ShopQRCodes.FindAsync(new object[] { qrCodeId }, cancellationToken: cancellationToken);
        if (qrCode is null)
        {
            throw new KeyNotFoundException("QR Code not found.");
        }

        qrCode.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateQRCodeAsync(Guid qrCodeId, CancellationToken cancellationToken = default)
    {
        var qrCode = await _dbContext.ShopQRCodes.FindAsync(new object[] { qrCodeId }, cancellationToken: cancellationToken);
        if (qrCode is null)
        {
            throw new KeyNotFoundException("QR Code not found.");
        }

        qrCode.IsActive = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
