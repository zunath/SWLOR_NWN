using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SWLOR.Game.Server.Core
{
    public static class Metrics
    {
        static readonly ActivitySource _activitySource = null;
        static readonly string _sourceName = "nwn.swlor.jaeger";

        static Metrics()
        {
            _activitySource = new ActivitySource(_sourceName, "1.0.0");
        }

        public static TracerProvider Initialize()
        {
            // give jaeger time to boot up
            Thread.Sleep(5000);

            // initialize connection to jaeger UDP agent
            return Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("SWLOR-Server"))
                .AddSource(_sourceName)
                .AddJaegerExporter(configure =>
                {
                    configure.Protocol = OpenTelemetry.Exporter.JaegerExportProtocol.HttpBinaryThrift;
                    configure.Endpoint = new Uri("http://jaeger:14268");
                })
                .AddConsoleExporter()
                .Build();
        }

        #nullable enable 
        public static Activity? Create(string name)
        {
            // potential enhancement: track instruments 
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/README.md#tracing-api

            return _activitySource.CreateActivity(name, ActivityKind.Internal);
        }

        public static Activity? Create(string name, ActivityContext parent)
        {
            return _activitySource.CreateActivity(name, ActivityKind.Internal, parent);
        }
    }
}
