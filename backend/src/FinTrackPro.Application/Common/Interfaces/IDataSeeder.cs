namespace FinTrackPro.Application.Common.Interfaces;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
