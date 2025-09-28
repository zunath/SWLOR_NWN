# SWLOR Test Shared

This project contains shared testing utilities and mock implementations for the SWLOR project.

## TestBase Class

The `TestBase` class provides centralized initialization of the mock NWScript service for all test projects. This eliminates the need to manually call `NWScript.SetService(new NWScriptServiceMock())` in every test class.

### Usage

1. **Inherit from TestBase** in your test class
2. **Call InitializeMockNWScript()** in your `[SetUp]` method
3. **Optionally use GetMockService()** to access mock data for verification

### Example

```csharp
using NUnit.Framework;
using SWLOR.Test.Shared;

namespace YourTestProject
{
    [TestFixture]
    public class YourServiceTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            // This single line initializes the mock NWScript service for all tests
            InitializeMockNWScript();
            
            // Your other test setup code goes here...
        }

        [Test]
        public void YourTest_WithMockNWScript_ShouldWork()
        {
            // The mock NWScript service is automatically available
            // You can use any NWScript methods and they will use the mock implementation
            
            // Example: Create a mock creature and test some functionality
            var mockService = GetMockService();
            
            // Set up mock data
            var creatureId = 123u;
            mockService.SetCreatureLevel(creatureId, 10);
            
            // Test your service that uses NWScript
            // YourService.DoSomething(creatureId);
            
            // Verify the mock was called correctly
            var level = mockService.GetCreatureLevel(creatureId);
            Assert.That(level, Is.EqualTo(10));
        }
    }
}
```

### Benefits

- **Centralized Setup**: No need to repeat `NWScript.SetService(new NWScriptServiceMock())` in every test class
- **Thread-Safe**: The initialization is thread-safe and idempotent
- **Consistent**: All test projects use the same mock implementation
- **Easy to Maintain**: Changes to the mock setup only need to be made in one place
- **Access to Mock Data**: Use `GetMockService()` to directly access and verify mock state

### Migration Guide

To migrate existing test classes:

1. **Add the using statement**:
   ```csharp
   using SWLOR.Test.Shared;
   ```

2. **Inherit from TestBase**:
   ```csharp
   public class YourServiceTests : TestBase
   ```

3. **Replace manual initialization**:
   ```csharp
   // OLD:
   NWScript.SetService(new NWScriptServiceMock());
   
   // NEW:
   InitializeMockNWScript();
   ```

4. **Remove the using statement** (if no longer needed):
   ```csharp
   // Remove this if you're not using it elsewhere:
   using SWLOR.NWN.API.Service;
   ```

### Available Methods

- `InitializeMockNWScript()`: Sets up the mock NWScript service (call in [SetUp])
- `GetMockService()`: Gets the mock service instance for direct access to mock data
- `ResetMockService()`: Resets the mock service state (if needed)

## Mock NWScript Service

The `NWScriptServiceMock` class provides a complete mock implementation of the `INWScriptService` interface. It includes:

- State tracking for all NWScript operations
- Helper methods for test verification
- Support for all NWScript function categories (Combat, Area, Item, etc.)

### Accessing Mock Data

Use `GetMockService()` to access the mock service and verify state:

```csharp
var mockService = GetMockService();

// Set up test data
mockService.SetCreatureLevel(creatureId, 15);
mockService.SetCreatureName(creatureId, "Test Creature");

// Verify state
Assert.That(mockService.GetCreatureLevel(creatureId), Is.EqualTo(15));
Assert.That(mockService.GetCreatureName(creatureId), Is.EqualTo("Test Creature"));
```

## Project Structure

- `TestBase.cs`: Base class for all test classes
- `NWScript/`: Mock implementations of NWScript functions
- `Examples/`: Example test classes demonstrating usage patterns

## Adding to New Test Projects

When creating new test projects:

1. **Add a reference** to `SWLOR.Test.Shared`
2. **Inherit from TestBase** in your test classes
3. **Call InitializeMockNWScript()** in your [SetUp] methods

This ensures all test projects use the same mock implementation consistently.
