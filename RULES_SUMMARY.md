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

## 🧪 TESTING FRAMEWORK - CRITICAL FOR AI AGENTS

### How Testing Works in SWLOR
1. **TestBase.InitializeMockNWScript()** automatically replaces ALL NWScript and NWNX services with mocks
2. **Use direct NWScript static calls** in tests (e.g., `NWScript.CreateArea()`)
3. **Mock other services** with NSubstitute (database, cache, etc.)
4. **NEVER** call `GetMockService()` or create manual NWScript mocks

### Example Test Pattern
```csharp
[TestFixture]
public class MyServiceTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        InitializeMockNWScript(); // Handles ALL NWScript/NWNX mocking
        
        _mockDatabaseService = Substitute.For<IDatabaseService>();
        _service = new MyService(_mockDatabaseService);
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
