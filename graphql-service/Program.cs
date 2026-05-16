using Microsoft.EntityFrameworkCore;
using graphql_service.Data;
using graphql_service.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL sa EF Core — pooled factory za Hot Chocolate
builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(3)
    )
);

// GraphQL server sa Hot Chocolate
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.MapGraphQL();

app.Run();
