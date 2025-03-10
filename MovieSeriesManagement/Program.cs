using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieSeriesManagement.Data;
using MovieSeriesManagement.Data.Repositories;
using MovieSeriesManagement.Models.Entities;
using MovieSeriesManagement.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Modifica la configuración de Identity para ser más permisiva con los requisitos
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;

    // Configuración de contraseña menos estricta para desarrollo
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Configuración de usuario
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<IViewingHistoryRepository, ViewingHistoryRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();

// Register services
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IViewingHistoryService, ViewingHistoryService>();

// Agregar el servicio de email
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Datos del administrador
    string adminEmail = "admin@movieflix.com";
    string adminPassword = "Admin123!";

    // Comprobar si el administrador ya existe
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        // Crear el usuario administrador
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "System",
            ProfilePictureUrl = "/images/profiles/default-profile.jpg"
        };

        var result = await userManager.CreateAsync(admin, adminPassword);

        if (result.Succeeded)
        {
            // Asignar el rol de administrador
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

// Añadir después de la configuración de la aplicación, justo antes de app.Run():

// Asegurarse de que existan las carpetas necesarias
var webRootPath = app.Environment.WebRootPath;
var profileImagesPath = Path.Combine(webRootPath, "images", "profiles");
var contentImagesPath = Path.Combine(webRootPath, "images", "content");

if (!Directory.Exists(profileImagesPath))
{
    Directory.CreateDirectory(profileImagesPath);
}

if (!Directory.Exists(contentImagesPath))
{
    Directory.CreateDirectory(contentImagesPath);
}

// Copiar una imagen predeterminada si no existe
var defaultProfileImagePath = Path.Combine(profileImagesPath, "default-profile.jpg");
if (!System.IO.File.Exists(defaultProfileImagePath))
{
    // Crear una imagen predeterminada simple
    using (var bitmap = new System.Drawing.Bitmap(200, 200))
    {
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Gray);
            using (var font = new System.Drawing.Font("Arial", 40))
            {
                graphics.DrawString("?", font, System.Drawing.Brushes.White, new System.Drawing.PointF(80, 60));
            }
        }
        bitmap.Save(defaultProfileImagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
    }
}

app.Run();

