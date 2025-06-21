using Pdf2HtmlServer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.Configure<AuthRef>(conf =>
    {
        var configuration = builder.Configuration;
        conf.AuthKey = configuration["AuthKey"]??"";
        conf.HtmlStoragePath = configuration["HtmlStoragePath"]??"";
        conf.PdfStoragePath = configuration["PdfStoragePath"]??"";
        conf.RunPath = configuration["RunPath"]??"";
    })
    ;
builder.WebHost.ConfigureKestrel(options =>
{
    var configuration = builder.Configuration;
    var port = Int32.Parse(configuration["Port"]??"9000");
    options.ListenAnyIP(port); // 监听所有 IP 的 5001 端口
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapFallbackToFile("{*path:nonfile}", "index.html");
app.MapControllers();
app.Run();