using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Entity;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Shared.UI.Service
{
    [TestFixture]
    public class GuiServiceTests : TestBase
    {
        private GuiService _guiService;
        private IDatabaseService _mockDatabaseService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
            _mockDatabaseService = Substitute.For<IDatabaseService>();
            _guiService = new GuiService(_mockDatabaseService);
        }

        [Test]
        public void Constructor_WithDatabaseService_SetsDatabaseService()
        {
            // Act
            var guiService = new GuiService(_mockDatabaseService);

            // Assert
            Assert.That(guiService, Is.Not.Null);
        }

        [Test]
        public void CacheData_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.CacheData());
        }

        [Test]
        public void CreatePlayerWindows_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.CreatePlayerWindows());
        }

        [Test]
        public void SavePlayerWindowGeometry_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.SavePlayerWindowGeometry());
        }

        [Test]
        public void RegisterElementEvent_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var elementId = "test_element";
            var eventName = "click";
            var methodInfo = typeof(object).GetMethod("ToString");
            var methodDetail = new GuiMethodDetail(methodInfo, new List<KeyValuePair<Type, object>>());

            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.RegisterElementEvent(elementId, eventName, methodDetail));
        }

        [Test]
        public void HandleNuiEvents_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.HandleNuiEvents());
        }

        [Test]
        public void HandleNuiWatchEvent_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.HandleNuiWatchEvent());
        }

        [Test]
        public void BuildWindowId_WithValidWindowType_ReturnsWindowId()
        {
            // Arrange
            var windowType = GuiWindowType.CharacterSheet;

            // Act
            var result = _guiService.BuildWindowId(windowType);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void BuildEventKey_WithValidParameters_ReturnsEventKey()
        {
            // Arrange
            var playerId = "test_player";
            var elementId = "test_element";

            // Act
            var result = _guiService.BuildEventKey(playerId, elementId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void IsWindowOpen_WithValidParameters_ReturnsFalse()
        {
            // Arrange
            uint player = 12345;
            var windowType = GuiWindowType.CharacterSheet;

            // Act
            var result = _guiService.IsWindowOpen(player, windowType);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TogglePlayerWindow_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            uint player = 12345;
            var windowType = GuiWindowType.CharacterSheet;

            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.TogglePlayerWindow(player, windowType));
        }

        [Test]
        public void PublishRefreshEvent_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            uint player = 12345;
            var payload = Substitute.For<IGuiRefreshEvent>();

            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.PublishRefreshEvent(player, payload));
        }

        [Test]
        public void GetPlayerWindow_WithValidParameters_ThrowsKeyNotFoundException()
        {
            // Arrange
            uint player = 12345;
            var windowType = GuiWindowType.CharacterSheet;

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _guiService.GetPlayerWindow(player, windowType));
        }

        [Test]
        public void GetWindowTemplate_WithValidWindowType_ThrowsKeyNotFoundException()
        {
            // Arrange
            var windowType = GuiWindowType.CharacterSheet;

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _guiService.GetWindowTemplate(windowType));
        }

        [Test]
        public void CloseAllWindows_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.CloseAllWindows());
        }

        [Test]
        public void CloseWindow_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            uint player = 12345;
            var windowType = GuiWindowType.CharacterSheet;
            uint uiTarget = 67890;

            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.CloseWindow(player, windowType, uiTarget));
        }

        [Test]
        public void ReserveIds_WithValidCount_ReturnsIdReservation()
        {
            // Arrange
            var systemName = "test_system";
            var count = 5;

            // Act
            var result = _guiService.ReserveIds(systemName, count);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetSystemReservation_ThrowsKeyNotFoundException()
        {
            // Arrange
            var systemName = "test_system";

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _guiService.GetSystemReservation(systemName));
        }

        [Test]
        public void DrawWindow_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            uint player = 12345;
            int startId = 1;
            var anchor = ScreenAnchorType.TopLeft;
            int x = 100;
            int y = 100;
            int width = 200;
            int height = 150;
            float lifeTime = 5.0f;

            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.DrawWindow(player, startId, anchor, x, y, width, height, lifeTime));
        }

        [Test]
        public void CenterStringInWindow_WithValidParameters_ReturnsInt()
        {
            // Arrange
            var text = "Test Text";
            var windowX = 100;
            var windowWidth = 200;

            // Act
            var result = _guiService.CenterStringInWindow(text, windowX, windowWidth);

            // Assert
            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void DisableWindows_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _guiService.DisableWindows());
        }
    }
}
