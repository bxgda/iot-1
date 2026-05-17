using Microsoft.EntityFrameworkCore;
using rest_service.Data;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL sa EF Core — connection pool povecan za vece opterecenje
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection") + ";Maximum Pool Size=200;Minimum Pool Size=10;",
        npgsql => npgsql.EnableRetryOnFailure(3)
    )
);

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "IoT REST API",
        Version = "v1",
        Description = "REST mikroservis za IoT senzorske podatke — Smart Home dataset"
    });
});

var app = builder.Build();

// Swagger UI uvek dostupan (ne samo u Development modu)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT REST API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
