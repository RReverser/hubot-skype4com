using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SKYPE4COMLib;

namespace SkypeAdapter
{
    using Envelope = IDictionary<string, object>;
    using NodeFunction = Func<object, Task<object>>;

    public class SkypeAdapter
    {
        private Skype skype = new Skype();

        public SkypeAdapter()
        {
            ((_ISkypeEvents_Event)skype).AttachmentStatus += status =>
            {
                if (status == TAttachmentStatus.apiAttachAvailable) skype.Attach(Wait: false);
            };
        }

        private Chat GetChat(Envelope envelope)
        {
            var user = (Envelope)envelope["user"];
            var userId = (string)user["id"];
            return skype.FindChatUsingBlob(userId);
        }

        public void send(Envelope envelope, params string[] messages)
        {
            GetChat(envelope).SendMessage(string.Join(Environment.NewLine, messages));
        }

        public void reply(Envelope envelope, params string[] messages)
        {
            send(envelope, messages);
        }

        public void topic(Envelope envelope, params string[] lines)
        {
            GetChat(envelope).Topic = string.Join(Environment.NewLine, lines);
        }

        public void run()
        {
            if (!skype.Client.IsRunning) skype.Client.Start();
            skype.Attach(Wait: false);
        }

        public void onAttachmentStatus(NodeFunction handler)
        {
            ((_ISkypeEvents_Event)skype).AttachmentStatus += status => handler(status);
        }

        public void onMessage(NodeFunction handler)
        {
            skype.MessageStatus += (message, status) =>
            {
                if (status != TChatMessageStatus.cmsReceived) return;

                handler(new {
                    chatId = message.Chat.Blob,
                    chatName = message.ChatName,
                    messageId = message.Guid,
                    messageText = message.Body,
                    messageType = message.Type
                });
            };
        }
    }

    public class Startup
    {
        private const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;

        public Task<object> Invoke(object input)
        {
            var adapter = new SkypeAdapter();
            var type = adapter.GetType();

            var methods =
                type
                .GetMethods(bindingFlags)
                .ToDictionary(
                    method => method.Name,
                    method =>
                        (NodeFunction)
                        (args => Task.FromResult(type.InvokeMember(
                            method.Name,
                            bindingFlags,
                            null,
                            adapter,
                            (object[])args
                        )))
                );

            return Task.FromResult<object>(methods);
        }
    }
}
