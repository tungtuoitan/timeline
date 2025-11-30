# Validation Guide

## Overview

SuperApp sử dụng **Data Annotations** cho validation trong DTOs.

## Data Annotations

### Required Fields
```csharp
public class CreateNoteRequest
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }
}
```

### String Length
```csharp
[StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
public string Title { get; set; }

[MaxLength(5000)]
public string Content { get; set; }
```

### Email
```csharp
[Required]
[EmailAddress(ErrorMessage = "Invalid email format")]
public string Email { get; set; }
```

### Range
```csharp
[Range(1, int.MaxValue, ErrorMessage = "ID must be greater than 0")]
public int NoteId { get; set; }

[Range(0, 100)]
public int Priority { get; set; }
```

### RegularExpression
```csharp
[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone must be 10 digits")]
public string? Phone { get; set; }

[RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$")]
public string Password { get; set; }
```

### Compare
```csharp
public class SignupRequest
{
    [Required]
    public string Password { get; set; }

    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; }
}
```

### Custom Validation
```csharp
public class CreateNoteRequest : IValidatableObject
{
    public string Title { get; set; }
    public string Content { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title?.ToLower() == "untitled" && string.IsNullOrWhiteSpace(Content))
        {
            yield return new ValidationResult(
                "Cannot create untitled note without content",
                new[] { nameof(Title), nameof(Content) }
            );
        }
    }
}
```

## Validation Patterns

### Nullable Properties
```csharp
public string? OptionalField { get; set; }  // Nullable - optional

[Required]
public string RequiredField { get; set; }   // Non-nullable - required
```

### Conditional Validation
```csharp
public class UpdateNoteRequest
{
    public int NoteId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }  // Optional in update

    public string? Content { get; set; }  // Optional in update
}
```

### Collection Validation
```csharp
[Required]
[MinLength(1, ErrorMessage = "At least one tag is required")]
public List<int> TagIds { get; set; }
```

## Validation Response

**Automatic validation** (ASP.NET Core):
```csharp
[ApiController]  // Enables automatic 400 response
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNoteRequest request)
    {
        // If validation fails, returns 400 automatically
        // No need to check ModelState
        return Ok(await _service.CreateNoteAsync(request));
    }
}
```

**Validation Error Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required"],
    "Email": ["Invalid email format"]
  }
}
```

## Common Validation Rules

### Email
```csharp
[Required]
[EmailAddress]
[StringLength(100)]
public string Email { get; set; }
```

### Password
```csharp
[Required]
[StringLength(100, MinimumLength = 8)]
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
    ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
public string Password { get; set; }
```

### Phone
```csharp
[Phone]
[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone must be 10 digits")]
public string? PhoneNumber { get; set; }
```

### URL
```csharp
[Url]
public string? Website { get; set; }
```

## Business Validation (Services)

**Validation in service layer:**
```csharp
public async Task<NoteResponse> CreateNoteAsync(CreateNoteRequest request, string userEmail)
{
    // Business rule validation
    var noteCount = await _repository.GetUserNoteCountAsync(userEmail);
    if (noteCount >= 1000)
    {
        throw new BusinessRuleException("Maximum note limit reached (1000 notes)");
    }

    // Check duplicates
    var exists = await _repository.ExistsByTitleAsync(request.Title, userEmail);
    if (exists)
    {
        throw new ConflictException($"Note with title '{request.Title}' already exists");
    }

    // Create note
    var note = new Note
    {
        Title = request.Title,
        Content = request.Content,
        UserEmail = userEmail
    };

    await _repository.CreateAsync(note);
    return _mapper.Map<NoteResponse>(note);
}
```

## Validation Constants

**Centralized constants:**
```csharp
public static class ValidationConstants
{
    // Lengths
    public const int TitleMaxLength = 200;
    public const int ContentMaxLength = 5000;
    public const int EmailMaxLength = 100;
    public const int PasswordMinLength = 8;

    // Patterns
    public const string PhonePattern = @"^[0-9]{10}$";
    public const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$";

    // Messages
    public const string RequiredMessage = "{0} is required";
    public const string EmailMessage = "Invalid email format";
    public const string PasswordMessage = "Password must contain uppercase, lowercase, number and special character";
}
```

**Usage:**
```csharp
[StringLength(ValidationConstants.TitleMaxLength)]
public string Title { get; set; }
```

## Best Practices

### ✅ DO
- Use Data Annotations cho input validation
- Validate business rules trong services
- Use meaningful error messages
- Centralize validation constants
- Return 400 cho validation errors
- Return 422 cho business rule violations
- Test validation logic

### ❌ DON'T
- Skip validation
- Use magic strings/numbers
- Duplicate validation logic
- Validate in controllers
- Swallow validation errors
- Return 200 with errors

## Quick Reference

| Validation | Attribute | Example |
|------------|-----------|---------|
| Required | [Required] | `[Required] string Name` |
| String length | [StringLength] | `[StringLength(200)]` |
| Range | [Range] | `[Range(1, 100)]` |
| Email | [EmailAddress] | `[EmailAddress]` |
| Phone | [Phone] | `[Phone]` |
| URL | [Url] | `[Url]` |
| Regex | [RegularExpression] | `[RegularExpression(@"pattern")]` |
| Compare | [Compare] | `[Compare("Password")]` |

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** API_DESIGN.md, ERROR_HANDLING.md
