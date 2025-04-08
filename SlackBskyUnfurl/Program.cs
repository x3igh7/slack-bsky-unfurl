using SlackBskyUnfurl.Services;
using SlackBskyUnfurl.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSnapshotCollector();

builder.Services.AddScoped<ISlackService, SlackService>();
builder.Services.AddSingleton<IBlueSkyService, BlueSkyService>();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options => options.AddDefaultPolicy(policyBuilder => {
    policyBuilder
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();