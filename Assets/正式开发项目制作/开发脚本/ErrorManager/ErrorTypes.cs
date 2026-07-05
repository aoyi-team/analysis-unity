namespace ErrorManagement
{
    public enum ErrorLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum ErrorCategory
    {
        System,
        Network,
        Battle,
        UI,
        Auth,
        Resource,
        Other
    }

    public struct ErrorEntry
    {
        public ErrorLevel Level;
        public ErrorCategory Category;
        public string Message;
        public string StackTrace;
        public string Timestamp;

        public ErrorEntry(ErrorLevel level, ErrorCategory category, string message, string stackTrace = "")
        {
            Level = level;
            Category = category;
            Message = message;
            StackTrace = stackTrace;
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public override string ToString()
        {
            return $"[{Timestamp}] [{Level}] [{Category}] {Message}";
        }
    }
}
