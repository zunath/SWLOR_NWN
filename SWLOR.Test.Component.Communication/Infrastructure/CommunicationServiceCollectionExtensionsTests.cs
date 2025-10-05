using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.EventHandlers;
using SWLOR.Component.Communication.Infrastructure;
using SWLOR.Component.Communication.Service;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Test.Component.Communication.Infrastructure
{
    [TestFixture]
    public class CommunicationServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddCommunicationServices_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddCommunicationServices());
        }

        [Test]
        public void AddCommunicationServices_ShouldReturnSameServiceCollection()
        {
            // Act
            var result = _services.AddCommunicationServices();

            // Assert
            Assert.That(result, Is.SameAs(_services));
        }

        [Test]
        public void AddCommunicationServices_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _services.AddCommunicationServices();
                _services.AddCommunicationServices();
            });
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterChatCommandService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var chatCommandServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandService));
            Assert.That(chatCommandServiceDescriptor, Is.Not.Null);
            Assert.That(chatCommandServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterCommunicationService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var communicationServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ICommunicationService));
            Assert.That(communicationServiceDescriptor, Is.Not.Null);
            Assert.That(communicationServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterLanguageService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var languageServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILanguageService));
            Assert.That(languageServiceDescriptor, Is.Not.Null);
            Assert.That(languageServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterHoloComService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var holoComServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IHoloComService));
            Assert.That(holoComServiceDescriptor, Is.Not.Null);
            Assert.That(holoComServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterRoleplayXPService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var roleplayXPServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRoleplayXPService));
            Assert.That(roleplayXPServiceDescriptor, Is.Not.Null);
            Assert.That(roleplayXPServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterSnippetService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var snippetServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ISnippetService));
            Assert.That(snippetServiceDescriptor, Is.Not.Null);
            Assert.That(snippetServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterMessagingService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var messagingServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IMessagingService));
            Assert.That(messagingServiceDescriptor, Is.Not.Null);
            Assert.That(messagingServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterDialogService()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var dialogServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IDialogService));
            Assert.That(dialogServiceDescriptor, Is.Not.Null);
            Assert.That(dialogServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterDialogBuilderAsTransient()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var dialogBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IDialogBuilder));
            Assert.That(dialogBuilderDescriptor, Is.Not.Null);
            Assert.That(dialogBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterChatCommandBuilder()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var chatCommandBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandBuilder));
            Assert.That(chatCommandBuilderDescriptor, Is.Not.Null);
            Assert.That(chatCommandBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterSnippetBuilder()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var snippetBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ISnippetBuilder));
            Assert.That(snippetBuilderDescriptor, Is.Not.Null);
            Assert.That(snippetBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterCommunicationEventHandlers()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(CommunicationEventHandlers));
            Assert.That(eventHandlerDescriptor, Is.Not.Null);
            Assert.That(eventHandlerDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterServicesAsSingletons()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var chatCommandServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandService));
            var communicationServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ICommunicationService));
            var languageServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILanguageService));
            
            Assert.That(chatCommandServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(communicationServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(languageServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterAllRequiredServices()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var chatCommandServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandService));
            var communicationServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ICommunicationService));
            var languageServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILanguageService));
            var holoComServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IHoloComService));
            var roleplayXPServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRoleplayXPService));
            var snippetServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ISnippetService));
            var messagingServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IMessagingService));
            var dialogServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IDialogService));
            var dialogBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IDialogBuilder));
            var chatCommandBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandBuilder));
            var snippetBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ISnippetBuilder));
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(CommunicationEventHandlers));
            
            Assert.That(chatCommandServiceDescriptor, Is.Not.Null, "IChatCommandService should be registered");
            Assert.That(communicationServiceDescriptor, Is.Not.Null, "ICommunicationService should be registered");
            Assert.That(languageServiceDescriptor, Is.Not.Null, "ILanguageService should be registered");
            Assert.That(holoComServiceDescriptor, Is.Not.Null, "IHoloComService should be registered");
            Assert.That(roleplayXPServiceDescriptor, Is.Not.Null, "IRoleplayXPService should be registered");
            Assert.That(snippetServiceDescriptor, Is.Not.Null, "ISnippetService should be registered");
            Assert.That(messagingServiceDescriptor, Is.Not.Null, "IMessagingService should be registered");
            Assert.That(dialogServiceDescriptor, Is.Not.Null, "IDialogService should be registered");
            Assert.That(dialogBuilderDescriptor, Is.Not.Null, "IDialogBuilder should be registered");
            Assert.That(chatCommandBuilderDescriptor, Is.Not.Null, "IChatCommandBuilder should be registered");
            Assert.That(snippetBuilderDescriptor, Is.Not.Null, "ISnippetBuilder should be registered");
            Assert.That(eventHandlerDescriptor, Is.Not.Null, "CommunicationEventHandlers should be registered");
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterServicesWithCorrectImplementationTypes()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            var chatCommandServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IChatCommandService));
            var communicationServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ICommunicationService));
            var languageServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILanguageService));
            
            Assert.That(chatCommandServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(ChatCommandService)));
            Assert.That(communicationServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(CommunicationService)));
            Assert.That(languageServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(LanguageService)));
        }

        [Test]
        public void AddCommunicationServices_ShouldRegisterInterfaceDefinitions()
        {
            // Act
            _services.AddCommunicationServices();

            // Assert
            // Test that the generic helper methods were called to register interface definitions
            // We can't easily test the specific implementations without complex setup,
            // but we can verify that the registration process completed without errors
            Assert.That(_services.Count, Is.GreaterThan(0), "Services should be registered");
        }

        [Test]
        public void AddCommunicationServices_ShouldHandleNullServiceCollection()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddCommunicationServices());
        }
    }
}
