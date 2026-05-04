using SuperAppModels.DTOs.Requests;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Ins
{
    public interface IWikiRepository
    {
        // ── Read ──────────────────────────────────────────────────────────────
        Task<List<WikiKeyword>> GetAllKeywordsAsync(int userId);
        Task<List<WikiInfo>> GetAllInfosAsync(int userId);
        Task<WikiKeyword?> GetKeywordByIdAsync(int id, int userId);
        Task<WikiInfo?> GetInfoByIdAsync(int id, int userId);

        // ── Keywords ──────────────────────────────────────────────────────────
        Task<WikiKeyword> CreateKeywordAsync(string name, int userId);
        Task<WikiKeyword?> UpdateKeywordMetaAsync(int id, int userId, string? name, string? iconBase64, List<string>? synonyms);
        Task<bool> SoftDeleteKeywordAsync(int id, int userId);
        Task<bool> SavePinnedPositionAsync(int keywordId, int userId, double x, double y);

        // ── Infos ─────────────────────────────────────────────────────────────
        Task<WikiInfo> CreateInfoAsync(WikiUpsertInfoRequest request);
        Task<WikiInfo?> UpdateInfoAsync(WikiUpsertInfoRequest request);
        Task<bool> SoftDeleteInfoAsync(int id, int userId);
        Task<bool> RestoreInfoAsync(int id, int userId);

        // ── Manual link management ────────────────────────────────────────────
        Task AddInfoLinksAsync(int keywordId, List<int> infoIds, int userId);
        Task RemoveInfoLinksAsync(int keywordId, List<int> infoIds);

        // ── Full rescan (manual trigger only) ─────────────────────────────────
        Task<int> RescanAllLinksAsync(int userId);

        // ── Interactions ──────────────────────────────────────────────────────
        Task IncrementKeywordViewAsync(int keywordId, int userId);
        Task IncrementKeywordReadAsync(int keywordId, int userId);
        Task IncrementKeywordEditAsync(int keywordId, int userId);
    }
}
