using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Infrastructure.Persistence.Interceptors;

namespace ProjetoClubes.API.Middleware;

/// <summary>
/// Converte exceções em respostas RFC 9457 (application/problem+json) com mensagens compreensíveis,
/// um código estável ("codigo") e o Correlation-Id para suporte. Detalhes internos nunca vão para o cliente.
/// </summary>
public sealed class TratamentoGlobalExcecoes(IProblemDetailsService problemDetails, ILogger<TratamentoGlobalExcecoes> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, codigo, titulo, detalhe) = Mapear(exception);
        ProblemDetails problema;

        if (exception is ValidationException validacao)
        {
            var erros = validacao.Errors
                .GroupBy(e => ParaCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
            problema = new ValidationProblemDetails(erros);
        }
        else
        {
            problema = new ProblemDetails();
        }

        problema.Status = status;
        problema.Title = titulo;
        problema.Detail = detalhe;
        problema.Type = $"https://httpstatuses.io/{status}";
        problema.Extensions["codigo"] = codigo;
        problema.Extensions["correlationId"] = http.Items[ContextoRequisicaoMiddleware.ChaveItem] as string
                                               ?? http.TraceIdentifier;

        if (status >= 500)
            logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", http.Request.Method, http.Request.Path);
        else if (exception is ViolacaoTenantException)
            logger.LogWarning(exception, "Violação de isolamento entre clubes em {Caminho}", http.Request.Path);
        else
            logger.LogInformation("Requisição recusada ({Status} {Codigo}): {Mensagem}", status, codigo, exception.Message);

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = problema,
            Exception = exception,
        });
    }

    private static (int Status, string Codigo, string Titulo, string? Detalhe) Mapear(Exception ex) => ex switch
    {
        ValidationException => (400, "validacao", "Verifique os dados informados.", null),
        ClubeNaoSelecionadoException e => (400, e.Codigo, e.Message, null),
        NaoAutenticadoException e => (401, e.Codigo, e.Message, null),
        AcessoNegadoException e => (403, e.Codigo, e.Message, null),
        ViolacaoTenantException => (403, "acesso_negado", "Você não tem permissão para realizar esta operação.", null),
        NaoEncontradoException e => (404, e.Codigo, e.Message, null),
        ConflitoException e => (409, e.Codigo, e.Message, null),
        ContaBloqueadaException e => (423, e.Codigo, e.Message, null),
        DomainException e => (422, e.Codigo, e.Message, null),
        DbUpdateConcurrencyException => (409, "concorrencia", "O registro foi alterado por outra pessoa. Recarregue e tente novamente.", null),
        // Códigos estendidos do SQLite: 2067/1555 único, 275 CHECK, 787 chave estrangeira, 1811 gatilho (RAISE).
        DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 2067 or 1555 } } =>
            (409, "duplicidade", "Já existe um registro com estes dados.", null),
        DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 1811 } e } when e.Message.Contains("conflito_horario") =>
            (409, "conflito_horario", "Já existe um registro ocupando este período.", null),
        DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 275 } } =>
            (422, "regra_violada", "A operação deixaria os dados inválidos (ex.: estoque negativo).", null),
        DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 787 } } =>
            (409, "registro_vinculado", "Este registro está vinculado a outros dados e não pode ser alterado desta forma.", null),
        BadHttpRequestException e => (e.StatusCode, "requisicao_invalida", "Requisição inválida.", null),
        OperationCanceledException => (499, "cancelada", "Requisição cancelada.", null),
        _ => (500, "erro_interno", "Ocorreu um erro inesperado. Tente novamente em instantes.", null),
    };

    private static string ParaCamelCase(string nome) =>
        string.Join('.', nome.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..]));
}
