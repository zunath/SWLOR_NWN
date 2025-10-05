using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Communication.Service
{
    [TestFixture]
    public class DialogBuilderTests : TestBase
    {
        [Test]
        public void DialogBuilder_ShouldHaveFreshState_WhenCreatedMultipleTimes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            var serviceProvider = services.BuildServiceProvider();

            // Act - Create two separate instances
            var builder1 = serviceProvider.GetRequiredService<IDialogBuilder>();
            var builder2 = serviceProvider.GetRequiredService<IDialogBuilder>();

            // Add pages to first builder
            builder1.AddPage("PAGE1", page => { });
            builder1.AddPage("PAGE2", page => { });

            // Add same page ID to second builder - should not conflict
            builder2.AddPage("PAGE1", page => { });
            builder2.AddPage("PAGE2", page => { });

            // Assert - Both builders should have their own state
            Assert.That(builder1, Is.Not.SameAs(builder2));
            
            // Both should be able to add the same page IDs without conflict
            var dialog1 = builder1.Build();
            var dialog2 = builder2.Build();
            
            Assert.That(dialog1, Is.Not.Null);
            Assert.That(dialog2, Is.Not.Null);
        }

        [Test]
        public void DialogBuilder_ShouldThrowException_WhenAddingDuplicatePageInSameInstance()
        {
            // Arrange
            var builder = new DialogBuilder();

            // Act
            builder.AddPage("PAGE1", page => { });
            
            // Assert - Adding the same page ID twice should throw
            Assert.Throws<ArgumentException>(() => 
                builder.AddPage("PAGE1", page => { }));
        }

        [Test]
        public void DialogBuilder_ShouldAllowSamePageId_InDifferentInstances()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            var serviceProvider = services.BuildServiceProvider();

            var builder1 = serviceProvider.GetRequiredService<IDialogBuilder>();
            var builder2 = serviceProvider.GetRequiredService<IDialogBuilder>();

            // Act & Assert - Both should be able to add the same page ID
            builder1.AddPage("MAIN_PAGE", page => { });
            builder2.AddPage("MAIN_PAGE", page => { }); // Should not throw

            var dialog1 = builder1.Build();
            var dialog2 = builder2.Build();
            
            Assert.That(dialog1, Is.Not.Null);
            Assert.That(dialog2, Is.Not.Null);
        }

        [Test]
        public void DialogBuilder_ShouldResetState_WhenBuildIsCalled()
        {
            // Arrange
            var builder = new DialogBuilder();
            builder.AddPage("PAGE1", page => { });
            builder.Build(); // First build

            // Act - Add pages after first build
            builder.AddPage("PAGE2", page => { });
            builder.AddPage("PAGE3", page => { });

            // Assert - Should be able to add pages after build
            var dialog = builder.Build();
            Assert.That(dialog, Is.Not.Null);
        }
    }
}
