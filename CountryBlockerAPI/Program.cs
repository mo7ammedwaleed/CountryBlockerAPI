using CountryBlockerAPI.BackgroundServices;
using CountryBlockerAPI.Repository;
using CountryBlockerAPI.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Country Blocker API",
        Version = "v1",
        Description = "Manage blocked countries and validate IP addresses using third-party geolocation."
    });
});

builder.Services.AddSingleton<ICountryRepository, CountryRepository>();

builder.Services.AddHttpClient<IGeoLocationService, GeoLocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<ICountryService, CountryService>();

builder.Services.AddHostedService<TemporalBlockCleanupService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Country Blocker API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();