using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PicPaySimplified.Infra.Messaging.Configurations;
using PicPaySimplified.Infra.Messaging.Consumers;
using PicPaySimplified.Infra.Messaging.Interfaces;
using PicPaySimplified.Infra.Messaging.Producer;
using PipcPaySimplified.Application;
using PipcPaySimplified.Application.EventHandlers;
using PipcPaySimplified.Application.Interfaces;
using PipcPaySimplified.Application.UseCases.CreateAccount;
using PipcPaySimplified.Domain.Events;
using PipcPaySimplified.Domain.Repositories;
using PipcPaySimplified.Domain.SeedWork;
using PipcPaySimplified.Infra.Data;
using PipcPaySimplified.Infra.Data.Repositories;
using PipcPaySimplified.Infra.ExternalService.RestEase;
using PipcPaySimplified.Infra.ExternalService.Services;
using RabbitMQ.Client;
using Refit;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();

        builder.Services.AddDbContext<PicPaySimplifiedDbContext>(
            options => options.UseMySql(
                "Server=localhost;Port=3307;Database=PicpaySimplified;Uid=root;Pwd=root",
                ServerVersion.AutoDetect("Server=localhost;Port=3307;Database=PicpaySimplified;Uid=root;Pwd=root")
            )
        );

        builder.Services.AddScoped<IAccountRepository, AccountRepository>();
        builder.Services.AddScoped<ITransferService, TransferService>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<INotifyService, NotifyService>();
        builder.Services.AddMediatR(config => 
            config.RegisterServicesFromAssemblies(typeof(CreateAccount).Assembly)
        );

        builder.Services
            .AddRefitClient<ITransferApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://util.devi.tools/api")); 
        
        builder.Services
            .AddRefitClient<INotifyApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://util.devi.tools/api"));

        builder.Services.AddTransient<IDomainEventPublisher, DomainEventPublisher>();
        builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
        builder.Services.AddTransient<IDomainEventHandler<TransferReceivedEvent>, TransferReceivedEventHandler>();

        builder.Services
            .Configure<RabbitMQConfiguration>(
                builder.Configuration.GetSection(RabbitMQConfiguration.ConfigurationSection)
            );

        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<RabbitMQConfiguration>>().Value;

            var factory = new ConnectionFactory
            {
                HostName = config.Hostname!,
                Port = config.Port,
                UserName = config.Username!,
                Password = config.Password!

            };

            return Task.Run(() => factory.CreateConnectionAsync()).GetAwaiter().GetResult();
        });

        builder.Services.AddSingleton<ChannelManager>();
        
        builder.Services.AddTransient<IMessageProducer, RabbitMQProducer>();

        builder.Services.AddHostedService<TransferReceivedConsumer>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}