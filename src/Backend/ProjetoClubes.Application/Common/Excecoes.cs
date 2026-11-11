namespace ProjetoClubes.Application.Common;

/// <summary>Base das exceções de aplicação. A API converte cada tipo no status HTTP correspondente.</summary>
public abstract class AppException(string codigo, string mensagem) : Exception(mensagem)
{
    public string Codigo { get; } = codigo;
}

/// <summary>404 — registro inexistente ou fora do escopo do clube atual (não revela existência).</summary>
public sealed class NaoEncontradoException(string recurso)
    : AppException("nao_encontrado", $"{recurso} não encontrado(a).");

/// <summary>409 — conflito com o estado atual (ex.: duplicidade).</summary>
public sealed class ConflitoException(string codigo, string mensagem) : AppException(codigo, mensagem);

/// <summary>403 — autenticado, mas sem permissão para a operação.</summary>
public sealed class AcessoNegadoException(string mensagem = "Você não tem permissão para realizar esta operação.")
    : AppException("acesso_negado", mensagem);

/// <summary>401 — credenciais ou sessão inválidas.</summary>
public sealed class NaoAutenticadoException(string mensagem, string codigo = "nao_autenticado")
    : AppException(codigo, mensagem);

/// <summary>423 — conta temporariamente bloqueada.</summary>
public sealed class ContaBloqueadaException(string mensagem) : AppException("conta_bloqueada", mensagem);

/// <summary>400 — clube não selecionado para uma operação que exige clube.</summary>
public sealed class ClubeNaoSelecionadoException()
    : AppException("clube_nao_selecionado", "Selecione um clube para continuar.");
