using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sulthan.Core.Interfaces;
using Sulthan.Infrastructure.Data;
using Sulthan.Infrastructure.Printing;
using Sulthan.Infrastructure.Repositories;
using Sulthan.Infrastructure.Services;
using SulthanERP.Api.Filters;
using SulthanERP.Api.Middleware;
using SulthanERP.Api.Printing;
using System.Text;

namespace SulthanERP.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://0.0.0.0:5195");

            // Database
            builder.Services.AddSingleton<KitchenPrintJobInterceptor>();
            builder.Services.AddDbContext<RestaurantDbContext>((serviceProvider, options) =>
                options
                    .UseSqlServer(
                        builder.Configuration.GetConnectionString("DefaultConnection"),
                        sqlOptions => sqlOptions.MigrationsAssembly("Sulthan.Infrastructure"))
                    .AddInterceptors(
                        serviceProvider.GetRequiredService<KitchenPrintJobInterceptor>()));

            // Controllers
            builder.Services
                .AddControllers(options =>
                {
                    options.Filters.Add<ValidationFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            // Prevent default ASP.NET validation response
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // ===========================
            // Dependency Injection
            // ===========================

            // Authentication
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IJwtService, JwtService>();

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
            builder.Services.AddScoped<ITableRepository, TableRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<IBillCounterRepository, BillCounterRepository>();
            builder.Services.AddScoped<IKitchenOrderTicketRepository, KitchenOrderTicketRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

            // Services
            builder.Services.AddScoped<ITableService, TableService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IKitchenOrderTicketService, KitchenOrderTicketService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<ICheckoutService, CheckoutService>();
            builder.Services.AddScoped<IPendingOrderService, PendingOrderService>();
            builder.Services.AddScoped<ICaptainOrderService, CaptainOrderService>();
            builder.Services.AddScoped<ICashClosingService, CashClosingService>();
            builder.Services.AddScoped<IBillingService, BillingService>();
            builder.Services.AddScoped<IReceiptFormatter, ReceiptFormatter>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            // Durable automatic kitchen printing
            builder.Services.Configure<KitchenPrintingOptions>(
                builder.Configuration.GetSection(KitchenPrintingOptions.SectionName));
            builder.Services.AddSingleton<KitchenKotFormatter>();
            builder.Services.AddSingleton<KitchenKotCancellationFormatter>();
            builder.Services.AddSingleton<IKitchenPrintTransport, KitchenPrintTransport>();
            builder.Services.AddHostedService<KitchenPrintWorker>();

            // Durable automatic customer-bill and paid-receipt printing
            builder.Services.Configure<CashierPrintingOptions>(
                builder.Configuration.GetSection(CashierPrintingOptions.SectionName));
            builder.Services.AddScoped<CustomerBillFormatter>();
            builder.Services.AddSingleton<ICashierPrintTransport, CashierPrintTransport>();
            builder.Services.AddHostedService<CustomerBillPrintWorker>();

            // Filters
            builder.Services.AddScoped<ValidationFilter>();

            // JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"];

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey!))
                    };
                });

            builder.Services.AddAuthorization();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Sulthan ERP API",
                    Version = "v1"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT Token"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
                await DataSeeder.SeedAsync(context);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
