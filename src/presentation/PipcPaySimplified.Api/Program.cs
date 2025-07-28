using Microsoft.EntityFrameworkCore;
using PicPaySimplified.Infra.Messaging.Configurations;
using PicPaySimplified.Infra.Messaging.Consumers;
using PicPaySimplified.Infra.Messaging.Interfaces;
using PicPaySimplified.Infra.Messaging.Producer;
using PipcPaySimplified.Api.Configurations;
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
                builder.Configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
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

        builder.Services.AddRabbitMqConfiguration(builder.Configuration);

        builder.Services.AddApplicationInsightsTelemetry(builder.Configuration);

        builder.Services.AddSingleton<ChannelManager>();
        
        builder.Services.AddTransient<IMessageProducer, RabbitMQProducer>();

        builder.Services.AddHostedService<TransferReceivedConsumer>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                // Pega o DbContext da injeção de dependência
                var context = services.GetRequiredService<PicPaySimplifiedDbContext>();

                // Pega o logger para registrar o que está acontecendo
                var logger = services.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Verificando e aplicando migrations pendentes...");

                // Aplica quaisquer migrations que ainda não foram aplicadas ao banco de dados.
                // Isso também criará o banco de dados se ele não existir.
                context.Database.Migrate();

                logger.LogInformation("Migrations aplicadas com sucesso.");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Ocorreu um erro durante a aplicação das migrations.");
                // Opcional: você pode decidir se quer parar a aplicação aqui ou não.
                // Para a maioria dos casos, se a migration falhar, a aplicação não funcionará de qualquer maneira.
            }
        }

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