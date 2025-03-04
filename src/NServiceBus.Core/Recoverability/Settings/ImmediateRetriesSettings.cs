namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration.AdvancedExtensibility;
    using Faults;
    using Settings;

    /// <summary>
    /// Configuration settings for Immediate Retries.
    /// </summary>
    public partial class ImmediateRetriesSettings : ExposeSettings
    {
        internal ImmediateRetriesSettings(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// Configures the amount of times a message should be immediately retried after failing
        /// before escalating to Delayed Retries.
        /// </summary>
        /// <param name="numberOfRetries">The number of times to immediately retry a failed message.</param>
        public void NumberOfRetries(int numberOfRetries)
        {
            Guard.ThrowIfNegative(numberOfRetries);

            Settings.Set(RecoverabilityComponent.NumberOfImmediateRetries, numberOfRetries);
        }

        /// <summary>
        /// Registers a callback which is invoked when a message fails processing and will be immediately retried.
        /// </summary>
        public ImmediateRetriesSettings OnMessageBeingRetried(Func<ImmediateRetryMessage, CancellationToken, Task> notificationCallback)
        {
            Guard.ThrowIfNull(notificationCallback);

            var subscriptions = Settings.Get<RecoverabilityComponent.Configuration>();
            subscriptions.MessageRetryNotification.Subscribe((retry, cancellationToken) =>
            {
                if (!retry.IsImmediateRetry)
                {
                    return Task.CompletedTask;
                }

                var headerCopy = new Dictionary<string, string>(retry.Message.Headers);
                return notificationCallback(new ImmediateRetryMessage(retry.Message.MessageId, headerCopy, retry.Message.Body, retry.Exception, retry.Attempt), cancellationToken);
            });

            return this;
        }
    }
}