using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Confluent.Kafka;
using Serilog;
using ProcessadorAcoes.Models;
using ProcessadorAcoes.Validators;
using ProcessadorAcoes.Data;

namespace ProcessadorAcoes
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Carregando configurações...");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json");
            var configuration = builder.Build();

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
            logger.Information(
                "Testando o consumo de mensagens com Kafka + MongoDB");

            string bootstrapServers = configuration["Kafka_Broker"];
            string nomeTopic = configuration["Kafka_Topic"];

            logger.Information($"BootstrapServers = {bootstrapServers}");
            logger.Information($"Topic = {nomeTopic}");

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = $"{nomeTopic}-mongodb",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe(nomeTopic);

                    try
                    {
                        var repository = new AcoesRepository(configuration);
                        while (true)
                        {
                            var cr = consumer.Consume(cts.Token);
                            string dados = cr.Message.Value;

                            logger.Information(
                                $"Mensagem lida: {dados}");

                            var dadosAcao = JsonSerializer.Deserialize<DadosAcao>(dados,
                                new JsonSerializerOptions()
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                            var validationResult = new AcaoValidator().Validate(dadosAcao);
                            if (validationResult.IsValid)
                            {
                                repository.Save(dadosAcao);    
                                logger.Information("Ação registrada com sucesso!");
                            }
                            else
                            {
                                logger.Error("Dados inválidos para a Ação");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        consumer.Close();
                        logger.Warning("Cancelada a execução do Consumer...");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Exceção: {ex.GetType().FullName} | " +
                             $"Mensagem: {ex.Message}");
            }

        }
    }
}