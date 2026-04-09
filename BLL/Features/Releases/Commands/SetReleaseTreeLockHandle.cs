using DAL.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BLL.Features.Releases.Commands;

public class SetReleaseTreeLockHandle : IRequestHandler<SetReleaseTreeLockRequest, SetReleaseTreeLockResponse>
{
    private readonly MyContext _db;

    public SetReleaseTreeLockHandle(MyContext db) => _db = db;

    public async Task<SetReleaseTreeLockResponse> Handle(SetReleaseTreeLockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ReleaseId <= 0)
            return Fail("Geçersiz release.");

        var release = await _db.Releases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReleaseId && !r.IsDeleted, cancellationToken);
        if (release == null)
            return Fail("Release bulunamadı.");
        if (release.IsCancelled)
            return Fail("İptal edilmiş sürümde kilit değiştirilemez.");

        var batches = await _db.Batches
            .Where(b => b.ReleaseId == request.ReleaseId && !b.IsDeleted)
            .ToListAsync(cancellationToken);

        if (batches.Count == 0)
            return Fail("Bu sürümde batch yok.");

        foreach (var b in batches)
            b.IsLocked = request.Lock;

        await _db.SaveChangesAsync(cancellationToken);

        return new SetReleaseTreeLockResponse
        {
            Success = true,
            Message = request.Lock ? "Ağaç kilitlendi." : "Ağaç düzenlemeye açıldı."
        };
    }

    private static SetReleaseTreeLockResponse Fail(string msg) =>
        new() { Success = false, Message = msg };
}
