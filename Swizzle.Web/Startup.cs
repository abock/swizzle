using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

using Swizzle.Formatters;
using Swizzle.Models;
using Swizzle.Services;

namespace Swizzle
{
    public sealed class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
            => Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<
                ObjectPoolProvider,
                DefaultObjectPoolProvider>();
            services.AddSingleton(serviceProvider => serviceProvider
                .GetRequiredService<ObjectPoolProvider>()
                .Create(new StringBuilderPooledObjectPolicy()));

            services.Configure<IngestionServiceOptions>(
                Configuration.GetSection(IngestionServiceOptions.SectionName));

            services.AddSingleton<IngestionService>();

            services.AddSingleton<SwizzleAuthenticationService>();
            services.AddAuthentication("Bearer")
                .AddScheme<
                    SwizzleAuthenticationOptions,
                    SwizzleAuthenticationHandler>("Bearer", null);

            services.AddControllers(options =>
            {
                options.OutputFormatters
                    .RemoveType<SystemTextJsonOutputFormatter>();
                options.OutputFormatters
                    .RemoveType<StringOutputFormatter>();
                options.OutputFormatters
                    .Add(new PlainTextItemOutputFormatter());
                options.OutputFormatters
                    .Add(new SystemTextJsonOutputFormatter(
                        SwizzleJsonSerializerOptions.Default));
                options.OutputFormatters
                    .Add(new ItemResourceOutputFormatter());

                foreach (var kind in ItemResourceKind.All)
                {
                    foreach (var extension in kind.Extensions)
                    {
                        foreach (var contentType in kind.ContentTypes)
                            options.FormatterMappings
                                .SetMediaTypeMappingForFormat(
                                    extension,
                                    contentType);
                    }
                }
            });

            services.AddRazorPages();

            services.AddSwaggerGen();
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            app.ApplicationServices
                .GetRequiredService<IngestionService>()
                .IngestRegisteredCollections(
                    IngestFileOptions.ProduceAlternateResources);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(config =>
            {
                config.SwaggerEndpoint("/swagger/v1/swagger.json", "v1 API");
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
