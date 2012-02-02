// -----------------------------------------------------------------------
// <copyright file="EntityRepositaryException.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Database
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    ///   Custom entity repositary exception
    /// </summary>
    public class EntityRepositaryException : Exception
    {
        public EntityRepositaryException()
        {
        }

        public EntityRepositaryException(string message)
            : base(message)
        {
        }

        public EntityRepositaryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected EntityRepositaryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}