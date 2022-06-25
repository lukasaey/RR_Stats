using DataAccess;
using RR_Scraper;
using RR_Stats;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IDataAccess, DataAccess.DataAccess>();

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

var config = app.Configuration;
var updater = new DBUpdater(config.GetSection("RRApiKey").Value, config.GetConnectionString("default"), Enumerable.Range(1, 10));

async void UpdatingLoop()
{
    await updater.UpdateDB();
    await Task.Delay(TimeSpan.FromDays(1));
}

var task = Task.Run(UpdatingLoop);
app.Run();
 