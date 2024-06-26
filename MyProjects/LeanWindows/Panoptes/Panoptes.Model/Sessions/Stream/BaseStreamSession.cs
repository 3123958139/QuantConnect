﻿using Microsoft.Extensions.Logging;
using Panoptes.Model.Serialization;
using Panoptes.Model.Serialization.Packets;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using PacketType = QuantConnect.Packets.PacketType;
using System.IO;
using ProtoBuf.Serializers;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Collections.Generic;

namespace Panoptes.Model.Sessions.Stream
{
    public abstract class BaseStreamSession : ISession
    {
        protected readonly ILogger _logger;

        protected readonly ISessionHandler _sessionHandler;
        protected readonly IResultConverter _resultConverter;

        protected readonly BackgroundWorker _eternalQueueListener = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected readonly BackgroundWorker _queueReader = new BackgroundWorker() { WorkerSupportsCancellation = true };
        protected CancellationTokenSource _cts;

        protected readonly BlockingCollection<Packet> _packetQueue = new BlockingCollection<Packet>();

        protected readonly SynchronizationContext _syncContext;

        protected readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        protected readonly string _host;
        protected readonly int _port;
        protected readonly bool _closeAfterCompleted;

        //protected JsonSerializerOptions _options;

        public string Name => $"{_host}:{_port}";

        public BaseStreamSession(ISessionHandler sessionHandler, IResultConverter resultConverter,
            StreamSessionParameters parameters, ILogger logger)
        {
            _logger = logger;

            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // Allow proper decoding of orders.
            //_options = DefaultJsonSerializerOptions.Default;

            _sessionHandler = sessionHandler;
            _resultConverter = resultConverter;

            _host = parameters.Host;
            if (!int.TryParse(parameters.Port, out var port))
            {
                throw new ArgumentOutOfRangeException("The port should be an integer.", nameof(port));
            }
            _port = port;
            _closeAfterCompleted = parameters.CloseAfterCompleted;

            _syncContext = SynchronizationContext.Current;

            if (_syncContext == null)
            {
                throw new NullReferenceException($"BaseStreamSession: {SynchronizationContext.Current} is null, please make sure the seesion was created in UI thread.");
            }
        }

        public virtual void Initialize()
        {
            Subscribe();
        }

