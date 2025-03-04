namespace NServiceBus.ContainerTests
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_building_components
    {
        [Test]
        public void Singleton_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreEqual(serviceProvider.GetService(typeof(SingletonComponent)), serviceProvider.GetService(typeof(SingletonComponent)));
        }

        [Test]
        public void Transient_components_should_yield_unique_instances()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreNotEqual(serviceProvider.GetService<TransientComponent>(), serviceProvider.GetService<TransientComponent>());
        }

        [Test]
        public void Scoped_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var instance1 = serviceProvider.GetService(typeof(ScopedComponent));
            var instance2 = serviceProvider.GetService(typeof(ScopedComponent));

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Lambda_scoped_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var instance1 = serviceProvider.GetService(typeof(ScopedLambdaComponent));
            var instance2 = serviceProvider.GetService(typeof(ScopedLambdaComponent));

            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void Lambda_transient_components_should_yield_unique_instances()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreNotEqual(serviceProvider.GetService(typeof(TransientLambdaComponent)), serviceProvider.GetService(typeof(TransientLambdaComponent)));
        }

        [Test]
        public void Lambda_singleton_components_should_yield_the_same_instance()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.AreEqual(serviceProvider.GetService(typeof(SingletonLambdaComponent)), serviceProvider.GetService(typeof(SingletonLambdaComponent)));
        }

        [Test]
        public void Resolving_all_components_of_unregistered_types_should_give_empty_list()
        {
            var serviceCollection = new ServiceCollection();
            InitializeServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            Assert.IsEmpty(serviceProvider.GetServices(typeof(UnregisteredComponent)));
        }

        [Test]
        public void Resolving_recursive_types_does_not_stack_overflow()
        {
            try
            {
                var serviceCollection = new ServiceCollection();
                InitializeServices(serviceCollection);
                var serviceProvider = serviceCollection.BuildServiceProvider();
                serviceProvider.GetService(typeof(RecursiveComponent));
            }
            catch (Exception)
            {
                // this can't be a StackOverflowException as they can't be caught
            }
        }

        void InitializeServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(typeof(SingletonComponent));
            serviceCollection.AddTransient(typeof(TransientComponent));
            serviceCollection.AddScoped(typeof(ScopedComponent));
            serviceCollection.AddSingleton(_ => new SingletonLambdaComponent());
            serviceCollection.AddTransient(_ => new TransientLambdaComponent());
            serviceCollection.AddScoped(_ => new ScopedLambdaComponent());
            serviceCollection.AddSingleton(_ => new RecursiveComponent());
        }

        public class RecursiveComponent
        {
            public RecursiveComponent Instance { get; set; }
        }

        public class SingletonComponent
        {
        }

        public interface ISingletonComponentWithPropertyDependency
        {
        }

        public class SingletonComponentWithPropertyDependency : ISingletonComponentWithPropertyDependency
        {
            public SingletonComponent Dependency { get; set; }
        }

        public class TransientComponent
        {
        }

        public class UnregisteredComponent
        {
            public SingletonComponent SingletonComponent { get; set; }
        }

        public class SingletonLambdaComponent
        {
        }

        public class ScopedLambdaComponent
        {
        }

        public class TransientLambdaComponent
        {
        }
    }

    public class StaticFactory
    {
        public ComponentCreatedByFactory Create()
        {
            return new ComponentCreatedByFactory();
        }
    }

    public class ComponentCreatedByFactory
    {
    }

    public class ComponentWithBothConstructorAndSetterInjection
    {
        public ComponentWithBothConstructorAndSetterInjection(ConstructorDependency constructorDependency)
        {
            ConstructorDependency = constructorDependency;
        }

        public ConstructorDependency ConstructorDependency { get; }

        public SetterDependency SetterDependency { get; set; }
    }

    public class ConstructorDependency
    {
    }

    public class SetterDependency
    {
    }
}