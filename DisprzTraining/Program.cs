using DisprzTraining.Models;
using DisprzTraining.DataAccess;
using DisprzTraining.Business;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository & Service
builder.Services.AddScoped<AppointmentRepository>();
builder.Services.AddScoped<AppointmentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000") // React dev server
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Use CORS before MapControllers
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
