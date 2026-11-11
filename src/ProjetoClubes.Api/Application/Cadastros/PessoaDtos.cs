using FluentValidation;
using ProjetoClubes.Application.Clubes;
using ProjetoClubes.Application.Common;
using ProjetoClubes.Domain.Common;
using ProjetoClubes.Domain.Pessoas;

namespace ProjetoClubes.Application.Cadastros;

public sealed record DadosPessoaisRequest(
    string NomeCompleto,
    string? Cpf,
    string? Rg,
    DateOnly? DataNascimento,
    Sexo Sexo,
    EstadoCivil? EstadoCivil,
    EnderecoDto? Endereco,
    string? Telefone,
    string? Email,
    string? Profissao = null);

public sealed record PessoaDto(
    Guid Id,
    string NomeCompleto,
    string? Cpf,
    string? Rg,
    DateOnly? DataNascimento,
    int? Idade,
    Sexo Sexo,
    EstadoCivil? EstadoCivil,
    EnderecoDto Endereco,
    string? Telefone,
    string? Email,
    bool TemFoto,
    bool TemAcessoApp,
    string? Profissao,
    DateTimeOffset CadastradoEm);

public sealed record HistoricoCadastroDto(string Evento, string Descricao, Guid? UsuarioId, string? UsuarioNome,
    DateTimeOffset OcorridoEm);

public sealed record AlterarSituacaoRequest(string? Motivo);

/// <summary>Regras de validação dos dados pessoais (associado exige CPF; dependente, opcional).</summary>
public sealed class DadosPessoaisValidator : AbstractValidator<DadosPessoaisRequest>
{
    public DadosPessoaisValidator(bool exigeCpf)
    {
        RuleFor(x => x.NomeCompleto).NotEmpty().WithMessage("Informe o nome completo.").MaximumLength(150)
            .Must(n => n.Trim().Contains(' ')).WithMessage("Informe nome e sobrenome.");
        if (exigeCpf) RuleFor(x => x.Cpf).NotEmpty().WithMessage("Informe o CPF.");
        RuleFor(x => x.Cpf).Cpf();
        RuleFor(x => x.Rg).MaximumLength(20);
        RuleFor(x => x.DataNascimento).NotNull().WithMessage("Informe a data de nascimento.")
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("A data de nascimento não pode ser futura.")
            .GreaterThan(new DateOnly(1900, 1, 1)).WithMessage("Data de nascimento inválida.");
        RuleFor(x => x.Sexo).IsInEnum();
        RuleFor(x => x.Profissao).MaximumLength(100);
        RuleFor(x => x.EstadoCivil).IsInEnum();
        RuleFor(x => x.Telefone).Telefone();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("E-mail inválido.").MaximumLength(254);
        When(x => x.Endereco is not null, () =>
        {
            RuleFor(x => x.Endereco!.Uf).Uf();
            RuleFor(x => x.Endereco!.Cep).Cep();
            RuleFor(x => x.Endereco!.Logradouro).MaximumLength(200);
            RuleFor(x => x.Endereco!.Numero).MaximumLength(20);
            RuleFor(x => x.Endereco!.Complemento).MaximumLength(100);
            RuleFor(x => x.Endereco!.Bairro).MaximumLength(100);
            RuleFor(x => x.Endereco!.Cidade).MaximumLength(100);
        });
    }
}

public static class PessoaMapeamento
{
    public static void Aplicar(Pessoa p, DadosPessoaisRequest r)
    {
        p.NomeCompleto = r.NomeCompleto.Trim();
        p.DefinirCpf(r.Cpf);
        p.Rg = Limpo(r.Rg);
        p.DataNascimento = r.DataNascimento;
        p.Sexo = r.Sexo;
        p.EstadoCivil = r.EstadoCivil;
        p.Telefone = string.IsNullOrWhiteSpace(r.Telefone) ? null : Documentos.SomenteDigitos(r.Telefone);
        p.Email = Limpo(r.Email);
        p.Profissao = Limpo(r.Profissao);
        AplicarEndereco(p, r.Endereco);
    }

    /// <summary>Campos que aparecem no histórico quando mudam (sem guardar os valores — dados pessoais).</summary>
    public static Dictionary<string, string?> Resumo(Pessoa p) => new()
    {
        ["nome"] = p.NomeCompleto, ["CPF"] = p.Cpf, ["RG"] = p.Rg, ["nascimento"] = p.DataNascimento?.ToString("O"),
        ["gênero"] = p.Sexo.ToString(), ["estado civil"] = p.EstadoCivil?.ToString(), ["telefone"] = p.Telefone,
        ["e-mail"] = p.Email, ["profissão"] = p.Profissao,
        ["endereço"] = $"{p.Endereco.Logradouro}|{p.Endereco.Numero}|{p.Endereco.Complemento}|{p.Endereco.Bairro}|{p.Endereco.Cidade}|{p.Endereco.Uf}|{p.Endereco.Cep}",
    };

    public static List<string> CamposAlterados(Dictionary<string, string?> antes, Dictionary<string, string?> depois) =>
        antes.Where(kv => kv.Value != depois[kv.Key]).Select(kv => kv.Key).ToList();

    public static void AplicarEndereco(Pessoa p, EnderecoDto? e)
    {
        p.Endereco.Logradouro = Limpo(e?.Logradouro);
        p.Endereco.Numero = Limpo(e?.Numero);
        p.Endereco.Complemento = Limpo(e?.Complemento);
        p.Endereco.Bairro = Limpo(e?.Bairro);
        p.Endereco.Cidade = Limpo(e?.Cidade);
        p.Endereco.Uf = Limpo(e?.Uf)?.ToUpperInvariant();
        p.Endereco.Cep = string.IsNullOrWhiteSpace(e?.Cep) ? null : Documentos.SomenteDigitos(e.Cep);
    }

    public static PessoaDto ParaDto(Pessoa p, DateOnly hoje) => new(p.Id, p.NomeCompleto, p.Cpf, p.Rg, p.DataNascimento,
        p.Idade(hoje), p.Sexo, p.EstadoCivil,
        new EnderecoDto(p.Endereco.Logradouro, p.Endereco.Numero, p.Endereco.Complemento, p.Endereco.Bairro,
            p.Endereco.Cidade, p.Endereco.Uf, p.Endereco.Cep),
        p.Telefone, p.Email, p.FotoArquivoId is not null, p.UsuarioId is not null, p.Profissao, p.CriadoEm);

    private static string? Limpo(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
