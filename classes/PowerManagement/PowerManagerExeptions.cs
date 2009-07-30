using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;


namespace XviD4PSP
{
    /// <summary>
    /// The exception that is thrown when an error occures while doing power 
    /// management.
    /// </summary>
    [Serializable]
    public class PowerManagerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PowerManagerException class.
        /// </summary>
        public PowerManagerException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PowerManagerException class with a 
        /// specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PowerManagerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PowerManagerException class with a 
        /// specified error message and Inner Exception.
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The message that describes the innerException.
        /// </param>
        public PowerManagerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PowerManagerException class with a 
        /// specified SerializationInfo and context.
        /// </summary>
        /// <param name="info">
        /// Specifies the serialization information.
        /// </param>
        /// <param name="context">
        /// The message that describes the context of the exception.
        /// </param>
        protected PowerManagerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}