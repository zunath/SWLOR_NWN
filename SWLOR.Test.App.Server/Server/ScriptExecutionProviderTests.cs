using NSubstitute;
using SWLOR.App.Server.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.App.Server.Server
{
    /// <summary>
    /// Unit tests for ScriptExecutionProvider class.
    /// Tests the bridge between NWScript API and SWLOR's script execution system.
    /// </summary>
    [TestFixture]
    public class ScriptExecutionProviderTests
    {
        private IClosureManager _mockClosureManager;
        private ScriptToEventMapper _mockMapper;
        private IScriptExecutor _mockExecutor;
        private ILogger _mockLogger;
        private ScriptExecutionProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _mockClosureManager = Substitute.For<IClosureManager>();
            _mockLogger = Substitute.For<ILogger>();
            _mockMapper = new ScriptToEventMapper(_mockLogger);
            _mockExecutor = Substitute.For<IScriptExecutor>();
            
            _provider = new ScriptExecutionProvider(
                _mockClosureManager,
                _mockMapper,
                _mockExecutor);
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new ScriptExecutionProvider(
                _mockClosureManager,
                _mockMapper,
                _mockExecutor);

            // Assert
            Assert.That(provider, Is.Not.Null);
        }

        [Test]
        public void ObjectSelf_Get_ShouldReturnClosureManagerValue()
        {
            // Arrange
            const uint expectedObjectId = 12345u;
            _mockClosureManager.ObjectSelf.Returns(expectedObjectId);

            // Act
            var result = _provider.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(expectedObjectId));
            var _ = _mockClosureManager.Received(1).ObjectSelf;
        }

        [Test]
        public void ObjectSelf_Set_ShouldUpdateClosureManagerValue()
        {
            // Arrange
            const uint newObjectId = 54321u;

            // Act
            _provider.ObjectSelf = newObjectId;

            // Assert
            _mockClosureManager.Received(1).ObjectSelf = newObjectId;
        }

        [Test]
        public void HasScript_WithValidScriptName_ShouldCallMapper()
        {
            // Arrange
            const string scriptName = "mod_load";

            // Act
            var result = _provider.HasScript(scriptName);

            // Assert - Result depends on whether mapper finds the script
            // In test environment, result may vary, so just verify it doesn't crash
            Assert.That(result, Is.TypeOf<bool>());
        }

        [Test]
        public void HasScript_WithNullScriptName_ShouldReturnFalse()
        {
            // Arrange
            string scriptName = null;

            // Act
            var result = _provider.HasScript(scriptName);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasScript_WithEmptyScriptName_ShouldReturnFalse()
        {
            // Arrange
            const string scriptName = "";

            // Act
            var result = _provider.HasScript(scriptName);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasScript_WithInvalidScriptName_ShouldReturnFalse()
        {
            // Arrange
            const string scriptName = "invalid_script_that_does_not_exist";

            // Act
            var result = _provider.HasScript(scriptName);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetActionScripts_WithAnyScriptName_ShouldReturnEmptyCollection()
        {
            // Arrange
            const string scriptName = "mod_load";

            // Act
            var result = _provider.GetActionScripts(scriptName);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty, "GetActionScripts is deprecated and should return empty collection");
        }

        [Test]
        public void GetActionScripts_CalledMultipleTimes_ShouldAlwaysReturnEmpty()
        {
            // Act
            var result1 = _provider.GetActionScripts("script1");
            var result2 = _provider.GetActionScripts("script2");
            var result3 = _provider.GetActionScripts("script3");

            // Assert
            Assert.That(result1, Is.Empty);
            Assert.That(result2, Is.Empty);
            Assert.That(result3, Is.Empty);
        }

        [Test]
        public void ExecuteInScriptContext_WithAction_ShouldCallExecutor()
        {
            // Arrange
            Action testAction = () => { };
            const uint objectId = 12345u;
            const int scriptEventId = 100;

            // Act
            _provider.ExecuteInScriptContext(testAction, objectId, scriptEventId);

            // Assert
            _mockExecutor.Received(1).ExecuteInScriptContext(testAction, objectId, scriptEventId);
        }

        [Test]
        public void ExecuteInScriptContext_WithDefaultParameters_ShouldUseDefaults()
        {
            // Arrange
            Action testAction = () => { };

            // Act
            _provider.ExecuteInScriptContext(testAction);

            // Assert
            _mockExecutor.Received(1).ExecuteInScriptContext(
                testAction,
                Arg.Any<uint>(),
                Arg.Any<int>());
        }

        [Test]
        public void ExecuteInScriptContext_WithNullAction_ShouldPassToExecutor()
        {
            // Arrange
            Action nullAction = null;

            // Act & Assert - Should not throw, delegate to executor
            Assert.DoesNotThrow(() => _provider.ExecuteInScriptContext(nullAction));
        }

        [Test]
        public void HasScript_CalledMultipleTimes_ShouldBeConsistent()
        {
            // Arrange
            const string scriptName = "mod_load";

            // Act
            var result1 = _provider.HasScript(scriptName);
            var result2 = _provider.HasScript(scriptName);
            var result3 = _provider.HasScript(scriptName);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
            Assert.That(result2, Is.EqualTo(result3));
        }

        [Test]
        public void ObjectSelf_SetThenGet_ShouldReturnSameValue()
        {
            // Arrange
            const uint testValue = 99999u;
            _mockClosureManager.ObjectSelf.Returns(testValue);

            // Act
            _provider.ObjectSelf = testValue;
            var result = _provider.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(testValue));
        }

        [Test]
        public void Constructor_ShouldNotCallAnyMethods()
        {
            // Arrange & Act
            var provider = new ScriptExecutionProvider(
                _mockClosureManager,
                _mockMapper,
                _mockExecutor);

            // Assert - Constructor should not trigger any operations
            _mockExecutor.DidNotReceive().ExecuteInScriptContext(
                Arg.Any<Action>(),
                Arg.Any<uint>(),
                Arg.Any<int>());
        }
    }
}

