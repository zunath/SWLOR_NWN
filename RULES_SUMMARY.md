# AI Agent Rules Summary

## ⚠️ MANDATORY: Read AI_AGENT_RULES.md Before Any Changes

This is a summary of the most critical rules. For complete details, see `AI_AGENT_RULES.md`.

## 🚨 CRITICAL DEPENDENCY RULES (NON-NEGOTIABLE)
1. **Shared projects MUST NEVER reference Component projects**
2. **Component projects MUST NEVER reference other Component projects**  
3. **Shared and Component projects MUST NEVER reference Test projects**

## 🚫 FORBIDDEN PATTERNS
- ❌ Regular C# async/await (use SWLOR.Shared.Core.Async only)
- ❌ Hardcoded data values (use config/database/enums)
- ❌ Cross-component dependencies
- ❌ Event handlers mixed with business logic
- ❌ Services not registered as singletons
- ❌ Using Moq instead of NSubstitute
- ❌ Using MSTest/xUnit instead of NUnit
- ❌ Manual NWScript/NWNX mocking (TestBase handles automatically)
- ❌ Calling GetMockService() in tests
- ❌ Creating manual NWScript mocks
- ❌ Modifying TestBase class or adding methods to it
- ❌ Adding `using SWLOR.NWN.API.Service;` for NWScript calls (NWScript is global)
- ❌ Using static plugin classes (AdministrationPlugin, AreaPlugin, etc.)
- ❌ Static calls to NWNX plugin services

## ✅ REQUIRED PATTERNS
- ✅ Separate event handlers from services
- ✅ Use dependency injection
- ✅ Register services as singletons
- ✅ Use NSubstitute + NUnit for testing
- ✅ Store data in config/database, not hardcoded
- ✅ Use proper using statement organization
- ✅ Use TestBase.InitializeMockNWScript() for automatic mocking
- ✅ Use direct NWScript static calls in tests
- ✅ Mock only non-NWScript services with NSubstitute
- ✅ Use NWScript without adding using statements (it's global)
- ✅ Inject NWNX plugin services through constructor
- ✅ Use service interfaces (IAdministrationPluginService, etc.)
- ✅ Mock NWNX plugin services with NSubstitute in tests

## 📋 PRE-COMMIT CHECKLIST
- [ ] No cross-project dependencies
- [ ] No hardcoded values
- [ ] No regular async/await
- [ ] Services as singletons
- [ ] Event handlers separated
- [ ] Using statements correct
- [ ] Tests use NSubstitute + NUnit
- [ ] Tests inherit from TestBase
- [ ] Tests call InitializeMockNWScript()
- [ ] No manual NWScript/NWNX mocking
- [ ] TestBase class not modified
- [ ] No static plugin class usage
- [ ] NWNX plugin services injected via constructor
- [ ] Plugin services mocked with NSubstitute in tests

## 🧪 TESTING FRAMEWORK - CRITICAL FOR AI AGENTS

### How Testing Works in SWLOR
1. **TestBase.InitializeMockNWScript()** automatically replaces NWScript with mocks
2. **Use direct NWScript static calls** in tests (e.g., `NWScript.CreateArea()`)
3. **Mock other services** with NSubstitute (database, cache, NWNX plugin services, etc.)
4. **NEVER** call `GetMockService()` or create manual NWScript mocks
5. **IMPORTANT**: NWNX plugin services must be mocked with NSubstitute (static classes no longer exist)

### Example Test Pattern
```csharp
[TestFixture]
public class MyServiceTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        InitializeMockNWScript(); // Handles NWScript mocking
        
        // Mock other services with NSubstitute
        _mockDatabaseService = Substitute.For<IDatabaseService>();
        _mockAdministrationPlugin = Substitute.For<IAdministrationPluginService>();
        
        _service = new MyService(_mockDatabaseService, _mockAdministrationPlugin);
    }
    
    [Test]
    public void MyTest()
    {
        // Use NWScript directly - mocking is automatic
        var area = NWScript.CreateArea("", "", "Test Area");
        NWScript.SetName(area, "Test Area");
        
        _service.ProcessArea(area);
        
        var name = NWScript.GetName(area);
        Assert.That(name, Is.EqualTo("Test Area"));
    }
}
```

**Full rules: See `AI_AGENT_RULES.md`**
