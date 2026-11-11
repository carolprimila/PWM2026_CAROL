namespace ProjetoClubes.Domain.Common;

/// <summary>Violação de regra de negócio. A API converte em HTTP 422 com o código informado.</summary>
public class DomainException(string codigo, string mensagem) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
}
