using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Data;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Repositories
{
    public class WikiRepository : IWikiRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WikiRepository> _logger;

        public WikiRepository(ApplicationDbContext context, ILogger<WikiRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger  = logger  ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Read ──────────────────────────────────────────────────────────────

        public async Task<List<WikiKeyword>> GetAllKeywordsAsync(int userId)
        {
            return await _context.WikiKeywords
                .Where(k => k.UserId == userId)
                .Include(k => k.Synonyms)
                .Include(k => k.InfoKeywords)
                .OrderBy(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WikiInfo>> GetAllInfosAsync(int userId)
        {
            return await _context.WikiInfos
                .Where(i => i.UserId == userId)
                .Include(i => i.InfoKeywords)
                .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                .ToListAsync();
        }

        public async Task<WikiKeyword?> GetKeywordByIdAsync(int id, int userId)
        {
            return await _context.WikiKeywords
                .Include(k => k.Synonyms)
                .Include(k => k.InfoKeywords)
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        }

        public async Task<WikiInfo?> GetInfoByIdAsync(int id, int userId)
        {
            return await _context.WikiInfos
                .Include(i => i.InfoKeywords)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        }

        // ── Keywords ──────────────────────────────────────────────────────────

        public async Task<WikiKeyword> CreateKeywordAsync(string name, int userId)
        {
            var entity = new WikiKeyword(name, userId);
            _context.WikiKeywords.Add(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created wiki keyword {Id} '{Name}' for user {UserId}", entity.Id, name, userId);
            return entity;
        }

        public async Task<WikiKeyword?> UpdateKeywordMetaAsync(
            int id, int userId, string? name, string? iconBase64, List<string>? synonyms)
        {
            var entity = await _context.WikiKeywords
                .Include(k => k.Synonyms)
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId && k.DeletedAt == null);

            if (entity == null) return null;

            entity.UpdateMeta(name, iconBase64);

            // Only replace synonyms if explicitly provided
            if (synonyms != null)
            {
                _context.WikiKeywordSynonyms.RemoveRange(entity.Synonyms);
                foreach (var s in synonyms.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                    entity.Synonyms.Add(new WikiKeywordSynonym { KeywordId = entity.Id, Synonym = s.Trim() });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated wiki keyword {Id} for user {UserId}", id, userId);
            return entity;
        }

        public async Task<bool> SoftDeleteKeywordAsync(int id, int userId)
        {
            // Do NOT filter by DeletedAt == null — allow idempotent deletes so that
            // keywords already soft-deleted (but still visible in the client cache)
            // don't produce a spurious 404.
            var entity = await _context.WikiKeywords
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
            if (entity == null) return false;

            if (entity.DeletedAt == null)
            {
                entity.SoftDelete();
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM [wiki].[info_keyword] WHERE [keyword_id] = {0}", id);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Soft-deleted wiki keyword {Id} for user {UserId}", id, userId);
            }

            return true;
        }

        public async Task<bool> SavePinnedPositionAsync(int keywordId, int userId, double x, double y)
        {
            var entity = await _context.WikiKeywords
                .FirstOrDefaultAsync(k => k.Id == keywordId && k.UserId == userId && k.DeletedAt == null);
            if (entity == null) return false;
            entity.SavePosition(x, y);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Infos ─────────────────────────────────────────────────────────────

        public async Task<WikiInfo> CreateInfoAsync(WikiUpsertInfoRequest request)
        {
            var entity = new WikiInfo(request.Title, request.Content, request.UserId);
            _context.WikiInfos.Add(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created wiki info {Id} for user {UserId}", entity.Id, request.UserId);
            return entity;
        }

        public async Task<WikiInfo?> UpdateInfoAsync(WikiUpsertInfoRequest request)
        {
            var entity = await _context.WikiInfos
                .FirstOrDefaultAsync(i => i.Id == request.Id && i.UserId == request.UserId);
            if (entity == null) return null;
            entity.Update(request.Title, request.Content);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated wiki info {Id} for user {UserId}", entity.Id, request.UserId);
            return entity;
        }

        public async Task<bool> SoftDeleteInfoAsync(int id, int userId)
        {
            var entity = await _context.WikiInfos
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
            if (entity == null) return false;
            entity.SoftDelete();
            await _context.SaveChangesAsync();

            // Remove all keyword links immediately — a deleted info must not contribute
            // to any keyword's info list until it is restored and rescanned.
            await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM [wiki].[info_keyword] WHERE [info_id] = {0}", id);

            _logger.LogInformation("Soft-deleted wiki info {Id}, links removed", id);
            return true;
        }

        public async Task<bool> RestoreInfoAsync(int id, int userId)
        {
            var entity = await _context.WikiInfos
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && i.DeletedAt != null);
            if (entity == null) return false;
            entity.Restore();
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Manual link management ────────────────────────────────────────────

        public async Task AddInfoLinksAsync(int keywordId, List<int> infoIds, int userId)
        {
            foreach (var infoId in infoIds.Distinct())
            {
                var exists = await _context.WikiInfoKeywords
                    .AnyAsync(ik => ik.KeywordId == keywordId && ik.InfoId == infoId);
                if (!exists)
                    _context.WikiInfoKeywords.Add(new WikiInfoKeyword { KeywordId = keywordId, InfoId = infoId });
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemoveInfoLinksAsync(int keywordId, List<int> infoIds)
        {
            var links = await _context.WikiInfoKeywords
                .Where(ik => ik.KeywordId == keywordId && infoIds.Contains(ik.InfoId))
                .ToListAsync();
            _context.WikiInfoKeywords.RemoveRange(links);
            await _context.SaveChangesAsync();
        }

        // ── Full rescan (manual trigger only) ─────────────────────────────────

        /// <summary>
        /// Full rebuild: wipe all links for this user then recompute from scratch.
        /// Returns the number of links created.
        /// </summary>
        public async Task<int> RescanAllLinksAsync(int userId)
        {
            // 1. Delete all links belonging to this user's infos
            await _context.Database.ExecuteSqlRawAsync(@"
                DELETE ik
                FROM [wiki].[info_keyword] ik
                JOIN [wiki].[info] i ON i.[id] = ik.[info_id]
                WHERE i.[user_id] = {0}",
                userId);

            // 2. Rebuild everything in one query (word-boundary via PATINDEX)
            await _context.Database.ExecuteSqlRawAsync(@"
                INSERT INTO [wiki].[info_keyword] ([info_id], [keyword_id])
                SELECT DISTINCT i.[id], k.[id]
                FROM [wiki].[info]    i
                CROSS JOIN [wiki].[keyword] k
                LEFT  JOIN [wiki].[keyword_synonym] ks ON ks.[keyword_id] = k.[id]
                WHERE i.[user_id]    = {0}
                  AND k.[user_id]    = {0}
                  AND i.[deleted_at] IS NULL
                  AND k.[deleted_at] IS NULL
                  AND (
                        PATINDEX(N'%[^A-Za-z0-9]' + k.[name]     + N'[^A-Za-z0-9]%', N' ' + i.[content] + N' ') > 0
                     OR (ks.[synonym] IS NOT NULL AND
                            PATINDEX(N'%[^A-Za-z0-9]' + ks.[synonym] + N'[^A-Za-z0-9]%', N' ' + i.[content] + N' ') > 0
                        )
                  )",
                userId);

            // Count total links for this user
            return await _context.WikiInfoKeywords
                .Join(_context.WikiInfos, ik => ik.InfoId, i => i.Id, (ik, i) => new { ik, i })
                .Where(x => x.i.UserId == userId)
                .CountAsync();
        }

        // ── Interactions ──────────────────────────────────────────────────────

        public async Task IncrementKeywordViewAsync(int keywordId, int userId)
        {
            var e = await _context.WikiKeywords.FirstOrDefaultAsync(k => k.Id == keywordId && k.UserId == userId && k.DeletedAt == null);
            if (e == null) return;
            e.IncrementView();
            await _context.SaveChangesAsync();
        }

        public async Task IncrementKeywordReadAsync(int keywordId, int userId)
        {
            var e = await _context.WikiKeywords.FirstOrDefaultAsync(k => k.Id == keywordId && k.UserId == userId && k.DeletedAt == null);
            if (e == null) return;
            e.IncrementRead();
            await _context.SaveChangesAsync();
        }

        public async Task IncrementKeywordEditAsync(int keywordId, int userId)
        {
            var e = await _context.WikiKeywords.FirstOrDefaultAsync(k => k.Id == keywordId && k.UserId == userId && k.DeletedAt == null);
            if (e == null) return;
            e.IncrementEdit();
            await _context.SaveChangesAsync();
        }
    }
}
