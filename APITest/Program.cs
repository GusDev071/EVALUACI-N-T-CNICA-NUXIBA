using APITest.Data;
using APITest.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options
        .UseSqlServer(connectionString)
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

// ===== Ejercicio 1: API RESTful (CRUD de logins) =====
app.MapGet("/logins", async (AppDbContext context) =>
{
    var logins = await context.Logins.AsNoTracking().ToListAsync();
    return Results.Ok(logins);
});

app.MapPost("/logins", async (Login login, AppDbContext context) =>
{
    if (login.Fecha == default)
    {
        return Results.BadRequest("La fecha no es válida.");
    }

    if (login.TipoMov != 0 && login.TipoMov != 1)
    {
        return Results.BadRequest("TipoMov solo acepta 0 (logout) o 1 (login).");
    }

    var userExists = await context.Users.AnyAsync(u => u.UserId == login.UserId);
    if (!userExists)
    {
        return Results.BadRequest("El usuario no existe en ccUsers.");
    }

    var lastMove = await context.Logins
        .Where(l => l.UserId == login.UserId)
        .OrderByDescending(l => l.Fecha)
        .FirstOrDefaultAsync();

    if (login.TipoMov == 1 && lastMove != null && lastMove.TipoMov == 1)
    {
        return Results.BadRequest("No se puede registrar un login sin un logout anterior.");
    }

    if (login.TipoMov == 0 && lastMove == null)
    {
        return Results.BadRequest("No hay un login previo para cerrar.");
    }

    context.Logins.Add(login);
    await context.SaveChangesAsync();

    return Results.Created($"/logins/{login.LogId}", login);
});

app.MapPut("/logins/{id:int}", async (int id, Login input, AppDbContext context) =>
{
    var login = await context.Logins.FindAsync(id);
    if (login == null)
    {
        return Results.NotFound();
    }

    if (input.Fecha == default)
    {
        return Results.BadRequest("La fecha no es válida.");
    }

    if (input.TipoMov != 0 && input.TipoMov != 1)
    {
        return Results.BadRequest("TipoMov solo acepta 0 (logout) o 1 (login).");
    }

    var userExists = await context.Users.AnyAsync(u => u.UserId == input.UserId);
    if (!userExists)
    {
        return Results.BadRequest("El usuario no existe en ccUsers.");
    }

    login.UserId = input.UserId;
    login.Extension = input.Extension;
    login.TipoMov = input.TipoMov;
    login.Fecha = input.Fecha;

    await context.SaveChangesAsync();

    return Results.Ok(login);
});

app.MapDelete("/logins/{id:int}", async (int id, AppDbContext context) =>
{
    var login = await context.Logins.FindAsync(id);
    if (login == null)
    {
        return Results.NotFound(new { message = "Registro no encontrado", id });
    }

    context.Logins.Remove(login);
    await context.SaveChangesAsync();

    return Results.Ok(new { message = "Registro eliminado correctamente", id });
});

// ===== Ejercicio 2: Consultas SQL y Optimización =====
// Implementado en README con consultas SQL para tiempos de logueo.

