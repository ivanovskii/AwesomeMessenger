using Grpc.Core;
using SimpleChatApp.GrpcService;
using SimpleChatApp.CommonTypes;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using static SimpleChatApp.GrpcService.ChatService;

namespace AwesomeMessenger
{
    public partial class MainPage : ContentPage
    {
        private ChatServiceClient chatServiceClient;
        private SimpleChatApp.GrpcService.Guid guid;

        // Error Messages:
        string loginErrorTitle = "Login error";
        string fillAllFields = "Fill in all the fields";
        string wrongLoginOrPassword = "Incorrect Login or Password";
        string unknownError = "Something was wrong";
        string ok = "OK";

        public MainPage()
        {
            InitializeComponent();
            chatServiceClient = new ChatServiceClient(new Channel("localhost: 30051", ChannelCredentials.Insecure));
        }

        private async Task<SimpleChatApp.GrpcService.Guid> Login(string login, string password)
        {
            var userData = new UserData()
            {
                Login = login,
                PasswordHash = SHA256.GetStringHash(password)
            };
            var authorizationData = new AuthorizationData()
            {
                ClearActiveConnection = true,
                UserData = userData
            };
            var ans = await chatServiceClient.LogInAsync(authorizationData);
            return ans.Sid;
        }

        private void Register_Pressed(object sender, EventArgs e)
        {
            var registrationPage = new RegisterPage();
            Navigation.PushModalAsync(registrationPage);
            LoginField.Text = "";
            PasswordField.Text = "";
        }

        private async Task<bool> Validate(string login, string password)
        {
            if (login != null && login != "" &&
                password != null && password != "")
                return true;
            else
                await DisplayAlert(loginErrorTitle, fillAllFields, ok);
            return false;
        }

        private async Task<bool> isAuthorization(SimpleChatApp.GrpcService.AuthorizationAnswer ans)
        {
            if (ans.Status != SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationSuccessfull)
            {
                var error = (ans.Status == SimpleChatApp.GrpcService.AuthorizationStatus.WrongLoginOrPassword)
                    ? wrongLoginOrPassword
                    : unknownError;
                await DisplayAlert(loginErrorTitle, error, ok);
                return false;
            }
            return true;
        }

        private async void Login_Pressed(object sender, EventArgs e)
        {

            if (await Validate(LoginField.Text, PasswordField.Text))
            {
                var userData = new UserData()
                {
                    Login = LoginField.Text,
                    PasswordHash = SHA256.GetStringHash(PasswordField.Text)
                };
                var authData = new AuthorizationData()
                {
                    ClearActiveConnection = true,
                    UserData = userData
                };
                var ans = await chatServiceClient.LogInAsync(authData);
                if (await isAuthorization(ans))
                {
                    var chatPage = new ChatPage(ans.Sid);
                    chatPage.AddMessagesAndSubscrube();
                    await Navigation.PushModalAsync(chatPage);
                    LoginField.Text = "";
                    PasswordField.Text = "";
                }
            }
        }
    }
}
