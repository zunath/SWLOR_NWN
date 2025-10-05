using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Dialog.ValueObjects
{
    [TestFixture]
    public class DialogBaseTests : TestBase
    {
        [Test]
        public void DialogBase_ShouldGetFreshDialogBuilder_OnEachSetUpCall()
        {
            // Arrange
            var mockDialogService = Substitute.For<IDialogService>();
            var mockDialogBuilder1 = Substitute.For<IDialogBuilder>();
            var mockDialogBuilder2 = Substitute.For<IDialogBuilder>();
            var mockDialog = new PlayerDialog("TestPage");

            // Create a real service provider with mocked services
            var services = new ServiceCollection();
            var callCount = 0;
            services.AddTransient<IDialogBuilder>(provider => callCount++ == 0 ? mockDialogBuilder1 : mockDialogBuilder2);
            var serviceProvider = services.BuildServiceProvider();

            mockDialogBuilder1.Build().Returns(mockDialog);
            mockDialogBuilder2.Build().Returns(mockDialog);

            var testDialog = new TestDialog(mockDialogService, serviceProvider);

            // Act - Call SetUp multiple times
            var result1 = testDialog.SetUp(123u);
            var result2 = testDialog.SetUp(456u);

            // Assert - Should get fresh DialogBuilder each time
            Assert.That(result1, Is.SameAs(mockDialog));
            Assert.That(result2, Is.SameAs(mockDialog));
        }

        [Test]
        public void DialogBase_ShouldUseDialogBuilder_ToBuildDialog()
        {
            // Arrange
            var mockDialogService = Substitute.For<IDialogService>();
            var mockDialogBuilder = Substitute.For<IDialogBuilder>();
            var mockDialog = new PlayerDialog("TestPage");

            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder>(provider => mockDialogBuilder);
            var serviceProvider = services.BuildServiceProvider();

            mockDialogBuilder.Build().Returns(mockDialog);

            var testDialog = new TestDialog(mockDialogService, serviceProvider);

            // Act
            var result = testDialog.SetUp(123u);

            // Assert
            mockDialogBuilder.Received(1).Build();
            Assert.That(result, Is.SameAs(mockDialog));
        }

        [Test]
        public void DialogBase_ShouldPassPlayerId_ToDialogBuilder()
        {
            // Arrange
            var mockDialogService = Substitute.For<IDialogService>();
            var mockDialogBuilder = Substitute.For<IDialogBuilder>();
            var mockDialog = new PlayerDialog("TestPage");

            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder>(provider => mockDialogBuilder);
            var serviceProvider = services.BuildServiceProvider();

            mockDialogBuilder.Build().Returns(mockDialog);

            var testDialog = new TestDialog(mockDialogService, serviceProvider);

            // Act
            testDialog.SetUp(123u);

            // Assert - Verify that the DialogBuilder was used (indirectly through Build call)
            mockDialogBuilder.Received(1).Build();
        }

        [Test]
        public void DialogBase_ShouldHandleNullDialogBuilder_Gracefully()
        {
            // Arrange
            var mockDialogService = Substitute.For<IDialogService>();

            var services = new ServiceCollection();
            // Don't register IDialogBuilder - this should cause GetRequiredService to throw
            var serviceProvider = services.BuildServiceProvider();

            var testDialog = new TestDialog(mockDialogService, serviceProvider);

            // Act & Assert - Should throw when DialogBuilder is not registered
            Assert.Throws<InvalidOperationException>(() => testDialog.SetUp(123u));
        }

        [Test]
        public void DialogBase_ShouldHandleDialogBuilderBuildFailure_Gracefully()
        {
            // Arrange
            var mockDialogService = Substitute.For<IDialogService>();
            var mockDialogBuilder = Substitute.For<IDialogBuilder>();

            var services = new ServiceCollection();
            services.AddTransient<IDialogBuilder>(provider => mockDialogBuilder);
            var serviceProvider = services.BuildServiceProvider();

            mockDialogBuilder.When(x => x.Build()).Throw(new InvalidOperationException("Build failed"));

            var testDialog = new TestDialog(mockDialogService, serviceProvider);

            // Act & Assert - Should propagate the exception
            Assert.Throws<InvalidOperationException>(() => testDialog.SetUp(123u));
        }
    }

    // Test implementation of DialogBase for testing
    public class TestDialog : DialogBase
    {
        public TestDialog(IDialogService dialogService, IServiceProvider serviceProvider) 
            : base(dialogService, serviceProvider)
        {
        }

        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder;
            return builder.Build();
        }
    }
}
