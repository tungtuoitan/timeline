# Validation Guide

## Table of Contents
- [Overview](#overview)
- [FluentValidation Setup](#fluentvalidation-setup)
- [Creating Validators](#creating-validators)
- [Validation Patterns](#validation-patterns)
- [Common Rules](#common-rules)
- [Custom Validators](#custom-validators)
- [Error Messages](#error-messages)
- [Validation Pipeline](#validation-pipeline)
- [Testing Validators](#testing-validators)
- [Best Practices](#best-practices)

---

## Overview

SuperApp uses **FluentValidation** for all input validation. This provides:
- Strongly-typed validation rules
- Separation of validation logic from business logic
- Consistent error messages
- Automatic integration with MediatR pipeline
- Easy unit testing

**Key Principle:** All commands and queries with user input MUST have validators.

---

## FluentValidation Setup

### Installation

FluentValidation is already configured in the project. If adding to a new project:

```bash
dotnet add package FluentValidation.AspNetCore
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### Registration in Program.cs

```csharp
// Register all validators from the Application assembly
builder.Services.AddValidatorsFromAssembly(typeof(CreateNoteValidator).Assembly);

// Add validation behavior to MediatR pipeline
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

### Validation Behavior

The `ValidationBehavior` automatically validates commands/queries before they reach handlers:

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

---

## Creating Validators

### Basic Validator Structure

Validators follow the naming convention: `{CommandName}Validator`

```csharp
using FluentValidation;
using SuperApp.Application.Features.Notes.Commands.CreateNote;

namespace SuperApp.Application.Features.Notes.Commands.CreateNote
{
    public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
    {
        public CreateNoteValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Note name is required")
                .MaximumLength(200).WithMessage("Note name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");
        }
    }
}
```

### File Organization

Place validators in the same folder as their command/query:

```
Application/
└── Features/
    └── Notes/
        └── Commands/
            └── CreateNote/
                ├── CreateNoteCommand.cs
                ├── CreateNoteCommandHandler.cs
                └── CreateNoteValidator.cs        ← Here
```

---

## Validation Patterns

### Required Fields

```csharp
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required");

// For strings, NotEmpty checks for null, empty, or whitespace
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Name is required");

// For nullable types
RuleFor(x => x.UserId)
    .NotNull().WithMessage("User ID is required")
    .GreaterThan(0).WithMessage("User ID must be positive");
```

### String Validation

```csharp
RuleFor(x => x.Name)
    .NotEmpty()
    .MinimumLength(3).WithMessage("Name must be at least 3 characters")
    .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(254); // RFC 5321 max email length

RuleFor(x => x.PhoneNumber)
    .NotEmpty()
    .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");

RuleFor(x => x.Url)
    .Must(BeAValidUrl).WithMessage("Invalid URL format")
    .When(x => !string.IsNullOrEmpty(x.Url));
```

### Numeric Validation

```csharp
RuleFor(x => x.Age)
    .GreaterThanOrEqualTo(0).WithMessage("Age cannot be negative")
    .LessThanOrEqualTo(150).WithMessage("Age must be realistic");

RuleFor(x => x.Price)
    .GreaterThan(0).WithMessage("Price must be positive")
    .ScalePrecision(2, 10).WithMessage("Price can have maximum 2 decimal places");

RuleFor(x => x.Quantity)
    .InclusiveBetween(1, 1000).WithMessage("Quantity must be between 1 and 1000");
```

### Date Validation

```csharp
RuleFor(x => x.BirthDate)
    .NotEmpty()
    .LessThan(DateTime.Now).WithMessage("Birth date must be in the past")
    .GreaterThan(DateTime.Now.AddYears(-150)).WithMessage("Birth date is too old");

RuleFor(x => x.StartDate)
    .LessThanOrEqualTo(x => x.EndDate)
    .WithMessage("Start date must be before or equal to end date")
    .When(x => x.EndDate.HasValue);

RuleFor(x => x.ExpiryDate)
    .GreaterThan(DateTime.Now).WithMessage("Expiry date must be in the future")
    .When(x => x.ExpiryDate.HasValue);
```

### Collection Validation

```csharp
RuleFor(x => x.Tags)
    .NotEmpty().WithMessage("At least one tag is required")
    .Must(tags => tags.Count <= 10).WithMessage("Maximum 10 tags allowed");

RuleFor(x => x.Emails)
    .NotEmpty().WithMessage("At least one email is required");

RuleForEach(x => x.Emails)
    .EmailAddress().WithMessage("All emails must be valid");

RuleFor(x => x.Categories)
    .Must(categories => categories.Distinct().Count() == categories.Count)
    .WithMessage("Duplicate categories are not allowed")
    .When(x => x.Categories != null && x.Categories.Any());
```

### Conditional Validation

```csharp
// Only validate when condition is met
RuleFor(x => x.CompanyName)
    .NotEmpty().WithMessage("Company name is required for business accounts")
    .When(x => x.AccountType == AccountType.Business);

// Validate unless condition is met
RuleFor(x => x.ParentId)
    .NotNull().WithMessage("Parent category is required for subcategories")
    .Unless(x => x.IsRootCategory);

// Complex conditions
RuleFor(x => x.DiscountCode)
    .NotEmpty().WithMessage("Discount code is required when applying discounts")
    .When(x => x.HasDiscount && x.DiscountAmount > 0);
```

### Dependent Validation

```csharp
// Validate one property based on another
RuleFor(x => x.ConfirmPassword)
    .Equal(x => x.Password).WithMessage("Passwords do not match")
    .When(x => !string.IsNullOrEmpty(x.Password));

// Cascade mode (stop on first failure)
RuleFor(x => x.Email)
    .Cascade(CascadeMode.Stop)
    .NotEmpty()
    .EmailAddress()
    .MustAsync(BeUniqueEmail).WithMessage("Email already exists");
```

---

## Common Rules

### Pre-built Rule Sets

Create reusable rule sets for common validations:

```csharp
public static class CommonValidationRules
{
    public static IRuleBuilderOptions<T, string> EmailRule<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(254).WithMessage("Email is too long");
    }

    public static IRuleBuilderOptions<T, string> PhoneRule<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format");
    }

    public static IRuleBuilderOptions<T, string> PasswordRule<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");
    }
}

// Usage
public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).EmailRule();
        RuleFor(x => x.Phone).PhoneRule();
        RuleFor(x => x.Password).PasswordRule();
    }
}
```

---

## Custom Validators

### Synchronous Custom Validation

```csharp
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Color)
            .Must(BeAValidColor).WithMessage("Invalid color format");
    }

    private bool BeAValidColor(string color)
    {
        if (string.IsNullOrEmpty(color))
            return true; // Allow empty if not required

        // Check hex color format
        return Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}
```

### Asynchronous Custom Validation

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(BeUniqueEmail).WithMessage("Email already exists");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MustAsync(BeUniqueUsername).WithMessage("Username already exists");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);
        return existingUser == null;
    }

    private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(username);
        return existingUser == null;
    }
}
```

### Complex Custom Validation

```csharp
public class UpdateNoteValidator : AbstractValidator<UpdateNoteCommand>
{
    private readonly INoteRepository _noteRepository;

    public UpdateNoteValidator(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;

        RuleFor(x => x)
            .MustAsync(HaveValidPermissions)
            .WithMessage("You don't have permission to update this note");
    }

    private async Task<bool> HaveValidPermissions(
        UpdateNoteCommand command,
        CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(command.NoteId);
        
        if (note == null)
            return false;

        // Check if user owns the note or is admin
        return note.UserId == command.UserId || command.IsAdmin;
    }
}
```

---

## Error Messages

### Default Messages

```csharp
RuleFor(x => x.Name)
    .NotEmpty(); // Uses default: "'Name' must not be empty."

RuleFor(x => x.Age)
    .GreaterThan(0); // Uses default: "'Age' must be greater than '0'."
```

### Custom Messages

```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("Please provide a note name");

RuleFor(x => x.Age)
    .GreaterThan(0).WithMessage("Age must be a positive number");
```

### Parameterized Messages

```csharp
RuleFor(x => x.Name)
    .MaximumLength(200)
    .WithMessage("Note name is too long. Maximum length is {MaxLength} characters, you entered {TotalLength}");

RuleFor(x => x.Price)
    .InclusiveBetween(1, 1000)
    .WithMessage("Price must be between {From} and {To}");
```

### Message with Property Values

```csharp
RuleFor(x => x.ConfirmEmail)
    .Equal(x => x.Email)
    .WithMessage((model, confirmEmail) => 
        $"Email confirmation '{confirmEmail}' does not match email '{model.Email}'");
```

### Localized Messages (Future Enhancement)

```csharp
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(x => Resources.NoteNameRequired)
            .MaximumLength(200).WithMessage(x => Resources.NoteNameTooLong);
    }
}
```

---

## Validation Pipeline

### How Validation Works

```
Request → Controller → MediatR → ValidationBehavior → Handler
                                       ↓
                                  Validates
                                       ↓
                         ValidationException (if fails)
                                       ↓
                            GlobalExceptionMiddleware
                                       ↓
                              400 Bad Request
```

### Validation Exception

```csharp
public class ValidationException : AppException
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(
                failureGroup => failureGroup.Key,
                failureGroup => failureGroup.ToArray()
            );
    }

    public IDictionary<string, string[]> Errors { get; }
}
```

### Error Response Format

```json
{
  "status": 400,
  "message": "One or more validation errors occurred",
  "errors": {
    "Name": [
      "Note name is required"
    ],
    "Description": [
      "Description cannot exceed 5000 characters"
    ],
    "Email": [
      "Invalid email format",
      "Email already exists"
    ]
  },
  "traceId": "00-abc123..."
}
```

---

## Testing Validators

### Unit Testing Validators

```csharp
using FluentValidation.TestHelper;
using Xunit;

public class CreateNoteValidatorTests
{
    private readonly CreateNoteValidator _validator;

    public CreateNoteValidatorTests()
    {
        _validator = new CreateNoteValidator();
    }

    [Fact]
    public void Should_HaveError_When_NameIsEmpty()
    {
        // Arrange
        var command = new CreateNoteCommand { Name = "", Description = "Test" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Note name is required");
    }

    [Fact]
    public void Should_HaveError_When_NameExceedsMaxLength()
    {
        // Arrange
        var command = new CreateNoteCommand 
        { 
            Name = new string('a', 201), 
            Description = "Test" 
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {
        // Arrange
        var command = new CreateNoteCommand 
        { 
            Name = "Valid Note", 
            Description = "Valid description" 
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    public void Should_HaveError_When_NameIsTooShort(string name)
    {
        // Arrange
        var command = new CreateNoteCommand { Name = name, Description = "Test" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
```

### Testing Async Validators

```csharp
public class CreateUserValidatorTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly CreateUserValidator _validator;

    public CreateUserValidatorTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _validator = new CreateUserValidator(_mockRepository.Object);
    }

    [Fact]
    public async Task Should_HaveError_When_EmailAlreadyExists()
    {
        // Arrange
        var existingUser = new User { Email = "test@example.com" };
        _mockRepository
            .Setup(r => r.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var command = new CreateUserCommand 
        { 
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email already exists");
    }

    [Fact]
    public async Task Should_NotHaveError_When_EmailIsUnique()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        var command = new CreateUserCommand 
        { 
            Email = "unique@example.com",
            Username = "newuser"
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
}
```

---

## Best Practices

### ✅ DO

1. **Create validators for all commands and queries with user input**
   ```csharp
   public class CreateNoteCommand : IRequest<NoteDto>
   {
       public string Name { get; init; }
       public string Description { get; init; }
   }
   
   // Always create corresponding validator
   public class CreateNoteValidator : AbstractValidator<CreateNoteCommand> { }
   ```

2. **Use meaningful error messages**
   ```csharp
   RuleFor(x => x.Email)
       .EmailAddress().WithMessage("Please enter a valid email address");
   ```

3. **Validate business rules in validators**
   ```csharp
   RuleFor(x => x.StartDate)
       .LessThan(x => x.EndDate)
       .WithMessage("Start date must be before end date");
   ```

4. **Use cascade mode for dependent validations**
   ```csharp
   RuleFor(x => x.Email)
       .Cascade(CascadeMode.Stop)
       .NotEmpty()
       .EmailAddress()
       .MustAsync(BeUniqueEmail);
   ```

5. **Test validators thoroughly**
   ```csharp
   [Theory]
   [InlineData("")]
   [InlineData(null)]
   [InlineData("   ")]
   public void Should_HaveError_When_NameIsEmpty(string name) { }
   ```

### ❌ DON'T

1. **Don't validate in controllers**
   ```csharp
   // ❌ Bad
   [HttpPost]
   public async Task<IActionResult> CreateNote(CreateNoteRequest request)
   {
       if (string.IsNullOrEmpty(request.Name))
           return BadRequest("Name is required");
       // ...
   }
   
   // ✅ Good - Let validator handle it
   [HttpPost]
   public async Task<IActionResult> CreateNote(CreateNoteRequest request)
   {
       var command = _mapper.Map<CreateNoteCommand>(request);
       var result = await _mediator.Send(command);
       return Ok(result);
   }
   ```

2. **Don't validate in handlers**
   ```csharp
   // ❌ Bad
   public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, NoteDto>
   {
       public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken ct)
       {
           if (string.IsNullOrEmpty(request.Name))
               throw new ValidationException("Name is required");
           // ...
       }
   }
   ```

3. **Don't use magic strings**
   ```csharp
   // ❌ Bad
   RuleFor(x => x.Name).MaximumLength(200);
   
   // ✅ Good
   public static class ValidationConstants
   {
       public const int NoteNameMaxLength = 200;
   }
   
   RuleFor(x => x.Name)
       .MaximumLength(ValidationConstants.NoteNameMaxLength);
   ```

4. **Don't swallow validation errors**
   ```csharp
   // ❌ Bad
   try
   {
       await _validator.ValidateAndThrowAsync(command);
   }
   catch (ValidationException)
   {
       // Silently ignore
   }
   ```

5. **Don't duplicate validation logic**
   ```csharp
   // ❌ Bad - Same validation in multiple validators
   public class CreateUserValidator : AbstractValidator<CreateUserCommand>
   {
       public CreateUserValidator()
       {
           RuleFor(x => x.Email)
               .NotEmpty()
               .EmailAddress()
               .MaximumLength(254);
       }
   }
   
   public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
   {
       public UpdateUserValidator()
       {
           RuleFor(x => x.Email)
               .NotEmpty()
               .EmailAddress()
               .MaximumLength(254); // Duplicated!
       }
   }
   
   // ✅ Good - Extract to reusable extension
   public static class CommonValidationRules
   {
       public static IRuleBuilderOptions<T, string> EmailRule<T>(
           this IRuleBuilder<T, string> ruleBuilder)
       {
           return ruleBuilder
               .NotEmpty()
               .EmailAddress()
               .MaximumLength(254);
       }
   }
   ```

---

## Validation Constants

Create a central location for validation constants:

```csharp
public static class ValidationConstants
{
    // String lengths
    public const int NoteNameMaxLength = 200;
    public const int NoteDescriptionMaxLength = 5000;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 20;
    public const int UserNameMaxLength = 50;
    
    // Numeric ranges
    public const int MinAge = 0;
    public const int MaxAge = 150;
    public const decimal MinPrice = 0.01m;
    public const decimal MaxPrice = 999999.99m;
    
    // Collection sizes
    public const int MaxTagsPerNote = 10;
    public const int MaxAttachmentsPerNote = 5;
    
    // Regex patterns
    public const string PhonePattern = @"^\+?[1-9]\d{1,14}$";
    public const string HexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
    public const string UsernamePattern = @"^[a-zA-Z0-9_-]{3,20}$";
    
    // Error messages
    public const string RequiredField = "{PropertyName} is required";
    public const string InvalidFormat = "{PropertyName} format is invalid";
    public const string TooLong = "{PropertyName} cannot exceed {MaxLength} characters";
}
```

---

## Quick Reference

### Common Validation Scenarios

| Scenario | Rule | Example |
|----------|------|---------|
| Required field | `NotEmpty()` | `RuleFor(x => x.Name).NotEmpty()` |
| Email | `EmailAddress()` | `RuleFor(x => x.Email).EmailAddress()` |
| Max length | `MaximumLength(n)` | `RuleFor(x => x.Name).MaximumLength(200)` |
| Min length | `MinimumLength(n)` | `RuleFor(x => x.Password).MinimumLength(8)` |
| Range | `InclusiveBetween(min, max)` | `RuleFor(x => x.Age).InclusiveBetween(0, 150)` |
| Positive number | `GreaterThan(0)` | `RuleFor(x => x.Price).GreaterThan(0)` |
| Regex | `Matches(pattern)` | `RuleFor(x => x.Phone).Matches(@"^\d{10}$")` |
| Enum | `IsInEnum()` | `RuleFor(x => x.Status).IsInEnum()` |
| Unique | `MustAsync(BeUnique)` | `RuleFor(x => x.Email).MustAsync(BeUnique)` |
| Conditional | `When(condition)` | `RuleFor(x => x.Tax).When(x => x.Amount > 100)` |

---

## Related Documentation

- **[Error Handling](ERROR_HANDLING.md)** - How validation errors are handled
- **[API Design](API_DESIGN.md)** - Request/response patterns with validation
- **[Testing Guidelines](TESTING.md)** - Testing validators and validation behavior
- **[Code Examples](CODE_EXAMPLES.md)** - Complete validation examples

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Maintained By:** Development Team