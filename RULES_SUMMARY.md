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

## ✅ REQUIRED PATTERNS
- ✅ Separate event handlers from services
- ✅ Use dependency injection
- ✅ Register services as singletons
- ✅ Use NSubstitute + NUnit for testing
- ✅ Store data in config/database, not hardcoded
- ✅ Use proper using statement organization

## 📋 PRE-COMMIT CHECKLIST
- [ ] No cross-project dependencies
- [ ] No hardcoded values
- [ ] No regular async/await
- [ ] Services as singletons
- [ ] Event handlers separated
- [ ] Using statements correct
- [ ] Tests use NSubstitute + NUnit

**Full rules: See `AI_AGENT_RULES.md`**
