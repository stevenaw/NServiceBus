namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using MessageInterfaces;
    using NServiceBus.Serialization;
    using NUnit.Framework;
    using Settings;

    public class When_registering_deserializers_with_settings : NServiceBusAcceptanceTest
    {
        const string Value1 = "SomeFancySettingsForMainSerializer";
        const string Value2 = "SomeFancySettingsForDeserializer";

        [Test]
        public async Task Should_not_override_serializer_settings()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<XmlCustomSerializationReceiver>(b => b.When(
                    (session, c) =>
                    {
                        var sendOptions = new SendOptions();
                        sendOptions.SetHeader("ContentType", "MyCustomSerializer");
                        return session.SendLocal(new MyRequest());
                    }))
                .Done(c => c.DeserializeCalled)
                .Run();

            Assert.True(context.HandlerGotTheRequest);
            Assert.True(context.SerializeCalled);
            Assert.True(context.DeserializeCalled);
            Assert.AreEqual(Value1, context.ValueFromSettingsForMainSerializer);
            Assert.AreEqual(Value2, context.ValueFromSettingsForDeserializer);
        }

        public class Context : ScenarioContext
        {
            public bool HandlerGotTheRequest { get; set; }
            public bool SerializeCalled { get; set; }
            public bool DeserializeCalled { get; set; }
            public string ValueFromSettingsForMainSerializer { get; set; }
            public string ValueFromSettingsForDeserializer { get; set; }
        }

        class XmlCustomSerializationReceiver : EndpointConfigurationBuilder
        {
            public XmlCustomSerializationReceiver()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<MyCustomSerializer>().Settings(Value1, (Context)r.ScenarioContext);
                    c.AddDeserializer<MyCustomSerializer>().Settings(Value2, (Context)r.ScenarioContext);
                });
            }

            class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public MyRequestHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    testContext.HandlerGotTheRequest = true;
                    return Task.CompletedTask;
                }

                Context testContext;
            }
        }

        public class MyRequest : IMessage
        {
        }

        public class MyCustomSerializer : SerializationDefinition
        {
            public override Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings)
            {
                return mapper => new MyCustomMessageSerializer(settings.GetOrDefault<string>("MyCustomSerializer.Settings"), settings.Get<Context>());
            }
        }

        class MyCustomMessageSerializer : IMessageSerializer
        {
            string valueFromSettings;
            Context context;

            public MyCustomMessageSerializer(string valueFromSettings, Context context)
            {
                this.valueFromSettings = valueFromSettings;
                this.context = context;
            }

            public void Serialize(object message, Stream stream)
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(MyRequest));

                context.SerializeCalled = true;
                context.ValueFromSettingsForMainSerializer = valueFromSettings;

                serializer.Serialize(stream, message);
            }

            public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null)
            {
                using (var stream = new MemoryStream(body.ToArray()))
                {
                    var serializer = new XmlSerializer(typeof(MyRequest));

                    var msg = serializer.Deserialize(stream);
                    context.DeserializeCalled = true;
                    context.ValueFromSettingsForDeserializer = valueFromSettings;

                    return new[]
                    {
                        msg
                    };
                }
            }

            public string ContentType => "MyCustomSerializer";
        }
    }

    static class CustomSettingsForMyCustomSerializer2
    {
        public static SerializationExtensions<When_registering_deserializers_with_settings.MyCustomSerializer> Settings(this SerializationExtensions<When_registering_deserializers_with_settings.MyCustomSerializer> extensions, string valueFromSettings, When_registering_deserializers_with_settings.Context context)
        {
            var settings = extensions.GetSettings();
            settings.Set("MyCustomSerializer.Settings", valueFromSettings);
            settings.Set(context);
            return extensions;
        }
    }
}