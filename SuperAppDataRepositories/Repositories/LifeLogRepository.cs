using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for LifeLog data access (tracks and logs)
    /// </summary>
    public class LifeLogRepository : ILifeLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LifeLogRepository> _logger;

        public LifeLogRepository(
            ApplicationDbContext context,
            ILogger<LifeLogRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ─── TRACKS ────────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetTracksAsync(LifeLogTrackFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting lifelog tracks for userId: {UserId}", filterOptions.UserId);

                var query = _context.LifeLogTracks.AsNoTracking()
                    .Where(t => t.UserId == filterOptions.UserId);

                if (filterOptions.Ids?.Count > 0)
                    query = query.Where(t => filterOptions.Ids.Contains(t.Id));

                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                    query = query.Where(t => t.Name.Contains(filterOptions.SearchText) ||
                                            (t.Description != null && t.Description.Contains(filterOptions.SearchText)));

                if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                {
                    if (filterOptions.DeletedAt == "null")
                        query = query.Where(t => t.DeletedAt == null);
                    else if (filterOptions.DeletedAt == "notNull")
                        query = query.Where(t => t.DeletedAt != null);
                }

                var tracks = await query.OrderBy(t => t.Name).ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Tracks retrieved successfully",
                    Data = tracks.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lifelog tracks");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertTracksAsync(List<LifeLogTrack> tracks)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var idsToUpdate = tracks.Where(t => t.Id > 0).Select(t => t.Id).ToList();
                    var existingDict = await _context.LifeLogTracks
                        .Where(t => idsToUpdate.Contains(t.Id))
                        .ToDictionaryAsync(t => t.Id, t => t);

                    var upserted = new List<LifeLogTrack>();

                    foreach (var track in tracks)
                    {
                        if (track.Id > 0 && existingDict.TryGetValue(track.Id, out var existing))
                        {
                            existing.Name = track.Name;
                            existing.Emoji = track.Emoji;
                            existing.Description = track.Description;
                            existing.IsSensitive = track.IsSensitive;
                            existing.Color = track.Color;
                            existing.DeletedAt = track.DeletedAt;
                            existing.UpdatedAt = DateTime.Now;
                            upserted.Add(existing);
                        }
                        else
                        {
                            track.CreatedAt = DateTime.Now;
                            track.UpdatedAt = DateTime.Now;
                            track.DeletedAt = null;
                            _context.LifeLogTracks.Add(track);
                            upserted.Add(track);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully upserted {tracks.Count} tracks",
                        Data = upserted.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error upserting tracks - rolled back");
                    return new ResultOptions { Success = false, Message = ex.Message + " - rolled back", Status = 500 };
                }
            });
        }

        // ─── LOGS ──────────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetLogsAsync(LifeLogLogFilterOptions filterOptions)
        {
            try
            {
                _logger.LogInformation("Getting lifelog logs for userId: {UserId}", filterOptions.UserId);

                var query = _context.LifeLogLogs.AsNoTracking()
                    .Where(l => l.UserId == filterOptions.UserId);

                if (filterOptions.Ids?.Count > 0)
                    query = query.Where(l => filterOptions.Ids.Contains(l.Id));

                if (filterOptions.TrackId.HasValue)
                    query = query.Where(l => l.TrackId == filterOptions.TrackId.Value);

                if (filterOptions.Types?.Count > 0)
                    query = query.Where(l => filterOptions.Types.Contains(l.Type));

                if (!string.IsNullOrWhiteSpace(filterOptions.SearchText))
                    query = query.Where(l => (l.Title != null && l.Title.Contains(filterOptions.SearchText)) ||
                                            (l.Description != null && l.Description.Contains(filterOptions.SearchText)));

                if (filterOptions.CreatedFrom.HasValue)
                    query = query.Where(l => l.CreatedAt >= filterOptions.CreatedFrom.Value);

                if (filterOptions.CreatedTo.HasValue)
                    query = query.Where(l => l.CreatedAt <= filterOptions.CreatedTo.Value);

                if (!string.IsNullOrEmpty(filterOptions.DeletedAt))
                {
                    if (filterOptions.DeletedAt == "null")
                        query = query.Where(l => l.DeletedAt == null);
                    else if (filterOptions.DeletedAt == "notNull")
                        query = query.Where(l => l.DeletedAt != null);
                }

                var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Logs retrieved successfully",
                    Data = logs.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lifelog logs");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertLogsAsync(List<LifeLogLog> logs)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var idsToUpdate = logs.Where(l => l.Id > 0).Select(l => l.Id).ToList();
                    var existingDict = await _context.LifeLogLogs
                        .Where(l => idsToUpdate.Contains(l.Id))
                        .ToDictionaryAsync(l => l.Id, l => l);

                    var upserted = new List<LifeLogLog>();

                    foreach (var log in logs)
                    {
                        if (log.Id > 0 && existingDict.TryGetValue(log.Id, out var existing))
                        {
                            existing.Type = log.Type;
                            existing.TrackId = log.TrackId;
                            existing.Title = log.Title;
                            existing.Description = log.Description;
                            existing.IsSensitive = log.IsSensitive;
                            existing.Location = log.Location;
                            existing.DeletedAt = log.DeletedAt;
                            existing.UpdatedAt = DateTime.Now;
                            existing.OccurAt = log.OccurAt;
                            upserted.Add(existing);
                        }
                        else
                        {
                            log.CreatedAt = DateTime.Now;
                            log.UpdatedAt = DateTime.Now;
                            log.OccurAt = log.OccurAt ?? DateTime.Now;
                            log.DeletedAt = null;
                            _context.LifeLogLogs.Add(log);
                            upserted.Add(log);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new ResultOptions
                    {
                        Success = true,
                        Message = $"Successfully upserted {logs.Count} logs",
                        Data = upserted.Cast<object>().ToList(),
                        Status = 200
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error upserting logs - rolled back");
                    return new ResultOptions { Success = false, Message = ex.Message + " - rolled back", Status = 500 };
                }
            });
        }
    }
}
