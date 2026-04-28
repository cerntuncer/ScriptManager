using BLL.Features.Releases.Commands;
using BLL.Services;
using DAL.Context;
using DAL.Repositories.Base;
using DAL.Repositories.Concretes;
using DAL.Repositories.Interfaces;
using MediatR;
using DAL.Entities;
using DAL.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Development: her dotnet run / yeniden başlatmada farklı çerez adı → tarayıcıdaki eski oturum kullanılmaz, giriş ekranı gelir.
var authCookieName = builder.Environment.IsDevelopment()
    ? $"ScriptManager.Auth.{Guid.NewGuid():N}"
    : "ScriptManager.Auth";

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    // Tüm MVC aksiyonları oturum ister; yalnızca [AllowAnonymous] işaretli olanlar (Login, Error vb.) açılır.
    options.Filters.Add(new AuthorizeFilter(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authCookieName;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<IConflictRepository, ConflictRepository>();
builder.Services.AddScoped<IReleaseRepository, ReleaseRepository>();
builder.Services.AddScoped<IScriptRepository, ScriptRepository>();
builder.Services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IScriptConflictSyncService, ScriptConflictSyncService>();
builder.Services.AddSingleton<ISqlScriptSyntaxValidator>(_ =>
    new SqlScriptSyntaxValidator(_.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ITargetEnvironmentRepository, TargetEnvironmentRepository>();
builder.Services.AddScoped<ISchemaValidationService, SchemaValidationService>();
builder.Services.AddMediatR(typeof(CreateReleaseHandle).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MyContext>();
        db.Database.Migrate();

        // Yerel seed: Ceren test hesapları (yalnızca Development). Şifre her açılışta bu üç kullanıcı için 123456 olur.
        const string seedPassword = "123456";
        var seedAccounts = new (string Email, string Name, UserRole Role)[]
        {
            ("ceren1@localhost", "Ceren (testçi)", UserRole.Tester),
            ("ceren2@localhost", "Ceren (geliştirici 2)", UserRole.Developer),
            ("ceren3@localhost", "Ceren (geliştirici)", UserRole.Developer),
        };

        foreach (var (email, name, role) in seedAccounts)
        {
            if (!await db.Users.AnyAsync(u => !u.IsDeleted && u.Email == email))
            {
                db.Users.Add(new User
                {
                    Name = name,
                    Email = email,
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
        }

        await db.SaveChangesAsync();

        var pwdHasher = new PasswordHasher<User>();
        foreach (var (email, _, _) in seedAccounts)
        {
            var u = await db.Users.FirstOrDefaultAsync(x => !x.IsDeleted && x.Email == email);
            if (u == null) continue;

            var cred = await db.UserCredentials.FirstOrDefaultAsync(c => c.UserId == u.Id && !c.IsDeleted);
            var hash = pwdHasher.HashPassword(u, seedPassword);
            if (cred == null)
            {
                db.UserCredentials.Add(new UserCredential
                {
                    UserId = u.Id,
                    UserName = u.Email,
                    PasswordHash = hash,
                    LockoutEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
            else
            {
                cred.UserName = u.Email;
                cred.PasswordHash = hash;
                cred.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Seed dışındaki kullanıcıların credential'ı yoksa varsayılan şifre ata (eski davranışa yakın)
        var seedEmails = new HashSet<string>(seedAccounts.Select(a => a.Email), StringComparer.OrdinalIgnoreCase);
        foreach (var u in await db.Users.Where(x => !x.IsDeleted).ToListAsync())
        {
            if (seedEmails.Contains(u.Email)) continue;
            var hasCred = await db.UserCredentials.AnyAsync(c => c.UserId == u.Id && !c.IsDeleted);
            if (hasCred) continue;
            db.UserCredentials.Add(new UserCredential
            {
                UserId = u.Id,
                UserName = u.Email,
                PasswordHash = pwdHasher.HashPassword(u, seedPassword),
                LockoutEnabled = false,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Development'ta sadece http://localhost:5270 kullanılıyor; HTTPS yönlendirmesi boş sayfa yaratabilir.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// / → Dashboard/Index; anonim kullanıcı FallbackPolicy ile /Account/Login'e gider.
// Kökü ayrıca Login'e bağlamıyoruz: RedirectToAction(Dashboard) bazen / üretir ve Login ile sonsuz yönlendirme oluşur.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard", action = "Index" });

app.Run();