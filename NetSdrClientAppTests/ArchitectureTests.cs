using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using System;


namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        private const string NetworkingNamespace = "NetSdrClientApp.Networking";
        private Assembly _assembly;

        [SetUp]
        public void Setup()
        {
            // Note: typeof(NetSdrClientApp.NetSdrClient) must refer to a type
            // within the assembly you intend to test.
            _assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;
        }

        [Test]
        public void Application_Should_Not_Depend_On_Test_Libraries()
        {
            string[] forbiddenReferences = { "NUnit", "Moq", "coverlet.core" };

            var result = Types
                .InAssembly(_assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenReferences)
                .GetResult();

            // The 'using System;' statement resolves the type mismatch error
            // reported on the '??' operator by making Array.Empty accessible.
            var failing = result.FailingTypes?
                .Select(t => t.FullName)
                .ToArray() ?? Array.Empty<string>();

            Assert.That(result.IsSuccessful,
                $"Found test deps: {string.Join(", ", failing)}");
        }

        [Test]
        public void NetworkingClasses_Should_Implement_Interfaces()
        {
            // This custom rule checks that any class in the Networking namespace 
            // ending with "Wrapper" implements at least one interface.
            var result = Types
                .InNamespace(NetworkingNamespace)
                .That().AreClasses()
                .And().HaveNameEndingWith("Wrapper")
                .Should()
                .MeetCustomRule(t => t.GetInterfaces().Any())
                .GetResult();

            var failing = result.FailingTypes?
                .Select(t => t.FullName)
                .ToArray() ?? Array.Empty<string>();

            Assert.That(result.IsSuccessful,
                $"Classes must implement interfaces: {string.Join(", ", failing)}");
        }

        [Test]
        public void NetSdrClient_Should_Be_Sealed()
        {
            var result = Types
                .InAssembly(_assembly)
                .That().HaveName("NetSdrClient")
                .Should().BeSealed()
                .GetResult();

            var failing = result.FailingTypes?
                .Select(t => t.FullName)
                .ToArray() ?? Array.Empty<string>();

            Assert.That(result.IsSuccessful,
                $"NetSdrClient must be sealed: {string.Join(", ", failing)}");
        }

        [Test]
        public void Interfaces_Should_Be_In_Correct_Namespace()
        {
            var result = Types
                .InAssembly(_assembly)
                .That().AreInterfaces()
                .And().HaveNameStartingWith("I")
                .Should().ResideInNamespace(NetworkingNamespace)
                .GetResult();

            var failing = result.FailingTypes?
                .Select(t => t.FullName)
                .ToArray() ?? Array.Empty<string>();

            Assert.That(result.IsSuccessful,
                $"Bad interface namespaces: {string.Join(", ", failing)}");
        }
    }
}