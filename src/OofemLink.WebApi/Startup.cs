﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.DataAccess;
using OofemLink.WebApi.Filters;

namespace OofemLink.WebApi
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRouting(options => options.LowercaseUrls = true);

			services.AddMvc(options =>
				{
					options.Filters.Add(typeof(NullResultIs404ActionFilter));
					options.Filters.Add(typeof(GlobalExceptionFilter));
				}).AddJsonOptions(options =>
				{
					options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
				});

			services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
				{
					builder.AllowAnyOrigin()
						   .AllowAnyMethod()
						   .AllowAnyHeader();
				}));

			services.AddDbContext<Data.DataContext>(options =>
			{
				switch (Configuration["DatabaseProvider"])
				{
					case "SqlServer":
						options.UseSqlServer(Configuration.GetConnectionString("oofem_db"));
						break;
					case "Sqlite":
						options.UseSqlite(Configuration.GetConnectionString("oofem_db"));
						break;
					case "InMemory":
						options.UseInMemoryDatabase();
						break;
				}
			});

			services.AddScoped<IProjectService, ProjectService>();
			services.AddScoped<ISimulationService, SimulationService>();
			services.AddScoped<IModelService, ModelService>();

			services.AddSwaggerGen();

			Mapper.Initialize(config => config.AddProfile<DtoMappingProfile>());
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			if (env.IsDevelopment())
			{
				loggerFactory.AddDebug();
			}

			app.UseCors("CorsPolicy");

			app.UseMvc();

			app.UseSwagger();
			app.UseSwaggerUi();
		}
	}
}
