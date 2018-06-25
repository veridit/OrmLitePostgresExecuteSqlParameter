using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Funq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Configuration;

namespace OrmLitePostgresExecuteSqlParameter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000/")
                .Build();

            host.Run();
        }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseServiceStack(new AppHost());

            app.Run(context =>
            {
                context.Response.Redirect("/metadata");
                return Task.FromResult(0);
            });
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("OrmLitePostgresExecuteSqlParameter", typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            AppSettings = new TextFileSettings("~/appsettings.txt".MapHostAbsolutePath());

            var pgProvider = ServiceStack.OrmLite.PostgreSQL.PostgreSqlDialectProvider.Instance;
            var connectionString = AppSettings.GetString("DatabaseConnectionString");
            var dbFactory = new OrmLiteConnectionFactory(connectionString, PostgreSqlDialect.Provider, setGlobalDialectProvider: true);
            var dates = OrmLiteConfig.DialectProvider.GetDateTimeConverter();
            dates.DateStyle = DateTimeKind.Utc;
            container.Register<IDbConnectionFactory>(dbFactory);

            using (var db = dbFactory.Open())
            {
                db.CreateTable<MyEntity>();
            }
        }
    }

    public class MyEntity
    {
        public long Id { get; set; }
        public DateTimeOffset At { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            Db.ExecuteSql("UPDATE my_entity SET at = now() WHERE id = ANY (@ids)", new {ids = new int[] { }});
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }
}