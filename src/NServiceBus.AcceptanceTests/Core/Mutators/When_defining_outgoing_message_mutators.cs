﻿namespace NServiceBus.AcceptanceTests.Core.Mutators
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_defining_outgoing_message_mutators : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_applied_to_outgoing_messages()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new Message())))
                .Done(c => c.MessageProcessed)
                .Run();

            Assert.True(context.TransportMutatorCalled);
            Assert.True(context.OtherTransportMutatorCalled);
            Assert.True(context.MessageMutatorCalled);
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool TransportMutatorCalled { get; set; }
            public bool OtherTransportMutatorCalled { get; set; }
            public bool MessageMutatorCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((config, context) =>
                 {
                     config.RegisterMessageMutator(new TransportMutator(context));
                     config.RegisterMessageMutator(new OtherTransportMutator(context));
                     config.RegisterMessageMutator(new MessageMutator(context));
                 });
            }

            class TransportMutator : IMutateOutgoingTransportMessages
            {
                public TransportMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    testContext.TransportMutatorCalled = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }

            class OtherTransportMutator : IMutateOutgoingTransportMessages
            {
                public OtherTransportMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    testContext.OtherTransportMutatorCalled = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }

            class MessageMutator : IMutateOutgoingMessages
            {
                public MessageMutator(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task MutateOutgoing(MutateOutgoingMessageContext context)
                {
                    testContext.MessageMutatorCalled = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }

            class Handler : IHandleMessages<Message>
            {
                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    testContext.MessageProcessed = true;

                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class Message : ICommand
        {
        }
    }
}