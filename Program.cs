using GestionStages.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;  

var builder = WebApplication.CreateBuilder(args);

// Configuration de QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuration de la base métier (Stages)
builder.Services.AddDbContext<StagesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StagesDb")));

// Configuration de la base d'authentification (Identity)
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDb")));

// Configuration d'Identity avec les pages par défaut (Login, Register, etc.)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Je rends le mot de passe plus simple pour les tests (comme dans le cours)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>() // J'active les rôles
.AddEntityFrameworkStores<AuthDbContext>();

// J'ajoute les Razor Pages (obligatoire pour les pages Identity scaffoldées)
builder.Services.AddRazorPages();

// J'enregistre mon seeder
builder.Services.AddScoped<IdentitySeeder>();

var app = builder.Build();

// ====================== SEEDING DES RÔLES ET ADMIN ======================
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
    await seeder.SeedAsync();
}
// ======================================================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Très important : avant UseAuthorization
app.UseAuthorization();

app.MapRazorPages(); // Pour les pages Identity (Login, Register, etc.)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();