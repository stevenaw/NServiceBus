﻿using NServiceBus.Unicast.Transport;
namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of a message queue
    /// </summary>
    public interface IMessageQueue
    {
        /// <summary>
        /// Initializes the message queue.
        /// </summary>
        /// <param name="inputqueue"></param>
        void Init(string inputqueue);

        /// <summary>
        /// Sends the given message to the destination, flowing transactions as requested.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="destination"></param>
        /// <param name="transactional"></param>
        void Send(TransportMessage message, string destination, bool transactional);

        /// <summary>
        /// Returns true if there's a message in the queue passed in the Init method.
        /// </summary>
        /// <returns></returns>
        bool HasMessage();

        /// <summary>
        /// Tries to receive a message from the queue passed in Init, flowing transactions as requested.
        /// </summary>
        /// <param name="transactional"></param>
        TransportMessage Receive(bool transactional);

        /// <summary>
        /// Creates the given queue if it doesn't already exist.
        /// </summary>
        /// <param name="queue"></param>
        void CreateQueue(string queue);
    }
}
