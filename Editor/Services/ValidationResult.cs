namespace Merge2.SceneEditor.Editor
{
    public readonly struct ValidationResult
    {
        public ValidationResult(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public ValidationSeverity Severity { get; }
        public string Message { get; }
    }

    public enum ValidationSeverity
    {
        Pass,
        Warning,
        Error
    }
}
