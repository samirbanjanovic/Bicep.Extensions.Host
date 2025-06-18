
using Azure.Bicep.Types.Index;

namespace Bicep.Extension.Host.TypeBuilder;

public record TypeSpec(string TypesJson, string IndexJson);

public interface ITypeDefinitionBuilder
{
    TypeSettings Settings { get; }
    TypeSpec GenerateBicepResourceTypes();
}
