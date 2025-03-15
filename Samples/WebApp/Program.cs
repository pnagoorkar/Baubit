using Baubit.DI;
using WebApp;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("myConfig.json");
builder.UseConfiguredServiceProviderFactory();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/myComponent", (MyComponent myComponent) => myComponent.DoSomething());

app.Run();
