// -----------------------------------------------------------------------
// <copyright file="FakeRepositaryException.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace AV.Models.Repositary
{
    /// <summary>
    ///   Custom exception for fake repositary
    /// </summary>
    [Serializable]
    public class FakeRepositaryException : Exception
    {
        public FakeRepositaryException() {}

        public FakeRepositaryException(string message) : base(message) {}

        public FakeRepositaryException(string message, Exception innerException) :
            base(message, innerException) {}

        protected FakeRepositaryException(SerializationInfo info,
                                          StreamingContext context) : base(info, context) {}
    }
}