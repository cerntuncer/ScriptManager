namespace DAL.Enums;

/// <summary>
/// Uyarı kaydı. Uygulama mantığında artık script durumu veya Hazır akışı buna göre engellenmez.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>Eski veritabanı satırları (varsayılan 0). Yeni kayıtlar <see cref="ReviewAdvised"/>.</summary>
    Blocking = 0,

    /// <summary>Bilgilendirme — kontrol ediniz.</summary>
    ReviewAdvised = 1,
}
