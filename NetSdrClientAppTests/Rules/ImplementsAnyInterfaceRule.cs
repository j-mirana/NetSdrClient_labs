using Mono.Cecil;
using NetArchTest.Rules;

namespace NetSdrClientAppTests.Rules;

public class ImplementsAnyInterfaceRule : ICustomRule
{
    public bool MeetsRule(TypeDefinition type)
    {
        // Implement your custom rule logic here
        // Example: Check if the type implements any interface
        return type.Interfaces.Count > 0;
    }
}