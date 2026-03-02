using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    /// <summary>
    /// Repository for TargetKeyword data access (pro.TargetKeywords table)
    /// </summary>
    public class TargetKeywordRepository : ITargetKeywordRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TargetKeywordRepository> _logger;

        public TargetKeywordRepository(
            ApplicationDbContext context,
            ILogger<TargetKeywordRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetByTargetAsync(int targetId, string targetType)
        {
            try
            {
                var items = await _context.TargetKeywords
                    .AsNoTracking()
                    .Where(t => t.TargetId == targetId && t.TargetType == targetType)
                    .ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Data = items.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TargetKeywords for targetId: {TargetId}, targetType: {TargetType}", targetId, targetType);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> CreateAsync(TargetKeyword item)
        {
            try
            {
                var exists = await _context.TargetKeywords
                    .AnyAsync(t => t.TargetId == item.TargetId && t.TargetType == item.TargetType && t.KeywordId == item.KeywordId);

                if (exists)
                {
                    return new ResultOptions
                    {
                        Success = false,
                        Message = "This keyword is already linked to the target",
                        Status = 409
                    };
                }

                _context.TargetKeywords.Add(item);
                await _context.SaveChangesAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Keyword linked successfully",
                    Object = item,
                    Status = 201
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating TargetKeyword");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> DeleteAsync(int id)
        {
            try
            {
                var item = await _context.TargetKeywords.FindAsync(id);
                if (item == null)
                {
                    return new ResultOptions { Success = false, Message = $"TargetKeyword with ID {id} not found", Status = 404 };
                }

                _context.TargetKeywords.Remove(item);
                await _context.SaveChangesAsync();

                return new ResultOptions { Success = true, Message = "Keyword unlinked successfully", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TargetKeyword with ID: {Id}", id);
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
