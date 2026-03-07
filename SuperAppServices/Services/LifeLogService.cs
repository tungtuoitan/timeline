using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppServices.Services
{
    /// <summary>
    /// Service for LifeLog feature (tracks + logs)
    /// </summary>
    public class LifeLogService : Interfaces.ILifeLogService
    {
        private readonly ILifeLogRepository _repo;
        private readonly ILogger<LifeLogService> _logger;

        public LifeLogService(ILifeLogRepository repo, ILogger<LifeLogService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public async Task<ResultOptions> UpsertTracksAsync(List<UpsertLifeLogTrackRequest> requests)
        {
            try
            {
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

                return await _repo.UpsertTracksAsync(tracks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting tracks");
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

        public async Task<ResultOptions> UpsertLogsAsync(List<UpsertLifeLogLogRequest> requests)
        {
            try
            {
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

                return await _repo.UpsertLogsAsync(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting logs");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
