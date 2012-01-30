// -----------------------------------------------------------------------
// <copyright file="EntityRepositaryTest.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Web.Tests.Database
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Linq;

    using AV.Database;
    using AV.Models;
    using AV.Web.Tests.Models.Repositary;

    using FluentAssertions;

    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityRepositaryTest
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="FakeRepositaryTest" /> class.
        /// </summary>
        public EntityRepositaryTest()
        {
            using (var connection = Database.DefaultConnectionFactory.CreateConnection("AV.Web.Tests.Database.SampleDb"))
            {
                if (Database.Exists(connection)) Database.Delete(connection);
            }

            IUnityContainer container = new UnityContainer();
            container.RegisterInstance(typeof(DbContext), new SampleDb());
            container.RegisterType<IRepositary<SimpleDbEntity>, EntityRepositary<SimpleDbEntity>>(
                new PerThreadLifetimeManager());
            container.RegisterType<IRepositary<AggregationDbEntity>, EntityRepositary<AggregationDbEntity>>(
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
        public EntityRepositary<AggregationDbEntity> AggregationEntitiesRepositary
        {
            get
            {
                return
                    ServiceLocator.Current.GetInstance<IRepositary<AggregationDbEntity>>() as
                    EntityRepositary<AggregationDbEntity>;
            }
        }

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public EntityRepositary<SimpleDbEntity> SimpleEntitiesRepositary
        {
            get
            {
                return ServiceLocator.Current.GetInstance<IRepositary<SimpleDbEntity>>() as EntityRepositary<SimpleDbEntity>;
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
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.SimpleEntities.ToList().ForEach(x => dbContext.SimpleEntities.Remove(x));
            dbContext.SaveChanges();

            dbContext.AggregationEntities.ToList().ForEach(x => dbContext.AggregationEntities.Remove(x));
            dbContext.SaveChanges();
        }

        /// <summary>
        ///   Save two connected entities (testing cascade save)
        /// </summary>
        [TestMethod]
        public void ConnectedEntitiesSave()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var aggregationEntity = new AggregationDbEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleDbEntity { Name = "Awesome string" };

            this.AggregationEntitiesRepositary.Save(aggregationEntity);

            dbContext.AggregationEntities.Should().HaveCount(1);
            dbContext.AggregationEntities.Should().Contain(x => x.Id == aggregationEntity.Id);
            dbContext.SimpleEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().Contain(x => x.Id == aggregationEntity.OneToOne.Id);
        }

        /// <summary>
        ///   Delete one of two connected entities should work
        /// </summary>
        [TestMethod]
        public void DeleteConnectedEntities()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var aggregationEntity = new AggregationDbEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleDbEntity { Name = "Awesome string" };

            this.AggregationEntitiesRepositary.Save(aggregationEntity);
            this.SimpleEntitiesRepositary.Remove(aggregationEntity.OneToOne);

            dbContext.AggregationEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().HaveCount(0);
        }

        /// <summary>
        ///   Deletes the entity.
        /// </summary>
        [TestMethod]
        public void DeleteEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            dbContext.SimpleEntities.Should().HaveCount(1);

            this.SimpleEntitiesRepositary.Remove(entity);
            dbContext.SimpleEntities.Should().HaveCount(0);
        }

        /// <summary>
        ///   Gets the saved entity.
        /// </summary>
        [TestMethod]
        public void GetSavedEntity()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.Single(x => x.Id == 1).Name.Should().Be("Awesome string");
        }

        /// <summary>
        ///   LINQ query can be executed.
        /// </summary>
        [TestMethod]
        public void LINQCanBeExecuted()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            var result = (from e in this.SimpleEntitiesRepositary where e.Name.Contains("Awesome") select e).First();

            result.Name.Should().Be("Awesome string");
        }

        /// <summary>
        ///   Save many to many associations
        /// </summary>
        [TestMethod]
        public void ManyToManyAssociationSave()
        {
            var nodes = new List<AggregationDbEntity>();
            const int NodesCount = 10;
            for (int i = 0; i < NodesCount; i++)
            {
                nodes.Add(new AggregationDbEntity { Name = string.Format("node {0}", i) });
            }

            foreach (var from in nodes)
            {
                foreach (var to in nodes)
                {
                    from.ManyToManyTo.Add(to);
                    to.ManyToManyFrom.Add(from);
                }
            }

            nodes.ForEach(x => this.AggregationEntitiesRepositary.Save(x));

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.AggregationEntities.Should().HaveCount(NodesCount);
            dbContext.AggregationEntities.Should().Contain(nodes);
        }

        /// <summary>
        ///   Save one to many associations
        /// </summary>
        [TestMethod]
        public void OneTwoManyAssociationSave()
        {
            var parent = new AggregationDbEntity { Name = "Parent" };
            for (int i = 0; i < 10; i++)
            {
                var child = new AggregationDbEntity { Name = string.Format("child {0}", i), OneToMany = parent };
                parent.ManyToOne.Add(child);
            }

            this.AggregationEntitiesRepositary.Save(parent);

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.AggregationEntities.Should().HaveCount(11);
            dbContext.AggregationEntities.Should().Contain(parent);
            dbContext.AggregationEntities.Should().Contain(parent.ManyToOne);
        }

        /// <summary>
        ///   Saves the entity.
        /// </summary>
        [TestMethod]
        public void SaveEntity()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.SimpleEntities.Should().Contain(x => x.Id == entity.Id);
        }

        /// <summary>
        ///   Updates the enity.
        /// </summary>
        [TestMethod]
        public void UpdateEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);
            dbContext.SimpleEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().Contain(x => x.Id == entity.Id);

            entity.Name = "На на на";
            this.SimpleEntitiesRepositary.Save(entity);

            dbContext.SimpleEntities.Should().HaveCount(1);
            var savedEntity = dbContext.SimpleEntities.Single(x => x.Id == entity.Id);
            savedEntity.Name.Should().Be("На на на");
        }

        /// <summary>
        ///   Check update operation is updating properties
        /// </summary>
        [TestMethod]
        public void ReloadEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);
            var entityToUpdate = new SimpleDbEntity { Id = entity.Id, Name = "Something else" };

            this.SimpleEntitiesRepositary.Update(entityToUpdate);

            entityToUpdate.Name.Should().Be("Awesome string");
        }

        #endregion
    }

    #region Sample Entities

    /// <summary>
    ///   Sample entity to test aggregations
    /// </summary>
    public class AggregationDbEntity : IEntity
    {
        #region Constructors and Destructors

        public AggregationDbEntity()
        {
            this.ManyToOne = new List<AggregationDbEntity>();
            this.ManyToManyFrom = new List<AggregationDbEntity>();
            this.ManyToManyTo = new List<AggregationDbEntity>();
        }

        #endregion

        #region Public Properties

        public long Id { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToManyFrom { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToManyTo { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToOne { get; set; }

        public string Name { get; set; }

        public virtual AggregationDbEntity OneToMany { get; set; }

        public virtual SimpleDbEntity OneToOne { get; set; }

        #endregion
    }

    /// <summary>
    ///   Sample simple entity to test NHibernate repositary
    /// </summary>
    public class SimpleDbEntity : IEntity
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets the id.
        /// </summary>
        /// <value> The identifier. </value>
        public long Id { get; set; }

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value> The simple name. </value>
        public string Name { get; set; }

        #endregion
    }

    public class SampleDb : DbContext
    {
        public DbSet<SimpleDbEntity> SimpleEntities { get; set; }

        public DbSet<AggregationDbEntity> AggregationEntities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new SimpleEntityConfiguration());
            modelBuilder.Configurations.Add(new AggregationEntityConfiguration());
        }
    }

    public class SimpleEntityConfiguration : EntityTypeConfiguration<SimpleDbEntity>
    {
        public SimpleEntityConfiguration()
        {
            this.HasKey(x => x.Id);
            Property(x => x.Name).IsRequired();
        }
    }

    public class AggregationEntityConfiguration : EntityTypeConfiguration<AggregationDbEntity>
    {
        public AggregationEntityConfiguration()
        {
            this.HasKey(x => x.Id);
            Property(x => x.Name);

            this.HasOptional(x => x.OneToOne).WithOptionalDependent().WillCascadeOnDelete();
            this.HasOptional(x => x.OneToMany).WithMany(x => x.ManyToOne).WillCascadeOnDelete();
            this.HasMany(x => x.ManyToManyFrom).WithMany(x => x.ManyToManyTo);
        }
    }

    #endregion
}