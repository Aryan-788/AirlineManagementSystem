using System;

namespace Shared.Exceptions
{
    public class NotFoundException : CustomException
    {
        public NotFoundException(string message) : base(message, 404)
        {
        }
    }
}
