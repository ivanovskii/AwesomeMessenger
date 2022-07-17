using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleChatApp.CommonTypes;
using SimpleChatApp.GrpcService;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static SimpleChatApp.GrpcService.ChatService;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AwesomeMessenger
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatPage : ContentPage
    {
        private ChatServiceClient chatServiceClient;
        private SimpleChatApp.GrpcService.Guid guid;
        private ObservableCollection<ExpMessageData> chat = new ObservableCollection<ExpMessageData>();
        private Dictionary<String, Color> usersColors = new Dictionary<String, Color>();
        private HashSet<Color> usedColors = new HashSet<Color>();
        private Random rnd = new Random();

        public class ExpMessageData
        {
            public string userLogin { get; set; }
            public string text { get; set; }
            public Color color { get; set; }
            public ExpMessageData(string userLogin, string text, Color color)
            {
                this.userLogin = userLogin;
                this.text = text;
                this.color = color;
            }
        }

        public ChatPage(SimpleChatApp.GrpcService.Guid guid_)
        {
            InitializeComponent();
            guid = guid_;
            chatServiceClient = new ChatServiceClient(
                new Grpc.Core.Channel(
                    "localhost",
                    30051,
                    Grpc.Core.ChannelCredentials.Insecure
                )
            );
            MessagesList.ItemsSource = chat;
        }

        public async void AddMessagesAndSubscrube()
        {
            var messages = await GetMessages();
            foreach (var message in messages)
                AddMessage(message);
            await Subscribe(AddMessage);
        }

        private bool Validate(string message)
        {
            if (message != null && message != "")
                return true;
            return false;
        }

        private async Task SendMessage(string message)
        {
            if (Validate(message))
            {
                var outgoingMessage = new OutgoingMessage()
                {
                    Sid = guid,
                    Text = message
                };
                var ans = await chatServiceClient.WriteAsync(outgoingMessage);
                MessageEntry.Text = "";
            }
        }

        private async Task<List<SimpleChatApp.GrpcService.MessageData>> GetMessages()
        {
            var now = DateTime.MaxValue;
            var then = DateTime.MinValue;

            var timeIntervalRequest = new TimeIntervalRequest()
            {
                StartTime = Timestamp.FromDateTime(then.ToUniversalTime()),
                EndTime = Timestamp.FromDateTime(now.ToUniversalTime()),
                Sid = guid
            };
            var messages = await chatServiceClient.GetLogsAsync(timeIntervalRequest);
            return messages.Logs.ToList();
        }

        private async Task Subscribe(Action<SimpleChatApp.GrpcService.MessageData> onMessage)
        {
            var streamingCall = chatServiceClient.Subscribe(guid);
            while (await streamingCall.ResponseStream.MoveNext())
            {
                var messages = streamingCall.ResponseStream.Current;
                foreach (var message in messages.Logs)
                {
                    onMessage(message);
                }
            }
        }
        private async Task Unsubscribe(Action<SimpleChatApp.GrpcService.MessageData> onMessage)
        {
            await chatServiceClient.UnsubscribeAsync(guid);
        }

        private async void Send_Pressed(object sender, EventArgs e)
        {
            await SendMessage(MessageEntry.Text);
        }

        private void ScrollMessagesToEnd(object obj)
        {
            MessagesList.ScrollTo(obj, ScrollToPosition.End, true);
        }

        private Color RandomColor()
        {
            Color choosedColor = Color.FromRgb(rnd.Next(256), rnd.Next(256), 0);
            if (usedColors.Contains(choosedColor))
            {
                return RandomColor();
            } 
            else 
            {
                return choosedColor;
            }
        }

        private void AddMessage(SimpleChatApp.GrpcService.MessageData md)
        {
            var randomColor = RandomColor();
            usedColors.Add(randomColor);

            if (!(usersColors.Keys.Contains(md.PlayerLogin)))
                usersColors.Add(md.PlayerLogin, randomColor);

            var expMessageData = new ExpMessageData(md.PlayerLogin, md.Text, usersColors[md.PlayerLogin]);
            
            chat.Add(expMessageData);
            ScrollMessagesToEnd(expMessageData);
        }

        private async void Logout_Pressed(object sender, EventArgs e)
        {
            await Unsubscribe(AddMessage);
            guid = new SimpleChatApp.GrpcService.Guid();
            await Navigation.PopModalAsync();
        }
    }
}