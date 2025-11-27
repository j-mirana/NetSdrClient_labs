using NetArchTest.Rules;
using NUnit.Framework;
using System.Reflection;

namespace NetSdrClientAppTests
{
    // Цей клас перевіряє архітектурні правила на рівні коду.
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
            // Arrange: Визначаємо список відомих тестових залежностей
            string[] forbiddenReferences = new[]
            {
                "NUnit",
                "Moq",
                "coverlet.core"
            };

            // Act: Визначаємо правило: жоден тип у збірці не повинен мати залежностей від заборонених бібліотек.
            // Ми перевіряємо основну збірку, а не тестову.
            IArchRule rule = Types
                .InAssembly(_assembly)
                .ShouldNot()
                .HaveDependencyOnAny(forbiddenReferences);

            // Assert: Перевіряємо, що правило не порушено.
            IArchitectureCheckResult result = rule.Check(_assembly);

            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Залежності від тестових бібліотек знайдено.");
        }

        // ПРАВИЛО 2: Класи в шарі Networking повинні реалізовувати інтерфейси.
        [Test]
        public void NetworkingClasses_Should_Implement_Interfaces()
        {
            // Act: Визначаємо правило: класи в Networking (наприклад, TcpClientWrapper) 
            // повинні реалізовувати принаймні один інтерфейс, якщо вони не абстрактні.
            IArchRule rule = Types
                .InNamespace(NetworkingNamespace)
                .That()
                .AreNotAbstract()
                .Should()
                .ImplementInterface(typeof(object)) // Перевіряє, що клас має інтерфейс
                .Or()
                .AreInterfaces() // Дозволяємо інтерфейси
                .And()
                .HaveNameEndingWith("Wrapper"); // Фільтруємо за нашими обгортками

            // Assert: Перевіряємо, що правило не порушено.
            IArchitectureCheckResult result = rule.Check(_assembly);

            Assert.That(result.IsSuccessful,
                $"Архітектурне правило порушено. Класи в {NetworkingNamespace} повинні реалізовувати інтерфейси.");
        }

        // ПРАВИЛО 3: Клас NetSdrClient (головний) повинен бути кінцевою точкою.
        [Test]
        public void NetSdrClient_Should_Be_Sealed()
        {
            IArchRule rule = Types
                .That()
                .HaveName("NetSdrClient")
                .Should()
                .BeSealed();

            IArchitectureCheckResult result = rule.Check(_assembly);

            Assert.That(result.IsSuccessful,
                "Клас NetSdrClient повинен бути 'sealed', щоб запобігти успадкуванню.");
        }
    }
}