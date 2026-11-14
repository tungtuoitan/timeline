using SuperAppModels.DTOs.Reflect;

namespace SuperAppServices.Interfaces.Reflect
{
    public interface IReflectScanService
    {
        /// <summary>Reads every Reflect row of the user that is new or edited since its last scan; each row is read once.</summary>
        Task<ReflectScanResult> ScanUserAsync(int userId, CancellationToken ct);

        /// <summary>The nightly run: scans every user who has pending rows. Returns how many rows were read.</summary>
        Task<int> ScanAllAsync(CancellationToken ct);

        Task<List<ReflectSeries>> GetSeriesAsync(int userId, DateTime? from, DateTime? to);
    }
}
