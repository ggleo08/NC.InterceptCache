using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NC.InterceptorCache;
using CacheSample.Web.BizServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace CacheSample.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public ILifetimeScope AutofacContainer { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddOptions();

            // �����ڴ滺��
            services.AddMemoryCaching(new MemoryCacheOptions()
            {
                // SizeLimit = 1024 // ���û��������С�������˸�ֵʱ��д�뻺���Ǳ�����ʾָ�������С
                // ExpirationScanFrequency= TimeSpan.FromSeconds(5), // ����ɨ��������ʱ����
                // CompactionPercentage=1024, // �����ڳ�������СʱҪѹ���Ļ�����
            });

            // ����Redis
            services.AddRedisCache();
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // 
            builder.RegisterType<MemoryCaching>().As<ICaching>();
            //builder.RegisterType<MemoryCache>().As<IMemoryCache>().SingleInstance();

            // TODO��ע��Redis����

            builder.RegisterType<TestService>().As<IService>().EnableInterfaceInterceptors();
            builder.RegisterType<CacheableInterceptor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime, IDistributedCache cache)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

            lifetime.ApplicationStarted.Register(() =>
            {
                var currentTime = DateTime.Now.ToString();
                byte[] encodedCurrentTime = Encoding.UTF8.GetBytes(currentTime);
                var options = new DistributedCacheEntryOptions()
                              .SetSlidingExpiration(TimeSpan.FromSeconds(20));
                cache.Set("cachedTime", encodedCurrentTime, options);
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
