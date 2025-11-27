using NetArchTest.Rules;
using NUnit.Framework;
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
            string[] forbiddenReferences = new[]
            {
                "NUnit",
                "Moq",
                "coverlet.core"
            };

            // Act: Визначаємо правило
            var result = Types
                .InAssembly(_assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenReferences)
                .GetResult();

            // Assert: Перевіряємо, що правило не порушено.
            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Залежності від тестових бібліотек знайдено: {string.Join(", ", result.FailingTypes ?? new string[0])}");
        }

        // ПРАВИЛО 2: Класи в шарі Networking повинні реалізовувати інтерфейси.
        [Test]
        public void NetworkingClasses_Should_Implement_Interfaces()
        {
            // Act: Визначаємо правило
            var result = Types
                .InNamespace(NetworkingNamespace)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Wrapper")
                .Should()
                .ImplementInterface()
                .GetResult();

            // Assert: Перевіряємо, що правило не порушено.
            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Класи в {NetworkingNamespace} повинні реалізовувати інтерфейси. Класи, що порушують правило: {string.Join(", ", result.FailingTypes ?? new string[0])}");
        }

        // ПРАВИЛО 3: Клас NetSdrClient (головний) повинен бути кінцевою точкою.
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

            Assert.That(result.IsSuccessful,
                $"Клас NetSdrClient повинен бути 'sealed', щоб запобігти успадкуванню. Поточний стан: {string.Join(", ", result.FailingTypes ?? new string[0])}");
        }

        // ДОДАТКОВЕ ПРАВИЛО: Перевірка, що інтерфейси визначені правильно
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
                .ResideInNamespace("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful,
                $"Інтерфейси повинні знаходитись у просторі імен Networking. Порушення: {string.Join(", ", result.FailingTypes ?? new string[0])}");
        }
    }
}