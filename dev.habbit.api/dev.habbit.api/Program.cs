
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var endpointString = builder.Configuration["OTEP_EXPORTER_OTLP_ENDPOINT"];
if (string.IsNullOrWhiteSpace(endpointString))
{
    throw new InvalidOperationException("OTLP exporter endpoint not configured.");
}

var endpointUri = new Uri(endpointString);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = endpointUri;
            otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = endpointUri;
            otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));


builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("ManualTest")
    .SetSampler(new AlwaysOnSampler())
    .AddOtlpExporter(opt =>
    {
        opt.Endpoint = endpointUri;
        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
    })
    .Build();

var tracer = tracerProvider.GetTracer("ManualTest");

using (var span = tracer.StartActiveSpan("StartupTestSpan"))
{
    span.SetAttribute("startup", "confirmed");
}

await app.RunAsync();
