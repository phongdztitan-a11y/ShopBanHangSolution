using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;

var builder = WebApplication.CreateBuilder(args);

// --- BƯỚC 1: ĐĂNG KÝ CONTROLLERS VÀ CẤU HÌNH JSON ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- BƯỚC 2: DATABASE (Render: DATABASE_URL dạng postgres://, thường không có :5432 → Uri.Port = -1) ---
var connectionString = ResolvePostgresConnectionString(builder.Configuration);

builder.Services.AddDbContext<ServerDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

// ===== THÊM ĐOẠN NÀY =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
    db.Database.Migrate();
}
// ==========================


if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string ResolvePostgresConnectionString(IConfiguration configuration)
{
    var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
              ?? configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(raw))
        throw new InvalidOperationException(
            "Thiếu DATABASE_URL (Render) hoặc ConnectionStrings:DefaultConnection.");

    if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return raw;

    var databaseUri = new Uri(raw);
    var userParts = databaseUri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userParts[0]);
    var password = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : string.Empty;
    var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
    var database = databaseUri.AbsolutePath.TrimStart('/');

    return
        $"Host={databaseUri.Host};" +
        $"Port={port};" +
        $"Database={database};" +
        $"Username={username};" +
        $"Password={password};" +
        "SSL Mode=Require;Trust Server Certificate=true";
}