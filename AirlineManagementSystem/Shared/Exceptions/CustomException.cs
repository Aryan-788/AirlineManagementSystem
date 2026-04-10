using System;

namespace Shared.Exceptions
{
    public class CustomException : Exception
    {
        public int StatusCode { get; }

        public CustomException(string message, int statusCode = 500) : base(message)
        {
            StatusCode = statusCode;
        }

        public CustomException(string message, Exception inner, int statusCode = 500) : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}
