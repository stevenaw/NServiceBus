namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Base class to do an advance registration of a step.
    /// </summary>
    [DebuggerDisplay("{StepId}({BehaviorType.FullName}) - {Description}")]
    public abstract class RegisterStep
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterStep" /> class.
        /// </summary>
        /// <param name="stepId">The unique identifier for this steps.</param>
        /// <param name="behavior">The type of <see cref="Behavior{TContext}" /> to register.</param>
        /// <param name="description">A brief description of what this step does.</param>
        /// <param name="factoryMethod">A factory method for creating the behavior.</param>
        protected RegisterStep(string stepId, Type behavior, string description, Func<IServiceProvider, IBehavior> factoryMethod = null)
        {
            this.factoryMethod = factoryMethod;
            BehaviorTypeChecker.ThrowIfInvalid(behavior, "behavior");
            Guard.ThrowIfNullOrEmpty(stepId);
            Guard.ThrowIfNullOrEmpty(description);

            BehaviorType = behavior;
            StepId = stepId;
            Description = description;
        }

        /// <summary>
        /// Gets the unique identifier for this step.
        /// </summary>
        public string StepId { get; }

        /// <summary>
        /// Gets the description for this registration.
        /// </summary>
        public string Description { get; private set; }

        internal List<Dependency> Befores { get; private set; }
        internal List<Dependency> Afters { get; private set; }

        /// <summary>
        /// Gets the type of <see cref="Behavior{TContext}" /> that is being registered.
        /// </summary>
        public Type BehaviorType { get; private set; }

        internal void ApplyContainerRegistration(IServiceCollection serviceCollection)
        {
            if (factoryMethod != null)
            {
                return;
            }

            serviceCollection.AddTransient(BehaviorType);
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id" /> one. If the <paramref name="id" /> does
        /// not exist, this condition is ignored.
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert before.</param>
        public void InsertBeforeIfExists(string id)
        {
            Guard.ThrowIfNullOrEmpty(id);

            Befores ??= new List<Dependency>();

            Befores.Add(new Dependency(StepId, id, Dependency.DependencyDirection.Before, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step before the <paramref name="id" /> one.
        /// </summary>
        public void InsertBefore(string id)
        {
            Guard.ThrowIfNullOrEmpty(id);

            Befores ??= new List<Dependency>();

            Befores.Add(new Dependency(StepId, id, Dependency.DependencyDirection.Before, true));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id" /> one. If the <paramref name="id" /> does
        /// not exist, this condition is ignored.
        /// </summary>
        /// <param name="id">The unique identifier of the step that we want to insert after.</param>
        public void InsertAfterIfExists(string id)
        {
            Guard.ThrowIfNullOrEmpty(id);

            Afters ??= new List<Dependency>();

            Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, false));
        }

        /// <summary>
        /// Instructs the pipeline to register this step after the <paramref name="id" /> one.
        /// </summary>
        public void InsertAfter(string id)
        {
            Guard.ThrowIfNullOrEmpty(id);

            Afters ??= new List<Dependency>();

            Afters.Add(new Dependency(StepId, id, Dependency.DependencyDirection.After, true));
        }

        internal void Replace(ReplaceStep replacement)
        {
            if (StepId != replacement.ReplaceId)
            {
                throw new InvalidOperationException($"Cannot replace step '{StepId}' with '{replacement.ReplaceId}'. The ID of the replacement must match the replaced step.");
            }

            BehaviorType = replacement.BehaviorType;
            factoryMethod = replacement.FactoryMethod;

            if (!string.IsNullOrWhiteSpace(replacement.Description))
            {
                Description = replacement.Description;
            }
        }

        internal IBehavior CreateBehavior(IServiceProvider defaultBuilder)
        {
            var behavior = factoryMethod != null
                ? factoryMethod(defaultBuilder)
                : (IBehavior)defaultBuilder.GetRequiredService(BehaviorType);

            return behavior;
        }

        internal static RegisterStep Create(string pipelineStep, Type behavior, string description, Func<IServiceProvider, IBehavior> factoryMethod = null)
        {
            return new DefaultRegisterStep(behavior, pipelineStep, description, factoryMethod);
        }

        Func<IServiceProvider, IBehavior> factoryMethod;

        class DefaultRegisterStep : RegisterStep
        {
            public DefaultRegisterStep(Type behavior, string stepId, string description, Func<IServiceProvider, IBehavior> factoryMethod)
                : base(stepId, behavior, description, factoryMethod)
            {
            }
        }
    }
}