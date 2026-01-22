# Contributing to WebP Scanner

Thank you for your interest in contributing to WebP Scanner! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Pull Request Process](#pull-request-process)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment. Please:

- Be respectful of differing viewpoints and experiences
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (optional, for integration testing)
- Git

### Development Environment

1. **Fork the repository** on GitHub

2. **Clone your fork:**
   ```bash
   git clone https://github.com/YOUR-USERNAME/App-WebP-Image-Scanner.git
   cd App-WebP-Image-Scanner
   ```

3. **Add upstream remote:**
   ```bash
   git remote add upstream https://github.com/csmashe/App-WebP-Image-Scanner.git
   ```

4. **Create a feature branch:**
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Backend (.NET)

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API (from src/WebPScanner.Api)
cd src/WebPScanner.Api
dotnet run
```

### Frontend (React)

```bash
# Navigate to frontend directory
cd src/WebPScanner.Web

# Install dependencies
npm install

# Start development server
npm run dev

# Run linter
npm run lint

# Build for production
npm run build
```

### Running Everything Together

### Option 1: Docker

```bash
docker-compose up --build
```

### Option 2: Local Development

Terminal 1 (API):
```bash
cd src/WebPScanner.Api
dotnet run
```

Terminal 2 (Frontend):
```bash
cd src/WebPScanner.Web
npm run dev
```

## Making Changes

### Branching Strategy

- `main` - Production-ready code
- `feature/*` - New features
- `fix/*` - Bug fixes
- `docs/*` - Documentation changes

### Commit Messages

Follow conventional commit format:

```text
type(scope): short description

Longer description if needed.

Closes #123
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `style` - Code style (formatting, etc.)
- `refactor` - Code refactoring
- `test` - Adding tests
- `chore` - Maintenance tasks

**Examples:**
```text
feat(crawler): add support for custom user agents
fix(email): handle SendGrid rate limiting correctly
docs(readme): update installation instructions
test(queue): add fairness algorithm edge case tests
```

### Keep Changes Focused

- One feature or fix per pull request
- Keep commits atomic and focused
- Avoid unrelated changes in the same PR

## Pull Request Process

1. **Update your branch** with the latest upstream changes:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run all tests** and ensure they pass:
   ```bash
   dotnet test
   cd src/WebPScanner.Web && npm run lint
   ```

3. **Push your changes:**
   ```bash
   git push origin feature/your-feature-name
   ```

4. **Create a Pull Request** on GitHub with:
   - Clear title describing the change
   - Description of what and why
   - Reference to related issues
   - Screenshots for UI changes
   - Note: For PRs from forks, CI runs on our self-hosted runner only after a maintainer applies the `ok-to-test` label

5. **Address review feedback** promptly

6. **Squash commits** if requested before merge

### PR Checklist

- [ ] Tests pass locally
- [ ] New code has test coverage
- [ ] Documentation updated if needed
- [ ] No new warnings or errors
- [ ] Commit messages follow conventions
- [ ] PR description is complete

## Coding Standards

### C# (.NET)

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods small and focused
- Use async/await for I/O operations
- Add XML documentation for public APIs

**Example:**
```csharp
/// <summary>
/// Validates the provided URL for scanning.
/// </summary>
/// <param name="url">The URL to validate.</param>
/// <returns>A validation result indicating success or failure.</returns>
public async Task<ValidationResult> ValidateUrlAsync(string url)
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return ValidationResult.Failure("URL is required");
    }

    // Implementation...
}
```

### TypeScript/React

- Use functional components with hooks
- Prefer TypeScript strict mode
- Use meaningful component and prop names
- Keep components small and focused
- Use Tailwind CSS for styling

**Example:**
```tsx
interface ScanFormProps {
  onSubmit: (url: string, email: string) => Promise<void>;
  isLoading: boolean;
}

export function ScanForm({ onSubmit, isLoading }: ScanFormProps) {
  const [url, setUrl] = useState('');
  const [email, setEmail] = useState('');

  // Implementation...
}
```

### File Organization

```text
src/
├── WebPScanner.Api/          # ASP.NET Core API
│   ├── Endpoints/             # FastEndpoints API endpoints
│   ├── Hubs/                  # SignalR hubs
│   ├── Middleware/            # Custom middleware
│   └── Services/              # API-specific services
├── WebPScanner.Core/         # Business logic
│   ├── Configuration/        # Options classes
│   ├── DTOs/                  # Data transfer objects
│   ├── Entities/              # Domain entities
│   ├── Interfaces/            # Service interfaces
│   ├── Models/                # Domain models
│   └── Services/              # Core services
├── WebPScanner.Data/         # Data access
│   ├── Context/               # EF Core DbContext
│   ├── Migrations/            # Database migrations
│   └── Repositories/          # Repository implementations
└── WebPScanner.Web/          # React frontend
    ├── src/
    │   ├── components/        # React components
    │   ├── hooks/             # Custom hooks
    │   ├── lib/               # Utilities
    │   ├── store/             # State management
    │   └── types/             # TypeScript types
    └── public/                # Static assets
```

## Testing Guidelines

### Backend Testing

- **Unit tests** for business logic (use xUnit)
- **Integration tests** for API endpoints
- **Mock external dependencies** (Puppeteer, SendGrid)
- **Test edge cases** and error conditions

**Example:**
```csharp
public class ValidationServiceTests
{
    private readonly ValidationService _service;

    public ValidationServiceTests()
    {
        _service = new ValidationService();
    }

    [Fact]
    public async Task ValidateUrl_WithValidHttpsUrl_ReturnsSuccess()
    {
        var result = await _service.ValidateUrlAsync("https://example.com");

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://192.168.1.1")]
    public async Task ValidateUrl_WithPrivateIp_ReturnsSsrfError(string url)
    {
        var result = await _service.ValidateUrlAsync(url);

        Assert.False(result.IsValid);
        Assert.Contains("SSRF", result.ErrorMessage);
    }
}
```

### Frontend Testing

For significant frontend changes, consider adding:
- Component tests with React Testing Library
- E2E tests with Playwright

### Running Tests

```bash
# All tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Specific project
dotnet test tests/WebPScanner.Core.Tests
```

## Documentation

### When to Update Docs

- New features require README updates
- Configuration changes require CONFIGURATION.md updates
- New common issues require TROUBLESHOOTING.md updates
- API changes require inline documentation

### Documentation Style

- Use clear, concise language
- Include code examples
- Keep formatting consistent
- Test all code examples

## Questions?

- Open an issue for feature discussions
- Use discussions for general questions
- Check existing issues before creating new ones

Thank you for contributing!
