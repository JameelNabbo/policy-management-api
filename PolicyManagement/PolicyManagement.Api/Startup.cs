using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Azure.Storage.Files.Shares;
using Glasswall.PolicyManagement.Common.Configuration;
using Glasswall.PolicyManagement.Common.Configuration.Validation;
using Glasswall.PolicyManagement.Common.Services;
using Glasswall.PolicyManagement.Common.Store;
using Glasswlal.PolicyManagement.Business.Configuration;
using Glasswlal.PolicyManagement.Business.Services;
using Glasswlal.PolicyManagement.Business.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Glasswall.PolicyManagement.Api
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging =>
            {
                logging.AddDebug();
            })
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("*",
                    builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowAnyOrigin();
                    });
            });

            services.TryAddTransient<IConfigurationParser, EnvironmentVariableParser>();
            services.TryAddTransient<IDictionary<string, IConfigurationItemValidator>>(_ => new Dictionary<string, IConfigurationItemValidator>
            {
                {nameof(IPolicyManagementApiConfiguration.AzureStorageConnectionString), new StringValidator(1)},
                {nameof(IPolicyManagementApiConfiguration.ShareName), new StringValidator(1)}
            });
            services.TryAddSingleton<IPolicyManagementApiConfiguration>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfigurationParser>();
                return configuration.Parse<PolicyManagementApiConfiguration>();
            });

            services.TryAddTransient<IPolicyService, PolicyService>();

            services.TryAddTransient(s =>
            {
                var configuration = s.GetRequiredService<IPolicyManagementApiConfiguration>();
                return new ShareServiceClient(configuration.AzureStorageConnectionString).GetShareClient(configuration.ShareName);
            });

            services.TryAddTransient<IEnumerable<IFileShare>>(s =>
            {
                var clients = s.GetRequiredService<IEnumerable<ShareClient>>();
                return clients.Select(client => new AzureFileShare(client)).ToArray();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseAuthorization();

            app.Use((context, next) =>
            {
                context.Response.Headers["Access-Control-Expose-Headers"] = "*";
                context.Response.Headers["Access-Control-Allow-Headers"] = "*";
                context.Response.Headers["Access-Control-Allow-Origin"] = "*";

                if (context.Request.Method != "OPTIONS") return next.Invoke();

                context.Response.StatusCode = 200;
                return context.Response.WriteAsync("OK");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseCors("*");
        }
    }
}