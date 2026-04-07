using BLL.Features.Releases.Commands;
using BLL.Services;
using DAL.Context;
using MediatR;
using DAL.Entities;
using DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IScriptConflictSyncService, ScriptConflictSyncService>();
builder.Services.AddScoped<IScriptWorkflowService, ScriptWorkflowService>();
builder.Services.AddMediatR(typeof(CreateReleaseHandle).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MyContext>();
        db.Database.Migrate();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Name = "Yerel geliştirici",
                    Email = "developer@localhost",
                    Role = UserRole.Developer,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new User
                {
                    Name = "Yerel yönetici",
                    Email = "admin@localhost",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new User
                {
                    Name = "Yerel testçi",
                    Email = "tester@localhost",
                    Role = UserRole.Tester,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            await db.SaveChangesAsync();
        }
        else
        {
            var hasAdmin = await db.Users.AnyAsync(u =>
                !u.IsDeleted && u.Role == UserRole.Admin);
            var adminEmailUsed = await db.Users.AnyAsync(u => !u.IsDeleted && u.Email == "admin@localhost");
            if (!hasAdmin && !adminEmailUsed)
            {
                db.Users.Add(new User
                {
                    Name = "Yerel yönetici",
                    Email = "admin@localhost",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await db.SaveChangesAsync();
            }
        }

        var pwdHasher = new PasswordHasher<User>();
        var allUsers = await db.Users.Where(u => !u.IsDeleted).ToListAsync();
        foreach (var u in allUsers)
        {
            var hasCred = await db.UserCredentials.AnyAsync(c => c.UserId == u.Id && !c.IsDeleted);
            if (hasCred)
                continue;
            db.UserCredentials.Add(new UserCredential
            {
                UserId = u.Id,
                UserName = u.Email,
                PasswordHash = pwdHasher.HashPassword(u, "Dev123!"),
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();