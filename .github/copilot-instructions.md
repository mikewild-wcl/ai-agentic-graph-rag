# Copilot Instructions
- Limit responses to one sentence especially for explanations.
- Projects in this repository should use the latest C# features.

  
# Team Best Practices
- always create files using file-scoped namespaces.
- When doing auth, always use modern .NET idioms & auth state is optional/should be set to false by default
- Prefer inline lambdas over full method bodies in C#.
- Prefer async and await over synchronous code.
- Never use CSS inline styles. Always use a CSS file.
- When creating Web API projects, prefer minimal APIs project.
- Constructor parameters must use camelCase (never PascalCase) even with primary constructor syntax; earlier deviations were corrected.


## Testing Guidelines
- use xUnit for unit tests
- use FluentAssertions version 7.2.0 for assertions
- Use Moq for mocking

