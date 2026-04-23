using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class TourService(AudioGuideDbContext dbContext) : ITourService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<IEnumerable<Tour>> GetAllToursAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tours
            .Include(t => t.Stops)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tour?> GetTourByIdAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tours
            .Include(t => t.Stops.OrderBy(s => s.Sequence))
            .ThenInclude(s => s.Poi)
            .FirstOrDefaultAsync(t => t.Id == tourId, cancellationToken);
    }

    public async Task<Tour?> GetTourByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tours
            .Include(t => t.Stops.OrderBy(s => s.Sequence))
            .ThenInclude(s => s.Poi)
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
    }

    public async Task<Tour> CreateTourAsync(string code, string name, string? description = null, Guid? shopId = null, CancellationToken cancellationToken = default)
    {
        var existingTour = await GetTourByCodeAsync(code, cancellationToken);
        if (existingTour is not null)
        {
            throw new InvalidOperationException($"Tour with code '{code}' already exists.");
        }

        var tour = new Tour
        {
            Code = code,
            Name = name,
            Description = description,
            ShopId = shopId
        };

        _dbContext.Tours.Add(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task<Tour> UpdateTourAsync(Guid tourId, string? name = null, string? description = null, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours.FindAsync(new object[] { tourId }, cancellationToken: cancellationToken);
        if (tour is null)
        {
            throw new KeyNotFoundException($"Tour with ID {tourId} not found.");
        }

        if (name is not null)
            tour.Name = name;
        if (description is not null)
            tour.Description = description;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return tour;
    }

    public async Task DeleteTourAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours.FindAsync(new object[] { tourId }, cancellationToken: cancellationToken);
        if (tour is null)
        {
            return;
        }

        var stops = await _dbContext.TourStops
            .Where(x => x.TourId == tourId)
            .ToListAsync(cancellationToken);

        _dbContext.TourStops.RemoveRange(stops);
        _dbContext.Tours.Remove(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<TourStop>> GetTourStopsAsync(Guid tourId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TourStops
            .Include(s => s.Poi)
            .Where(s => s.TourId == tourId)
            .OrderBy(s => s.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<TourStop> AddStopAsync(Guid tourId, Guid poiId, int sequence, string? nextStopHint = null, CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours.FindAsync(new object[] { tourId }, cancellationToken: cancellationToken);
        if (tour is null)
        {
            throw new KeyNotFoundException($"Tour with ID {tourId} not found.");
        }

        var poi = await _dbContext.Pois.FindAsync(new object[] { poiId }, cancellationToken: cancellationToken);
        if (poi is null)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        var existingStop = await _dbContext.TourStops
            .FirstOrDefaultAsync(s => s.TourId == tourId && s.Sequence == sequence, cancellationToken);

        if (existingStop is not null)
        {
            throw new InvalidOperationException($"Tour {tourId} already has a stop at sequence {sequence}.");
        }

        var stop = new TourStop
        {
            TourId = tourId,
            PoiId = poiId,
            Sequence = sequence,
            NextStopHint = nextStopHint
        };

        _dbContext.TourStops.Add(stop);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stop;
    }

    public async Task RemoveStopAsync(Guid tourStopId, CancellationToken cancellationToken = default)
    {
        var stop = await _dbContext.TourStops.FindAsync(new object[] { tourStopId }, cancellationToken: cancellationToken);
        if (stop is null)
        {
            return;
        }

        _dbContext.TourStops.Remove(stop);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TourStop> UpdateStopAsync(
        Guid tourStopId,
        Guid poiId,
        int sequence,
        string? nextStopHint = null,
        CancellationToken cancellationToken = default)
    {
        var stop = await _dbContext.TourStops.FindAsync(new object[] { tourStopId }, cancellationToken: cancellationToken);
        if (stop is null)
        {
            throw new KeyNotFoundException($"Tour stop with ID {tourStopId} not found.");
        }

        var poiExists = await _dbContext.Pois.AnyAsync(x => x.Id == poiId, cancellationToken);
        if (!poiExists)
        {
            throw new KeyNotFoundException($"POI with ID {poiId} not found.");
        }

        var conflict = await _dbContext.TourStops
            .AnyAsync(x => x.TourId == stop.TourId && x.Sequence == sequence && x.Id != stop.Id, cancellationToken);

        if (conflict)
        {
            throw new InvalidOperationException($"Tour already has a stop at sequence {sequence}.");
        }

        stop.PoiId = poiId;
        stop.Sequence = sequence;
        stop.NextStopHint = nextStopHint;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return stop;
    }

    public async Task ReorderStopsAsync(Guid tourId, List<Guid> tourStopIdsInOrder, CancellationToken cancellationToken = default)
    {
        var stops = await _dbContext.TourStops
            .Where(s => s.TourId == tourId)
            .ToListAsync(cancellationToken);

        for (int i = 0; i < tourStopIdsInOrder.Count; i++)
        {
            var stop = stops.FirstOrDefault(s => s.Id == tourStopIdsInOrder[i]);
            if (stop is not null)
            {
                stop.Sequence = i + 1;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TourStop?> GetNextStopAsync(Guid tourId, int currentSequence, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TourStops
            .Include(s => s.Poi)
            .Where(s => s.TourId == tourId && s.Sequence > currentSequence)
            .OrderBy(s => s.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TourStop?> GetPreviousStopAsync(Guid tourId, int currentSequence, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TourStops
            .Include(s => s.Poi)
            .Where(s => s.TourId == tourId && s.Sequence < currentSequence)
            .OrderByDescending(s => s.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
