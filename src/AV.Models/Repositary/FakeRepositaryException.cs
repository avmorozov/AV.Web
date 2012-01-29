// -----------------------------------------------------------------------
// <copyright file="FakeRepositaryException.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace AV.Models.Repositary
{
    /// <summary>
    ///   Custom exception for fake repositary
    /// </summary>
    public class FakeRepositaryException : Exception
    {
        public FakeRepositaryException(string message) : base(message) {}
    }
}