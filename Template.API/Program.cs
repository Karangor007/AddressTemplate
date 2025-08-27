using Template.API.Interface;
using Template.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IHtmlRenderService, HtmlRenderService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhostFrontEnd", policy =>
    {
        policy.WithOrigins("https://localhost:7248") // your MVC frontend
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("AllowLocalhostFrontEnd");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
