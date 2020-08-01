using System;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ProcessadorAcoes.Models;
using ProcessadorAcoes.Documents;

namespace ProcessadorAcoes.Data
{
    public class AcoesRepository
    {
        private readonly MongoClient _client;

        public AcoesRepository(IConfiguration configuration)
        {
            _client = new MongoClient(
                configuration["MongoDB_Connection"]);

        }

        public void Save(DadosAcao acao)
        {
            IMongoDatabase db = _client.GetDatabase("DBAcoesMongoDB");

            var historico =
                db.GetCollection<AcaoDocument>("HistoricoAcoes");

            var horario = DateTime.Now;
            var document = new AcaoDocument();
            document.HistLancamento = acao.Codigo + horario.ToString("yyyyMMdd-HHmmss");
            document.Codigo = acao.Codigo;
            document.Valor = acao.Valor.Value;
            document.Data = horario.ToString("yyyy-MM-dd HH:mm:ss");

            historico.InsertOne(document);
        }
    }
}