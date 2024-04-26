using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using Panoptes.Model.Serialization.Packets;
using System;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;

namespace Panoptes.Model.Sessions.Stream
{
    public sealed class StreamSession : BaseStreamSession
    {
        public StreamSession(ISessionHandler sessionHandler, IResultConverter resultConverter,
            StreamSessionParameters parameters, ILogger logger)
           : base(sessionHandler, resultConverter, parameters, logger)
        { }

        private readonly TimeSpan timeOut = TimeSpan.FromMilliseconds(500);

        protected override void EventsListener(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (var pullSocket = new PullSocket($">tcp://{_host}:{_port}"))
                {
                    while (!_eternalQueueListener.CancellationPending)
                    {
                        var message = new NetMQMessage();
                        if (!pullSocket.TryReceiveMultipartMessage(timeOut, ref message))
                        {
                            continue;
                        }

                        //try
                        //{
                            // There should only be 1 part messages
                            if (message.FrameCount != 1) continue;
                            var data = message[0];
                            var payload = data.ConvertToString(Encoding.UTF8);
                            var packet = JsonConvert.DeserializeObject<Packet>(payload);
                            HandlePacketEventsListener(payload, packet.Type);
                        //}
                        //catch (Exception ex)
                        //{
                        //    Trace.TraceError(ex.ToString());                            
                        //}
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _resetEvent.Set();
            }
        }
    }
}

