using ChessOnline.Application.Interfaces;
using ChessOnline.Application.Validators;
using ChessOnline.Domain.Entities;
using ChessOnline.Infrastructure.Persistence;
using ChessOnline.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// 🔹 FluentValidation
builder.Services.AddFluentValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// 🔹 Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, b =>
        b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

// 🔹 Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Tuỳ chỉnh password (cho dễ test)
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

//builder.Services.AddIdentityCore<User>()
//    .AddEntityFrameworkStores<AppDbContext>()
//    .AddSignInManager<SignInManager<User>>();

// 🔹 Services
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
    //.WithStaticAssets();

app.Run();