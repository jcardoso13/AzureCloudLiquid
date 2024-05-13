namespace CloudLiquid.Azure.Exceptions
{
    public class FunctionException : Exception
    {
        public string Function { get; set; }

        public FunctionException(string function) 
        {
            this.Function = function;
        }

        public FunctionException(string message, string function):base(message) 
        {
            this.Function = function;
        }
    }
    public class DownloadBlobException(string message, string function) : FunctionException(message, function)
    {
    }

    public class ReadRequestException(string message, string function) : FunctionException(message, function)
    {
    }

    public class ParsingException(string message, string function) : FunctionException(message, function)
    {
    }

    public class RunTemplateException(string message, string function, string errorAction) : FunctionException(message, function)
    {
        public string ErrorAction { get; set; } = errorAction;
    }

    public class CreateResponseException(string message, string function) : FunctionException(message, function)
    {
    }
}
