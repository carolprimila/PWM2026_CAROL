using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Clubes.API.Infrastructure;

public static class SwaggerNaming
{
    /// <summary>Nomes de schema legíveis: "MemberDto", "PagedResultOfMemberDto".</summary>
    public static string SchemaId(Type type)
    {
        if (type.IsGenericType)
            return type.Name.Split('`')[0] + "Of" + string.Join("And", type.GetGenericArguments().Select(SchemaId));
        return type.DeclaringType is null ? type.Name : type.DeclaringType.Name + type.Name;
    }
}

/// <summary>
/// Marca como obrigatórias no contrato as propriedades que nunca são nulas, para que os clientes
/// gerados (Web) tenham tipos precisos em vez de "tudo opcional".
/// </summary>
public class RequireNonNullableSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete || concrete.Properties is not { Count: > 0 }) return;
        var required = concrete.Required ?? new HashSet<string>();
        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!concrete.Properties.ContainsKey(name)) continue;
            bool nullable;
            if (property.PropertyType.IsValueType) nullable = Nullable.GetUnderlyingType(property.PropertyType) is not null;
            else lock (Nullability) nullable = Nullability.Create(property).ReadState != NullabilityState.NotNull;
            if (!nullable) required.Add(name);
        }
        concrete.Required = required;
    }
}

/// <summary>Traduz para português as mensagens padrão de validação do framework (as mensagens próprias já estão em português).</summary>
public static partial class ValidationMessages
{
    public static string Translate(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "Valor inválido.";
        if (message.EndsWith("field is required.")) return "Campo obrigatório.";
        var range = RangeRegex().Match(message);
        if (range.Success) return $"O valor deve estar entre {range.Groups[1].Value} e {range.Groups[2].Value}.";
        var minMax = MinMaxLengthRegex().Match(message);
        if (minMax.Success) return $"Informe entre {minMax.Groups[1].Value} e {minMax.Groups[2].Value} caracteres.";
        var max = MaxLengthRegex().Match(message);
        if (max.Success) return $"Informe no máximo {max.Groups[1].Value} caracteres.";
        var min = MinLengthRegex().Match(message);
        if (min.Success) return $"Informe ao menos {min.Groups[1].Value} caracteres ou itens.";
        if (message.Contains("not a valid e-mail")) return "E-mail inválido.";
        if (message.Contains("is not valid") || message.Contains("could not be converted") || message.Contains("is invalid")) return "Valor inválido.";
        return message;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"must be between (\S+) and (\S+)\.$")] private static partial System.Text.RegularExpressions.Regex RangeRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"minimum length of (\d+) and a maximum length of (\d+)")] private static partial System.Text.RegularExpressions.Regex MinMaxLengthRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"maximum length of \D?(\d+)")] private static partial System.Text.RegularExpressions.Regex MaxLengthRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"minimum length of \D?(\d+)")] private static partial System.Text.RegularExpressions.Regex MinLengthRegex();
}
