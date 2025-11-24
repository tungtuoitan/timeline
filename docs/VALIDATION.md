# Validation Guide Summary

## Overview
- SuperApp uses FluentValidation for input validation: strongly-typed rules, separation from business logic, consistent errors, MediatR integration, easy testing.
- Key: All commands/queries with user input MUST have validators.

## FluentValidation Setup
- Installed: FluentValidation.AspNetCore, FluentValidation.DependencyInjectionExtensions.
- Registration: AddValidatorsFromAssembly in Program.cs; Add ValidationBehavior to MediatR pipeline.
- ValidationBehavior: Validates before handler; throws ValidationException on failures.

## Creating Validators
- Naming: {CommandName}Validator.
- Structure: Inherit AbstractValidator<T>; define rules in constructor.
- File: Same folder as command/query.

## Validation Patterns
### Required Fields
- NotEmpty() for strings; NotNull() for nullable types.

### String Validation
- MinimumLength(n), MaximumLength(n), EmailAddress(), Matches(regex).

### Numeric Validation
- GreaterThan/OrEqualTo, LessThan/OrEqualTo, InclusiveBetween(min,max), ScalePrecision(decimals,totalDigits).

### Date Validation
- LessThan(date), GreaterThan(date), LessThanOrEqualTo(x => otherDate).

### Collection Validation
- NotEmpty(), Must(condition for count/distinct), RuleForEach(rule).

### Conditional Validation
- When(condition), Unless(condition).

### Dependent Validation
- Equal(x => otherProp), Cascade(CascadeMode.Stop).

## Common Rules
- Create reusable extensions: e.g., EmailRule, PhoneRule, PasswordRule with chained rules.

## Custom Validators
- Sync: Must(BeValidFunc).
- Async: MustAsync(BeUniqueAsync).
- Complex: MustAsync on whole object; inject repositories for DB checks.

## Error Messages
- Default or custom WithMessage("msg").
- Parameterized: Use {MaxLength}, {PropertyName}.
- With property values: WithMessage((model, val) => $"msg with {val}").

## Validation Pipeline
- Flow: Request → Controller → MediatR → ValidationBehavior → Handler.
- Exception: ValidationException with Errors dictionary.
- Response: 400 JSON with status, message, errors dict.

## Testing Validators
- Use TestHelper: TestValidate, ShouldHaveValidationErrorFor, ShouldNotHaveAnyValidationErrors.
- For async: TestValidateAsync; mock repositories.
- Test cases: Empty, invalid lengths, duplicates, etc.; use Theory for params.

## Best Practices
### DO
- Create validators for all input commands/queries.
- Use meaningful/parameterized messages.
- Validate business rules in validators.
- Use Cascade.Stop for efficiency.
- Test thoroughly with edge cases.

### DON'T
- Validate in controllers or handlers.
- Use magic strings (use constants).
- Swallow errors.
- Duplicate logic (use extensions).

## Validation Constants
- Central class: Lengths (e.g., NoteNameMaxLength=200), Ranges (MinAge=0), Patterns (PhonePattern), Messages (RequiredField="{PropertyName} is required").

## Quick Reference
| Scenario | Rule |
|----------|------|
| Required | NotEmpty() |
| Email | EmailAddress() |
| Length | Maximum/MinimumLength(n) |
| Range | InclusiveBetween(min,max) |
| Positive | GreaterThan(0) |
| Regex | Matches(pattern) |
| Enum | IsInEnum() |
| Unique | MustAsync(BeUnique) |
| Conditional | When(condition) |

## Related Documentation
- Error Handling, API Design, Testing Guidelines, Code Examples.

**Version:** 1.0 (Oct 2025)  
**Maintained By:** Dev Team