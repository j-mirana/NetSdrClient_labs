using NetArchTest.Rules;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        private const string NetworkingNamespace = "NetSdrClientApp.Networking";
        private Assembly _assembly;

        [SetUp]
        public void Setup()
        {
            // Завантажуємо головну збірку для аналізу
            _assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;
        }

        // ПРАВИЛО 1: Основний проект не повинен мати тестових залежностей.
        [Test]
        public void Application_Should_Not_Depend_On_Test_Libraries()
        {
            string[] forbiddenReferences =
            {
                "NUnit",
                "Moq",
                "coverlet.core"
            };

            var result = Types
                .InAssembly(_assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenReferences)
                .GetResult();

            // Правильний stringify
            var failing = result.FailingTypes?.Select(t => t.FullName).ToArray() ?? new string[0];

            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Залежності від тестових бібліотек знайдено: {string.Join(", ", failing)}");
        }

        // ПРАВИЛО 2: Класи в шарі Networking повинні реалізовувати інтерфейси.
        [Test]
        public void NetworkingClasses_Should_Implement_Interfaces()
        {
            var result = Types
                .InNamespace(NetworkingNamespace)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Wrapper")
                .Should()
                .ImplementInterface(typeof(NetSdrClientApp.Networking.IUdpClient)) // ← Явний Type!
                .Or()                                                             // дозволити інші інтерфейси
                .ImplementInterface(typeof(NetSdrClientApp.Networking.ITcpClient))
                .GetResult();

            var failing = result.FailingTypes?.Select(t => t.FullName).ToArray() ?? new string[0];

            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Класи в {NetworkingNamespace} повинні реалізовувати інтерфейси. Порушники: {string.Join(", ", failing)}");
        }

        // ПРАВИЛО 3: NetSdrClient повинен бути sealed.
        [Test]
        public void NetSdrClient_Should_Be_Sealed()
        {
            var result = Types
                .InAssembly(_assembly)
                .That()
                .HaveName("NetSdrClient")
                .Should()
                .BeSealed()
                .GetResult();

            var failing = result.FailingTypes?.Select(t => t.FullName).ToArray() ?? new string[0];

            Assert.That(result.IsSuccessful,
                $"Клас NetSdrClient повинен бути 'sealed'. Порушники: {string.Join(", ", failing)}");
        }

        // ПРАВИЛО 4: Інтерфейси повинні бути у правильному namespace
        [Test]
        public void Interfaces_Should_Be_In_Correct_Namespace()
        {
            var result = Types
                .InAssembly(_assembly)
                .That()
                .AreInterfaces()
                .And()
                .HaveNameStartingWith("I")
                .Should()
                .ResideInNamespace(NetworkingNamespace)
                .GetResult();

            var failing = result.FailingTypes?.Select(t => t.FullName).ToArray() ?? new string[0];

            Assert.That(result.IsSuccessful,
                $"Інтерфейси повинні знаходитись у просторі імен Networking. Порушення: {string.Join(", ", failing)}");
        }
    }
}
