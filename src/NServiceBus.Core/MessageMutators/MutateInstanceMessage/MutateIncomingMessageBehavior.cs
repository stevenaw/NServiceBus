﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MessageMutator;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;

    class MutateIncomingMessageBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public MutateIncomingMessageBehavior(HashSet<IMutateIncomingMessages> mutators)
        {
            this.mutators = mutators;
        }

        public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            if (hasIncomingMessageMutators)
            {
                return InvokeIncomingMessageMutators(context, next);
            }

            return next(context);
        }

        async Task InvokeIncomingMessageMutators(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            var logicalMessage = context.Message;
            var current = logicalMessage.Instance;

            var mutatorContext = new MutateIncomingMessageContext(current, context.Headers, context.CancellationToken);

            var hasMutators = false;

            foreach (var mutator in context.Builder.GetServices<IMutateIncomingMessages>())
            {
                hasMutators = true;

                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            foreach (var mutator in mutators)
            {
                hasMutators = true;

                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            hasIncomingMessageMutators = hasMutators;

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.Message);
            }

            await next(context).ConfigureAwait(false);
        }

        volatile bool hasIncomingMessageMutators = true;
        readonly HashSet<IMutateIncomingMessages> mutators;
    }
}