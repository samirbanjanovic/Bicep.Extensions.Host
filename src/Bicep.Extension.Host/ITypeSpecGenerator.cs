
namespace Bicep.Host.Types
{
    public record TypeSpec(string TypesJson, string IndexJson);

    public interface ITypeSpecGenerator
    {
        TypeSpec GenerateBicepResourceTypes();
    }
}