using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;
using SuperAppServices.Services.Keywords;

namespace SuperAppServices.Services.LifeLog
{
    /// <summary>
    /// Service for LifeLog feature (tracks + logs)
    /// </summary>
    public class LifeLogService : Interfaces.ILifeLogService
    {
        private readonly ILifeLogRepository _repo;
        private readonly ILogger<LifeLogService> _logger;
        private readonly KeywordServiceV2 _keywordService;

        public LifeLogService(ILifeLogRepository repo, ILogger<LifeLogService> logger, KeywordServiceV2 keywordService)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keywordService = keywordService ?? throw new ArgumentNullException(nameof(keywordService));
        }

        // ─── TRACKS ────────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetTracksAsync(LifeLogTrackFilterOptions filterOptions)
        {
            try
            {
                return await _repo.GetTracksAsync(filterOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracks");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetTrackByIdAsync(int id, int userId)
            => await GetTracksAsync(new LifeLogTrackFilterOptions { UserId = userId, Ids = new List<int> { id } });

        public async Task<ResultOptions> UpsertTracksAsync(List<UpsertLifeLogTrackRequest> requests)
        {
            try
            {
                _logger.LogInformation("Service.UpsertTracksAsync: {Count} tracks, ids=[{Ids}]",
                    requests.Count, string.Join(",", requests.Select(r => r.Id)));

                var tracks = requests.Select(r => new LifeLogTrack
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Name = r.Name,
                    Emoji = r.Emoji,
                    Description = r.Description,
                    IsSensitive = r.IsSensitive,
                    Color = r.Color,
                    DeletedAt = r.DeletedAt
                }).ToList();

                var result = await _repo.UpsertTracksAsync(tracks);

                if (result.Success)
                {
                    foreach (var (track, req) in tracks.Zip(requests))
                    {
                        if (track.Id > 0)
                            try { await _keywordService.SyncTrackKeywordAsync(track.Id, req.UserId); }
                            catch (Exception ex) { _logger.LogError(ex, "Error syncing keyword for track {Id}", track.Id); }
                    }
                }

                _logger.LogInformation("Service.UpsertTracksAsync done: success={Success} message={Message}",
                    result.Success, result.Message);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service.UpsertTracksAsync exception: {Message}", ex.Message);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        // ─── LOGS ──────────────────────────────────────────────────────────────

        public async Task<ResultOptions> GetLogsAsync(LifeLogLogFilterOptions filterOptions)
        {
            try
            {
                return await _repo.GetLogsAsync(filterOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> GetLogByIdAsync(int id, int userId)
            => await GetLogsAsync(new LifeLogLogFilterOptions { UserId = userId, Ids = new List<int> { id } });

        public async Task<ResultOptions> UpsertLogsAsync(List<UpsertLifeLogLogRequest> requests)
        {
            try
            {
                _logger.LogInformation("Service.UpsertLogsAsync: {Count} logs, ids=[{Ids}]",
                    requests.Count, string.Join(",", requests.Select(r => r.Id)));

                var logs = requests.Select(r => new LifeLogLog
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Type = r.Type,
                    TrackId = r.TrackId,
                    Title = r.Title,
                    Description = r.Description,
                    IsSensitive = r.IsSensitive,
                    Location = r.Location,
                    OccurAt = r.OccurAt,
                    DeletedAt = r.DeletedAt
                }).ToList();

                var result = await _repo.UpsertLogsAsync(logs);

                if (result.Success)
                {
                    foreach (var (log, req) in logs.Zip(requests))
                    {
                        if (log.Id > 0)
                            try { await _keywordService.SyncLogKeywordAsync(log.Id, req.UserId); }
                            catch (Exception ex) { _logger.LogError(ex, "Error syncing keyword for log {Id}", log.Id); }
                    }
                }

                _logger.LogInformation("Service.UpsertLogsAsync done: success={Success} message={Message}",
                    result.Success, result.Message);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service.UpsertLogsAsync exception: {Message}", ex.Message);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