        public virtual Task InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Initialize(), cancellationToken);
        }

        public virtual void Shutdown()
        {
            Unsubscribe();
        }

        public virtual void Subscribe()
        {
            try
            {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();

                    // Configure the worker threads
                    _eternalQueueListener.DoWork += EventsListener;
                    _eternalQueueListener.RunWorkerAsync();

                    _queueReader.DoWork += QueueReader;
                    _queueReader.RunWorkerAsync();

                    State = SessionState.Subscribed;
                    _logger.LogInformation("BaseStreamSession.Subscribe: New subscription.");
                }
                else
                {
                    _logger.LogInformation("BaseStreamSession.Subscribe: Cannot subscribe because aslready subscribed.");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not subscribe to the stream", e);
            }
        }

        public virtual void Unsubscribe()
        {
            try
            {
                if (_eternalQueueListener != null) // should never be the case - check if working?
                {
                    _eternalQueueListener.CancelAsync();
                    _eternalQueueListener.DoWork -= EventsListener;
                }

                if (_queueReader != null) // should never be the case - check if working?
                {
                    _queueReader.CancelAsync();
                    _queueReader.DoWork -= QueueReader;
                }

                _cts?.Cancel();
                State = SessionState.Unsubscribed;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not unsubscribe from the {this.GetType()}", e);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        #region Queue Reader
        protected virtual void QueueReader(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (!_queueReader.CancellationPending && !_cts.Token.IsCancellationRequested)
                {
                    var p = _packetQueue.Take(_cts.Token);
                    HandlePacketQueueReader(p);
                }
            }
            catch (OperationCanceledException)
            { }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        /// <summary>
        /// Handle the packet by parsing it and sending it through the <see cref="ISessionHandler"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <returns>Returns true if the packet was handled, otherwise false.</returns>
        protected bool HandlePacketQueueReader(Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.AlgorithmStatus:
                    _syncContext.Send(_ => _sessionHandler.HandleAlgorithmStatus((AlgorithmStatusPacket)packet), null);
                    break;

                case PacketType.LiveNode:
                    _syncContext.Send(_ => _sessionHandler.HandleLiveNode((LiveNodePacket)packet), null);
                    return false;

                case PacketType.AlgorithmNode:
                    var algorithmNodePacket = (AlgorithmNodePacket)packet;
                    //TODO
                    return false;

                case PacketType.LiveResult:
                    HandleLiveResultPacketQR(packet);
                    break;

                case PacketType.BacktestResult:
                    HandleBacktestResultPacketQR(packet);
                    break;

                case PacketType.Log:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((LogPacket)packet).Message, LogItemType.Log), null);
                    break;

                case PacketType.Debug:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((DebugPacket)packet).Message, LogItemType.Debug), null);
                    break;

                case PacketType.HandledError:
                    _syncContext.Send(_ => _sessionHandler.HandleLogMessage(DateTime.UtcNow, ((HandledErrorPacket)packet).Message, LogItemType.Error), null);
                    break;

                case PacketType.OrderEvent:
                    _syncContext.Send(_ => _sessionHandler.HandleOrderEvent((OrderEventPacket)packet), null);
                    break;

                default:
                    _logger.LogWarning("BaseStreamSession.HandlePacketQueueReader: Unknown packet '{packet}'", packet);
                    return false;
            }

            return true;
        }

        private void HandleBacktestResultPacketQR(Packet packet)
        {
            var backtestResultEventModel = (BacktestResultPacket)packet;
            var backtestResultUpdate = _resultConverter.FromBacktestResult(backtestResultEventModel.Results);

            var context = new ResultContext
            {
                Name = Name,
                Result = backtestResultUpdate,
                Progress = backtestResultEventModel.Progress
            };
            _syncContext.Send(_ => _sessionHandler.HandleResult(context), null);

            if (backtestResultEventModel.Progress == 1 && _closeAfterCompleted)
            {
                _syncContext.Send(_ => Unsubscribe(), null);
            }
        }

        private void HandleLiveResultPacketQR(Packet packet)
        {
            var liveResultEventModel = (LiveResultPacket)packet;
            var liveResultUpdate = _resultConverter.FromLiveResult(liveResultEventModel.Results);

            var context = new ResultContext
            {
                Name = Name,
                Result = liveResultUpdate
            };

            _syncContext.Send(_ => _sessionHandler.HandleResult(context), null);
        }
        #endregion

        #region Events Listener
        /// <summary>
        /// Implement it and use <see cref="HandlePacketEventsListener(string, PacketType)"/>.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void EventsListener(object sender, DoWorkEventArgs e);

        /// <summary>
        /// Deserialize the packet and add it to the packet queue.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="packetType"></param>
        /// <returns>Returns true if the packet was handled, otherwise false.</returns>
        protected bool HandlePacketEventsListener(string payload, PacketType packetType)
        {
            // TODO: check https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
            try
            {
                switch (packetType)
                {
                    case PacketType.AlgorithmStatus:
                        var algorithmStatus = JsonConvert.DeserializeObject<AlgorithmStatusPacket>(payload);
                        _packetQueue.Add(JsonConvert.DeserializeObject<AlgorithmStatusPacket>(payload));
                        break;

                    case PacketType.LiveNode:
                        _packetQueue.Add(JsonConvert.DeserializeObject<LiveNodePacket>(payload));
                        break;

                    case PacketType.AlgorithmNode:
                        _packetQueue.Add(JsonConvert.DeserializeObject<AlgorithmNodePacket>(payload));
                        break;

                    case PacketType.LiveResult:
                        _packetQueue.Add(JsonConvert.DeserializeObject<LiveResultPacket>(payload));
                        break;

                    case PacketType.BacktestResult:
                        JsonConverter[] jsonConverters = new JsonConverter[] { new OrderJsonConverter(), new OrderEventJsonConverter() };
                        _packetQueue.Add(JsonConvert.DeserializeObject<BacktestResultPacket>(payload, jsonConverters));
                        break;

                    case PacketType.OrderEvent:
                        _packetQueue.Add(JsonConvert.DeserializeObject<OrderEventPacket>(payload));
                        break;

                    case PacketType.Log:
                        _packetQueue.Add(JsonConvert.DeserializeObject<LogPacket>(payload));
                        break;

                    case PacketType.Debug:
                        _packetQueue.Add(JsonConvert.DeserializeObject<DebugPacket>(payload));
                        break;

                    case PacketType.HandledError:
                        _packetQueue.Add(JsonConvert.DeserializeObject<HandledErrorPacket>(payload));
                        break;

                    default:
                        _logger.LogWarning("BaseStreamSession.HandlePacketEventsListener: Unknown packet type '{packetType}'.", packetType);
                        return false;
                }
                return true;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("BaseStreamSession.HandlePacketEventsListener: Queue is disposed.");
                return false;
            }
        }
        #endregion

        public virtual void Dispose()
        {
            _cts?.Dispose();
            _eternalQueueListener.Dispose();
            _queueReader.Dispose();
            _packetQueue.Dispose();
            GC.SuppressFinalize(this);
        }

        public bool CanSubscribe { get; } = true;

        private SessionState _state = SessionState.Unsubscribed;
        public SessionState State
        {
            get
            {
                return _state;
            }

            protected set
            {
                _state = value;
                _sessionHandler.HandleStateChanged(value);
            }
        }
    }
}
