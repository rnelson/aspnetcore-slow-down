using Nearform.AspNetCore.SlowDown;

var builder = WebApplication.CreateBuilder(args);

#pragma warning disable EXTEXP0018
// Add HybridCache.
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

// Add services to the container.
builder.Services.AddRazorPages();

// Add SlowDown middleware.
builder.Services.AddSlowDown(options =>
{
    options.OnLimitReached = request =>
    {
        request.HttpContext.Response.Headers["X-SlowDown-OnLimitReached"] = "Hi!";
    };
    options.DelayAfter = 6;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.UseSlowDown();

app.Run();
