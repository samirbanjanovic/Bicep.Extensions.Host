
using Azure.Bicep.Types.Index;

namespace Bicep.Extension.Host.TypeBuilder;

public record TypeSpec(string TypesJson, string IndexJson);

public interface ITypeSpecGenerator
{
    TypeSettings Settings { get; }
    TypeSpec GenerateBicepResourceTypes();
}