// ===== Ejercicio 3: Generación de CSV =====
app.MapGet("/report/csv", async (int? userId, AppDbContext context) =>
{
    var usersQuery = context.Users.AsNoTracking();
    if (userId.HasValue)
    {
        usersQuery = usersQuery.Where(u => u.UserId == userId.Value);
    }
    var users = await usersQuery.ToListAsync();

    var areas = await context.Areas.AsNoTracking().ToDictionaryAsync(a => a.IDArea, a => a.AreaName);

    var logsQuery = context.Logins.AsNoTracking()
        .OrderBy(l => l.UserId)
        .ThenBy(l => l.Fecha);
    if (userId.HasValue)
    {
        logsQuery = logsQuery.Where(l => l.UserId == userId.Value).OrderBy(l => l.UserId).ThenBy(l => l.Fecha);
    }
    var logs = await logsQuery.ToListAsync();

    var totals = new Dictionary<int, long>();
    foreach (var group in logs.GroupBy(l => l.UserId))
    {
        DateTime? open = null;
        long sum = 0;
        foreach (var e in group)
        {
            if (e.TipoMov == 1)
            {
                if (open == null) open = e.Fecha;
            }
            else if (e.TipoMov == 0 && open != null)
            {
                var diff = (long)(e.Fecha - open.Value).TotalSeconds;
                if (diff > 0) sum += diff;
                open = null;
            }
        }
        if (sum > 0) totals[group.Key] = sum;
    }

    var sb = new StringBuilder();
    sb.AppendLine("Login,NombreCompleto,Area,TotalHoras");
    foreach (var kv in totals)
    {
        var user = users.FirstOrDefault(u => u.UserId == kv.Key);
        if (user == null) continue;
        var hours = Math.Round(kv.Value / 3600.0, 2);
        var loginName = user.Login ?? "";
        var fullName = string.Join(" ",
            new[] { user.Nombres, user.ApellidoPaterno, user.ApellidoMaterno }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        var areaName = user.IDArea.HasValue && areas.TryGetValue(user.IDArea.Value, out var aName)
            ? aName
            : string.Empty;

        var safeLogin = (loginName ?? string.Empty).Replace(",", " ");
        var safeFullName = fullName.Replace(",", " ");
        var safeArea = areaName.Replace(",", " ");

        sb.AppendLine($"{safeLogin},{safeFullName},{safeArea},{hours}");
    }

    // Si se pidió un usuario específico y no tuvo pares login/logout, igual generamos fila con 0
    if (userId.HasValue && sb.ToString().Split('\n').Length <= 2)
    {
        var singleUser = users.FirstOrDefault();
        if (singleUser != null)
        {
            var fullName = string.Join(" ",
                new[] { singleUser.Nombres, singleUser.ApellidoPaterno, singleUser.ApellidoMaterno }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            var areaName = singleUser.IDArea.HasValue && areas.TryGetValue(singleUser.IDArea.Value, out var aName)
                ? aName
                : string.Empty;
            var safeLogin = (singleUser.Login ?? string.Empty).Replace(",", " ");
            var safeFullName = fullName.Replace(",", " ");
            var safeArea = areaName.Replace(",", " ");
            sb.AppendLine($"{safeLogin},{safeFullName},{safeArea},0");
        }
    }

    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
    var fileName = userId.HasValue ? $"reporte_horas_user_{userId.Value}.csv" : "reporte_horas.csv";
    return Results.File(bytes, "text/csv", fileName);
});

app.MapGet("/report/csv/{userId:int}", async (int userId, AppDbContext context) =>
{
    var users = await context.Users.AsNoTracking().Where(u => u.UserId == userId).ToListAsync();
    var areas = await context.Areas.AsNoTracking().ToDictionaryAsync(a => a.IDArea, a => a.AreaName);
    var logs = await context.Logins.AsNoTracking()
        .Where(l => l.UserId == userId)
        .OrderBy(l => l.UserId)
        .ThenBy(l => l.Fecha)
        .ToListAsync();

    var totals = new Dictionary<int, long>();
    foreach (var group in logs.GroupBy(l => l.UserId))
    {
        DateTime? open = null;
        long sum = 0;
        foreach (var e in group)
        {
            if (e.TipoMov == 1)
            {
                if (open == null) open = e.Fecha;
            }
            else if (e.TipoMov == 0 && open != null)
            {
                var diff = (long)(e.Fecha - open.Value).TotalSeconds;
                if (diff > 0) sum += diff;
                open = null;
            }
        }
        if (sum > 0) totals[group.Key] = sum;
    }

    var sb = new StringBuilder();
    sb.AppendLine("Login,NombreCompleto,Area,TotalHoras");
    foreach (var kv in totals)
    {
        var user = users.FirstOrDefault(u => u.UserId == kv.Key);
        if (user == null) continue;
        var hours = Math.Round(kv.Value / 3600.0, 2);
        var loginName = user.Login ?? "";
        var fullName = string.Join(" ",
            new[] { user.Nombres, user.ApellidoPaterno, user.ApellidoMaterno }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        var areaName = user.IDArea.HasValue && areas.TryGetValue(user.IDArea.Value, out var aName)
            ? aName
            : string.Empty;

        var safeLogin = (loginName ?? string.Empty).Replace(",", " ");
        var safeFullName = fullName.Replace(",", " ");
        var safeArea = areaName.Replace(",", " ");

        sb.AppendLine($"{safeLogin},{safeFullName},{safeArea},{hours}");
    }

    if (sb.ToString().Split('\n').Length <= 2)
    {
        var singleUser = users.FirstOrDefault();
        if (singleUser != null)
        {
            var fullName = string.Join(" ",
                new[] { singleUser.Nombres, singleUser.ApellidoPaterno, singleUser.ApellidoMaterno }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            var areaName = singleUser.IDArea.HasValue && areas.TryGetValue(singleUser.IDArea.Value, out var aName)
                ? aName
                : string.Empty;
            var safeLogin = (singleUser.Login ?? string.Empty).Replace(",", " ");
            var safeFullName = fullName.Replace(",", " ");
            var safeArea = areaName.Replace(",", " ");
            sb.AppendLine($"{safeLogin},{safeFullName},{safeArea},0");
        }
    }

    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
    var fileName = $"reporte_horas_user_{userId}.csv";
    return Results.File(bytes, "text/csv", fileName);
});
app.Run();
