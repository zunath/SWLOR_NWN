# LanguageBuilder

## Overview
The LanguageBuilder generates randomized character substitution code for language systems in the SWLOR game. It creates C# switch statements that map characters to substitute characters for creating fictional languages.

## Command
```bash
SWLOR.CLI.exe -l
# or
SWLOR.CLI.exe --language
```

## Functionality
The LanguageBuilder creates randomized language substitution rules:

### 1. Character Base Reference
Uses the standard English alphabet (a-z) as the base character set for substitution.

### 2. Randomization Process
The tool performs several randomization steps:
- **Random Duplicates**: Adds 0-9 random duplicate characters to the substitution set
- **Random Removals**: Removes 0-4 random characters from the substitution set
- **Apostrophe Addition**: 50% chance to add an apostrophe character
- **Character Mapping**: Maps each base character to a random substitute character

### 3. Generated Output
Creates C# switch statements for both lowercase and uppercase characters:
```csharp
case 'a': sb.Append("x"); break;
case 'A': sb.Append("X"); break;
```

## Algorithm Details

### Character Processing
1. **Base Set**: Starts with a-z alphabet
2. **Duplication**: Adds random duplicates (0-9 characters)
3. **Removal**: Removes random characters (0-4 characters)
4. **Apostrophe**: 50% chance to add apostrophe
5. **Mapping**: Each base character gets mapped to a random substitute

### Special Features
- **Double Character Mapping**: 10% chance to map a character to two substitute characters
- **Case Handling**: Generates both lowercase and uppercase mappings
- **Apostrophe Support**: Includes apostrophe character in substitution set when enabled

## Output Format
The tool outputs C# code in the following format:
```csharp
case 'a': sb.Append("x"); break;
case 'A': sb.Append("X"); break;

case 'b': sb.Append("m"); break;
case 'B': sb.Append("M"); break;
// ... continues for all characters
```

## Usage
This tool is used during development to:
- Generate randomized language substitution rules
- Create fictional languages for roleplay purposes
- Implement language barriers in the game
- Add variety to communication systems

## Interactive Behavior
- The tool waits for a key press after generating the language code
- Output is displayed to the console
- Generated code can be copied and pasted into the game's language system

## Notes
- Each run produces different randomization results
- The tool is designed for one-time language generation
- Generated code should be integrated into the game's language processing system
- The randomization ensures unique language mappings for each generation 