﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ECommerce.Sales.Api.Consumers;
using ECommerce.Sales.Api.Modules;
using ECommerce.Sales.Api.Services;
using ECommerce.Services.Common.Configuration;
using MassTransit;
using MassTransit.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Sales.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IContainer Container { get; private set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var rabbitHost = Configuration["RabbitHost"];
            Console.WriteLine($"Using RabbitHost='{rabbitHost}'.");

            var connectionString = Configuration["ConnectionString"];
            Console.WriteLine($"Using connectionString='{connectionString}'.");

            var waiter = new DependencyAwaiter();
            waiter.WaitForRabbit(rabbitHost);
            waiter.WaitForSql(connectionString);

            services.AddMvc();

            var builder = new ContainerBuilder();

            builder.Populate(services);
            builder.RegisterModule<BusModule>();
            builder.RegisterModule<ConsumerModule>();
            builder.RegisterType<DataService>().As<IDataService>();

            Container = builder.Build();

            return new AutofacServiceProvider(Container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            var bus = Container.Resolve<IBusControl>();
            var busHandle = TaskUtil.Await(() => bus.StartAsync());
            lifetime.ApplicationStopping.Register(() => busHandle.Stop());
        }
    }
}
