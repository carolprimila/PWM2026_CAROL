using ProjetoClubes.Domain.Common;

namespace ProjetoClubes.Domain.Arquivos;

/// <summary>
/// Metadados de um arquivo privado (documento, foto). O conteúdo fica fora da área pública e só é entregue
/// por endpoints que verificam permissão. <see cref="Chave"/> é interna e nunca vai para o cliente.
/// </summary>
public class Arquivo : AuditableEntity, ITenantScoped
{
    private Arquivo() { }

    public Arquivo(Guid clubeId, string nomeOriginal, string contentType, long tamanhoBytes, string hashSha256, string chave)
    {
        ClubeId = clubeId;
        NomeOriginal = nomeOriginal;
        ContentType = contentType;
        TamanhoBytes = tamanhoBytes;
        HashSha256 = hashSha256;
        Chave = chave;
    }

    public Guid ClubeId { get; private set; }
    public string NomeOriginal { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long TamanhoBytes { get; private set; }
    public string HashSha256 { get; private set; } = null!;
    public string Chave { get; private set; } = null!;

    /// <summary>LGPD: o conteúdo foi apagado do armazenamento; o nome original (que pode identificar alguém) também sai.</summary>
    public void Descartar() => NomeOriginal = "removido";
}

public enum TipoDocumento
{
    Rg = 1,
    Cpf = 2,
    CertidaoNascimento = 3,
    CertidaoCasamento = 4,
    ComprovanteUniaoEstavel = 5,
    ComprovanteResidencia = 6,
    TermoResponsabilidade = 7,
    Outro = 99,
}

public enum SituacaoDocumento
{
    Pendente = 1,
    Aprovado = 2,
    Rejeitado = 3,
}

/// <summary>Documento pessoal de associado ou dependente (vinculado à pessoa).</summary>
public class DocumentoPessoal : AuditableEntity, ITenantScoped, ISoftDelete
{
    private DocumentoPessoal() { }

    public DocumentoPessoal(Guid clubeId, Guid pessoaId, Arquivo arquivo, TipoDocumento tipo, string? descricao)
    {
        ClubeId = clubeId;
        PessoaId = pessoaId;
        ArquivoId = arquivo.Id;
        Arquivo = arquivo;
        Tipo = tipo;
        Descricao = descricao;
        Situacao = SituacaoDocumento.Pendente;
    }

    public Guid ClubeId { get; private set; }
    public Guid PessoaId { get; private set; }
    public Guid ArquivoId { get; private set; }
    public Arquivo Arquivo { get; private set; } = null!;
    public TipoDocumento Tipo { get; private set; }
    public string? Descricao { get; private set; }
    public SituacaoDocumento Situacao { get; private set; }
    public string? ObservacaoAnalise { get; private set; }
    public Guid? AnalisadoPor { get; private set; }
    public DateTimeOffset? AnalisadoEm { get; private set; }

    public bool Excluido { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public Guid? ExcluidoPor { get; private set; }

    public void Analisar(bool aprovado, string? observacao, Guid? usuarioId, DateTimeOffset agora)
    {
        if (!aprovado && string.IsNullOrWhiteSpace(observacao))
            throw new DomainException("documento.motivo_obrigatorio", "Informe o motivo da rejeição do documento.");
        Situacao = aprovado ? SituacaoDocumento.Aprovado : SituacaoDocumento.Rejeitado;
        ObservacaoAnalise = observacao?.Trim();
        AnalisadoPor = usuarioId;
        AnalisadoEm = agora;
    }

    public void MarcarExcluido(DateTimeOffset quando, Guid? por)
    {
        Excluido = true;
        ExcluidoEm = quando;
        ExcluidoPor = por;
    }
}
