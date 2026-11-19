using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins.Reflect;
using SuperAppModels.DTOs;
using SuperAppModels.Models.Reflect;

namespace SuperAppDataRepositories.Repositories.Reflect
{
    public class ReflectPlanRepository : IReflectPlanRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReflectPlanRepository> _logger;

        public ReflectPlanRepository(ApplicationDbContext context, ILogger<ReflectPlanRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ResultOptions> GetItemsAsync(int userId, DateTime? from, DateTime? to)
        {
            try
            {
                var query = _context.ReflectPlanItems.AsNoTracking()
                    .Where(r => r.UserId == userId && r.DeletedAt == null);
                if (from.HasValue) query = query.Where(r => r.PlanDate >= from.Value.Date);
                if (to.HasValue) query = query.Where(r => r.PlanDate <= to.Value.Date);

                var items = await query.OrderBy(r => r.PlanDate).ThenBy(r => r.SortOrder).ThenBy(r => r.CreatedAt).ToListAsync();

                return new ResultOptions
                {
                    Success = true,
                    Message = "Reflect plan items retrieved successfully",
                    Data = items.Cast<object>().ToList(),
                    Status = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reflect plan items");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> UpsertItemAsync(ReflectPlanItem item)
        {
            try
            {
                var existing = await _context.ReflectPlanItems.FirstOrDefaultAsync(r => r.Id == item.Id);

                if (existing != null && existing.UserId != item.UserId)
                    return new ResultOptions { Success = false, Message = "Reflect plan item not found", Status = 404 };

                if (existing == null)
                {
                    item.CreatedAt = DateTime.UtcNow;
                    item.UpdatedAt = item.CreatedAt;
                    _context.ReflectPlanItems.Add(item);
                    existing = item;
                }
                else
                {
                    existing.PlanDate = item.PlanDate;
                    existing.Title = item.Title;
                    existing.Status = item.Status;
                    existing.SortOrder = item.SortOrder;
                    existing.Note = item.Note;
                    existing.Extra = item.Extra;
                    existing.DeletedAt = null;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Message = "Reflect plan item saved", Object = existing, Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving reflect plan item");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }

        public async Task<ResultOptions> DeleteItemAsync(int userId, string id)
        {
            try
            {
                var existing = await _context.ReflectPlanItems
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId && r.DeletedAt == null);
                if (existing == null)
                    return new ResultOptions { Success = true, Message = "Reflect plan item already gone", Status = 200 };

                existing.DeletedAt = DateTime.UtcNow;
                existing.UpdatedAt = existing.DeletedAt.Value;
                await _context.SaveChangesAsync();
                return new ResultOptions { Success = true, Message = "Reflect plan item deleted", Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reflect plan item");
                return new ResultOptions { Success = false, Message = ex.Message, Status = 500 };
            }
        }
    }
}
