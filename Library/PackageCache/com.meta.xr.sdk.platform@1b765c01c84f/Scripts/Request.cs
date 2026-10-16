// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    [Preserve]
    public class Request<T> : Request
    {
        public readonly Type requestType;
        public bool HasCallback => _callback != null || _genericCallback != null || _tcs != null;

        private Message<T>.Callback _genericCallback = null;
        private TaskCompletionSource<Message<T>> _tcs = null;

        // Holds a response that arrived before any handler was attached, so it can be
        // delivered once OnComplete()/Gen() runs instead of being dropped. See HandleMessage.
        private Message<T> _pendingMessage = null;
        private readonly object _handlerLock = new object();

        public Request(ulong requestID) : base(requestID)
        {
            requestType = typeof(T);
        }

        public Request<T> OnComplete(Message<T>.Callback callback)
        {
            Message<T> buffered;
            lock (_handlerLock)
            {
                if (_genericCallback != null || _tcs != null)
                {
                    throw new UnityException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
                }

                _genericCallback = callback;
                buffered = TakePendingMessage();
            }

            Callback.AddRequest(this);
            if (buffered != null)
            {
                Deliver(buffered);
            }
            return this;
        }

        #region Async await functions
        // Legacy await handler: await Leaderboards.GetEntries().Gen();
        public new async Task<Message<T>> Gen()
        {
            Message<T> buffered;
            lock (_handlerLock)
            {
                if (_genericCallback != null || _tcs != null)
                {
                    throw new UnityException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
                }

                _tcs = new TaskCompletionSource<Message<T>>();
                buffered = TakePendingMessage();
            }

            Callback.AddRequest(this);
            if (buffered != null)
            {
                Deliver(buffered);
            }
            return await _tcs.Task;
        }

        // New await handler: await Leaderboards.GetEntries();
        public new TaskAwaiter<Message<T>> GetAwaiter()
        {
            return Gen().GetAwaiter();
        }
        #endregion

        override public void HandleMessage(Message msg)
        {
            Message<T> typedMessage = new Message<T>(msg.requestID, msg.sessionID, msg.cookie, msg.data, msg.status);

            lock (_handlerLock)
            {
                // A response can arrive before the caller has attached a handler — e.g.
                // Core.AsyncInitialize delivers on a background thread that may finish
                // before the caller's await/OnComplete runs. Buffer it and let
                // OnComplete()/Gen() deliver it once a handler is attached.
                if (_callback == null && _genericCallback == null && _tcs == null)
                {
                    _pendingMessage = typedMessage;
                    return;
                }
            }

            Deliver(typedMessage);
        }

        private void Deliver(Message<T> typedMessage)
        {
            // handles the case with non typed message, where dev choose to convert themselves
            //
            // Leaderboards.GetEntries().OnComplete((Message message) => {
            //      Message<LeaderboardEntryList> msg = message as Message<LeaderboardEntryList>;
            //      LeaderboardEntryList entryList = msg.Data;
            // }
            if (_callback != null)
            {
                _callback(typedMessage);
                return;
            }

            // handles the case with typed message, the proper approach dev should use
            //
            // Leaderboards.GetEntries().OnComplete((Message<LeaderboardEntryList> message) => {
            //      LeaderboardEntryList entryList = msg.Data;
            // }
            if (_genericCallback != null)
            {
                _genericCallback(typedMessage);
                return;
            }

            // handles the case with Async Await, the modern async approach without callback
            //
            // Message<LeaderboardEntryList> listMessage = await Leaderboards.GetEntries().Gen();
            // LeaderboardEntryList entryList = msg.Data;
            if (_tcs != null)
            {
                _tcs.SetResult(typedMessage);
                return;
            }

            throw new UnityException("Request<T> with no handler. This should never happen.");
        }

        // Must be called while holding _handlerLock.
        private Message<T> TakePendingMessage()
        {
            Message<T> pending = _pendingMessage;
            _pendingMessage = null;
            return pending;
        }
    }

    [Preserve]
    public class Request
    {
        public ulong RequestID { get; set; }
        protected Message.Callback _callback;
        private TaskCompletionSource<Message> _tcs;


        public Request(ulong requestID)
        {
            this.RequestID = requestID;
        }

        public Request OnComplete(Message.Callback callback)
        {
            _callback = callback;
            Callback.AddRequest(this);
            return this;
        }

        #region Async await functions
        public async Task<Message> Gen()
        {
            _tcs = new TaskCompletionSource<Message>();
            Callback.AddRequest(this);
            return await _tcs.Task;
        }

        public TaskAwaiter<Message> GetAwaiter()
        {
            return Gen().GetAwaiter();
        }
        #endregion

        virtual public void HandleMessage(Message msg)
        {
            if (_callback != null)
            {
                _callback(msg);
                return;
            }

            if (_tcs != null)
            {
                _tcs.SetResult(msg);
                return;
            }

            throw new UnityException("Request with no handler. This should never happen.");
        }

        public static void RunCallbacks(uint limit = 0)
        {
            // limit parameter is deprecated, but we ignore it to avoid breaking existing code
            Callback.RunCallbacks();
        }
    }
}
