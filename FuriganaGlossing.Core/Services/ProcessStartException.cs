using System;

namespace FuriganaGlossing.Services
{
    public sealed class ProcessStartException : Exception
    {
        public ProcessStartException(string message)
            : base(message)
        {
        }

        public ProcessStartException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
