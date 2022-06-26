using DataAccess;
using RR_Scraper;
using RR_Stats;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IDataAccess, DataAccess.DataAccess>();
builder.Services.AddSingleton(x =>
{
    var config = x.GetRequiredService<IConfiguration>();
    var scraper = new Scraper(config.GetSection("RRApiKey").Value, config.GetConnectionString("default"));
    return new DBUpdater(config.GetConnectionString("default"), Enumerable.Range(1, 10), scraper);
});
builder.Services.AddHostedService(provider => provider.GetService<DBUpdater>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();