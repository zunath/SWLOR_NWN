using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Communication.Integration
{
    [TestFixture]
    public class DialogSystemIntegrationTests : TestBase
    {
        [Test]
        public void DialogSystem_ShouldNotThrow_WhenCreatingMultipleDialogBuilders()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<SWLOR.Shared.Abstractions.Contracts.ILogger>(provider => Substitute.For<SWLOR.Shared.Abstractions.Contracts.ILogger>());
            services.AddSingleton<IServiceProvider>(provider => provider);

            var serviceProvider = services.BuildServiceProvider();
            var dialogService = serviceProvider.GetRequiredService<IDialogService>();

            // Act & Assert - Should not throw when creating multiple DialogBuilder instances
            var builder1 = serviceProvider.GetRequiredService<IDialogBuilder>();
            var builder2 = serviceProvider.GetRequiredService<IDialogBuilder>();

            // Both should be different instances (transient registration)
            Assert.That(builder1, Is.Not.SameAs(builder2));
        }

        [Test]
        public void DialogSystem_ShouldHandleLoadConversationErrors_Gracefully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<SWLOR.Shared.Abstractions.Contracts.ILogger>(provider => Substitute.For<SWLOR.Shared.Abstractions.Contracts.ILogger>());
            services.AddSingleton<IServiceProvider>(provider => provider);

            var serviceProvider = services.BuildServiceProvider();
            var dialogService = serviceProvider.GetRequiredService<IDialogService>();

            var player = 123u;
            var talkTo = 456u;

            // Act & Assert - Should throw when conversation doesn't exist
            Assert.Throws<KeyNotFoundException>(() => 
                dialogService.LoadConversation(player, talkTo, "NonExistentDialog", -1));
        }

        [Test]
        public void DialogSystem_ShouldValidateInputParameters_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<SWLOR.Shared.Abstractions.Contracts.ILogger>(provider => Substitute.For<SWLOR.Shared.Abstractions.Contracts.ILogger>());
            services.AddSingleton<IServiceProvider>(provider => provider);

            var serviceProvider = services.BuildServiceProvider();
            var dialogService = serviceProvider.GetRequiredService<IDialogService>();

            var player = 123u;
            var talkTo = 456u;

            // Act & Assert - Should validate input parameters
            Assert.Throws<ArgumentException>(() => 
                dialogService.LoadConversation(player, talkTo, null, -1));
            
            Assert.Throws<ArgumentException>(() => 
                dialogService.LoadConversation(player, talkTo, "", -1));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                dialogService.LoadConversation(player, talkTo, "TestDialog", 0));
        }
    }
}