using NetArchTest.Rules;
using System;
using System.Linq;

namespace NetSdrClientAppTests.Rules
{
    public class ImplementsAnyInterfaceRule : ICustomRule
    {
        public bool MeetsRule(Type type)
        {
            return type.GetInterfaces().Any();
        }
    }
}
