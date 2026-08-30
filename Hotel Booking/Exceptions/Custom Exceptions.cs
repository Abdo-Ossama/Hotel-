namespace Hotel_Booking.Exceptions
{
   
    // abstract => no object
    public abstract class AppException : Exception
    {
        protected AppException(string message) : base(message) { }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message) { }
      
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.")
            : base(message) { }
    }

    public class ValidationAppException : AppException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationAppException(string message,IDictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
    }

   
    public class BusinessRuleException : AppException
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}