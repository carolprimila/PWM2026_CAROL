namespace ProjetoClubes.Domain.Common;

/// <summary>Objeto de valor de endereço (persistido como colunas da entidade dona).</summary>
public sealed class Endereco
{
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
}
