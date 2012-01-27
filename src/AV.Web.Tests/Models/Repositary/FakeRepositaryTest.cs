// -----------------------------------------------------------------------
// <copyright file="FakeRepositaryTest.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Web.Tests.Models.Repositary
{
    using System.Collections.Generic;
    using System.Linq;

    using AV.Models.Repositary;

    using FluentAssertions;

    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///   Test for fake repositary realization
    /// </summary>
    [TestClass]
    public class FakeRepositaryTest
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="FakeRepositaryTest" /> class.
        /// </summary>
        public FakeRepositaryTest()
        {
            IUnityContainer container = new UnityContainer();
            container.RegisterType<IRepositary<SimpleEntity>, FakeRepositary<SimpleEntity>>(
                new PerThreadLifetimeManager());
            container.RegisterType<IRepositary<AggregationEntity>, FakeRepositary<AggregationEntity>>(
                new PerThreadLifetimeManager());

            var serviceProvider = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => serviceProvider);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public FakeRepositary<AggregationEntity> AggregationEntitiesRepositary
        {
            get
            {
                return
                    ServiceLocator.Current.GetInstance<IRepositary<AggregationEntity>>() as
                    FakeRepositary<AggregationEntity>;
            }
        }

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public FakeRepositary<SimpleEntity> SimpleEntitiesRepositary
        {
            get
            {
                return ServiceLocator.Current.GetInstance<IRepositary<SimpleEntity>>() as FakeRepositary<SimpleEntity>;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Clears the memory buffer in repositary.
        /// </summary>
        [TestInitialize]
        public void ClearMemoryBuffer()
        {
            SimpleEntitiesRepositary.MemoryBuffer.Clear();
            AggregationEntitiesRepositary.MemoryBuffer.Clear();
        }

        /// <summary>
        ///   Clears the repositary.
        /// </summary>
        [TestMethod]
        public void ClearRepositary()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            SimpleEntitiesRepositary.Clear();
            SimpleEntitiesRepositary.MemoryBuffer.Should().BeEmpty();
        }

        /// <summary>
        ///   Save two connected entities (testing cascade save)
        /// </summary>
        [TestMethod]
        public void ConnectedEntitiesSave()
        {
            var aggregationEntity = new AggregationEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleEntity { Name = "Awesome string" };

            AggregationEntitiesRepositary.Save(aggregationEntity);

            AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity);
            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity.OneToOne);
        }

        /// <summary>
        ///   Delete one of two connected entities should work
        /// </summary>
        [TestMethod]
        public void DeleteConnectedEntities()
        {
            var aggregationEntity = new AggregationEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleEntity { Name = "Awesome string" };

            AggregationEntitiesRepositary.Save(aggregationEntity);
            SimpleEntitiesRepositary.Remove(aggregationEntity.OneToOne);

            AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity);
            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(0);
        }

        /// <summary>
        ///   Deletes the entity.
        /// </summary>
        [TestMethod]
        public void DeleteEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            SimpleEntitiesRepositary.Remove(entity);
            SimpleEntitiesRepositary.MemoryBuffer.Should().BeEmpty();
        }

        /// <summary>
        ///   Gets the saved entity.
        /// </summary>
        [TestMethod]
        public void GetSavedEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.Single(x => x.Id == 1).Should().Be(entity);
        }

        /// <summary>
        ///   LINQ query can be executed.
        /// </summary>
        [TestMethod]
        public void LINQCanBeExecuted()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            var result = (from e in SimpleEntitiesRepositary where e.Name.Contains("Awesome") select e).First();

            result.Should().Be(entity);
        }

        /// <summary>
        ///   Save many to many associations
        /// </summary>
        [TestMethod]
        public void ManyToManyAssociationSave()
        {
            var nodes = new List<AggregationEntity>();
            const int NodesCount = 10;
            for (int i = 0; i < NodesCount; i++)
                nodes.Add(new AggregationEntity { Name = string.Format("node {0}", i) });

            foreach (var from in nodes)
            {
                foreach (var to in nodes)
                {
                    from.ManyToManyTo.Add(to);
                    to.ManyToManyFrom.Add(from);
                }
            }

            nodes.ForEach(x => AggregationEntitiesRepositary.Save(x));

            AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(NodesCount);
            AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(nodes);
        }

        /// <summary>
        ///   Save one to many associations
        /// </summary>
        [TestMethod]
        public void OneTwoManyAssociationSave()
        {
            var parent = new AggregationEntity { Name = "Parent" };
            for (int i = 0; i < 10; i++)
            {
                var child = new AggregationEntity { Name = string.Format("child {0}", i), OneToMany = parent };
                parent.ManyToOne.Add(child);
            }

            AggregationEntitiesRepositary.Save(parent);

            AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(11);
            AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(parent);
            AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(parent.ManyToOne);
        }

        /// <summary>
        ///   Saves the entity.
        /// </summary>
        [TestMethod]
        public void SaveEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);
        }

        /// <summary>
        ///   Updates the enity.
        /// </summary>
        [TestMethod]
        public void UpdateEnity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            entity.Name = "На на на";
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);
        }

        /// <summary>
        ///   Check update operation is updating properties
        /// </summary>
        [TestMethod]
        public void UpdateEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);
            var entityToUpdate = new SimpleEntity { Id = entity.Id, Name = "Something else" };

            SimpleEntitiesRepositary.Update(entityToUpdate);

            entityToUpdate.Name.Should().Be(entity.Name);
        }

        #endregion

        /// <summary>
        ///   Sample entity to test aggregations
        /// </summary>
        public class AggregationEntity
        {
            #region Constructors and Destructors

            public AggregationEntity()
            {
                ManyToOne = new List<AggregationEntity>();
                ManyToManyFrom = new List<AggregationEntity>();
                ManyToManyTo = new List<AggregationEntity>();
            }

            #endregion

            #region Public Properties

            public int Id { get; set; }

            public ICollection<AggregationEntity> ManyToManyFrom { get; set; }

            public ICollection<AggregationEntity> ManyToManyTo { get; set; }

            public ICollection<AggregationEntity> ManyToOne { get; set; }

            public string Name { get; set; }

            public AggregationEntity OneToMany { get; set; }

            public SimpleEntity OneToOne { get; set; }

            #endregion
        }

        /// <summary>
        ///   Sample simple entity to test NHibernate repositary
        /// </summary>
        public class SimpleEntity
        {
            #region Public Properties

            /// <summary>
            ///   Gets or sets the id.
            /// </summary>
            /// <value> The identifier. </value>
            public virtual int Id { get; set; }

            /// <summary>
            ///   Gets or sets the name.
            /// </summary>
            /// <value> The simple name. </value>
            public virtual string Name { get; set; }

            #endregion

            #region Public Methods

            /// <summary>
            ///   Determines whether the specified <see cref="System.Object" /> is equal to this instance.
            /// </summary>
            /// <param name="obj"> The <see cref="System.Object" /> to compare with this instance. </param>
            /// <returns> <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c> . </returns>
            public override bool Equals(object obj)
            {
                if (obj is SimpleEntity)
                {
                    var entity = (SimpleEntity)obj;
                    return entity.Id == Id && entity.Name == Name;
                }

                return base.Equals(obj);
            }

            /// <summary>
            ///   Returns a hash code for this instance.
            /// </summary>
            /// <returns> A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. </returns>
            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            #endregion
        }
    }
}