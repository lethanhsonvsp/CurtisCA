namespace CANOpen.Models;

/// <summary>
/// Validation result container
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }
    
    public ValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }
    
    public static ValidationResult Success() => new(true, Array.Empty<string>());
    
    public static ValidationResult Failure(params string[] errors) => new(false, errors);
    
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors);
    
    public override string ToString()
    {
        if (IsValid)
            return "Valid";
        
        return $"Invalid: {string.Join(", ", Errors)}";
    }
}