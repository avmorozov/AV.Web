// -----------------------------------------------------------------------
// <copyright file="ViewModelHelpersTest.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Web.Tests.Models.ViewModel
{
    using System;

    using AV.Models;
    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests viewmodel helper functions
    /// </summary>
    [TestClass]
    public class ViewModelHelpersTest
    {
        /// <summary>
        /// Test view model properties filling from 
        /// values in entity
        /// </summary>
        [TestMethod]
        public void SimplePropertyMappingFill()
        {
            var entity = new SampleEntity { Name = "Awesome name", Birthday = new DateTime(2000, 01, 02) };
            var viewmodel = new SampleViewModel();

            viewmodel.Fill(entity);

            viewmodel.Comment.Should().BeNullOrEmpty();
            viewmodel.Wedding.Should().Be(DateTime.MinValue);
            viewmodel.Name.Should().Be("Awesome name");
            viewmodel.Birthday.Should().Be(new DateTime(2000, 01, 02));
        }

        /// <summary>
        /// Test entity properties updating from 
        /// values in view model
        /// </summary>
        [TestMethod]
        public void SimplePropertyMappingUpdate()
        {
            var entity = new SampleEntity {Id = 1, Name = "Awesome name", Birthday = new DateTime(2000, 01, 02) };
            var viewmodel = new SampleViewModel { Name = "Awefull name", Birthday = new DateTime(2001, 02, 03) };

            viewmodel.UpdateProperties(entity);

            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Awefull name");
            entity.Birthday.Should().Be(new DateTime(2001, 02, 03));
        }
    }

    /// <summary>
    /// Simple View Model
    /// </summary>
    public class SampleViewModel :IViewModel
    {
        public string Name { get; set; }

        public string Comment { get; set; }

        public DateTime Birthday { get; set; }

        public DateTime Wedding { get; set; }
    }

    /// <summary>
    /// Simple entity
    /// </summary>
    public class SampleEntity : IEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public DateTime Birthday { get; set; }
    }
}