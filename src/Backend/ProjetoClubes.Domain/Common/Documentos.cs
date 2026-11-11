namespace ProjetoClubes.Domain.Common;

/// <summary>Normalização e validação de CPF/CNPJ (dígitos verificadores).</summary>
public static class Documentos
{
    public static string SomenteDigitos(string? valor) =>
        valor is null ? string.Empty : new string(valor.Where(char.IsAsciiDigit).ToArray());

    public static bool CpfValido(string? cpf)
    {
        var d = SomenteDigitos(cpf);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;
        return Digito(d, 9, 10) == d[9] - '0' && Digito(d, 10, 11) == d[10] - '0';

        static int Digito(string s, int len, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < len; i++) soma += (s[i] - '0') * (pesoInicial - i);
            var r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }
    }

    public static bool CnpjValido(string? cnpj)
    {
        var d = SomenteDigitos(cnpj);
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;
        int[] p1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] p2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return Digito(d, p1) == d[12] - '0' && Digito(d, p2) == d[13] - '0';

        static int Digito(string s, int[] pesos)
        {
            var soma = 0;
            for (var i = 0; i < pesos.Length; i++) soma += (s[i] - '0') * pesos[i];
            var r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }
    }
}
