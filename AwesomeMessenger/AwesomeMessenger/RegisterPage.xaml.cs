using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleChatApp.CommonTypes;
using SimpleChatApp.GrpcService;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static SimpleChatApp.GrpcService.ChatService;

namespace AwesomeMessenger
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterPage : ContentPage
    {
        private ChatServiceClient chatServiceClient;

        // Error Messages:
        string errorTitle = "Registraion Error";
        string ok = "OK";
        string loginExist = "Login already exist";
        string unknownError = "Something was wrong";
        string fillAllFields = "Fill in all the fields";
        string passwordsDontMatch = "Passwords do not match";

        public RegisterPage()
        {
            InitializeComponent();
            chatServiceClient = new ChatServiceClient(
                new Grpc.Core.Channel(
                    "localhost",
                    30051,
                    Grpc.Core.ChannelCredentials.Insecure
                )
            );
        }

        private async Task<bool> Validate(string login, string password, string password2)
        {
            if (login != null && login != "" &&
                password != null && password != "" &&
                password2 != null && password2 != "")
            {
                if (password == password2)
                    return true;
                else
                    await DisplayAlert(errorTitle, passwordsDontMatch, ok);
            }
            else
                await DisplayAlert(errorTitle, fillAllFields, ok);
            return false;
        }

        private async Task<bool> isRegistered(RegistrationAnswer ans)
        {
            if (ans.Status != SimpleChatApp.GrpcService.RegistrationStatus.RegistrationSuccessfull)
            {
                var error = (ans.Status == SimpleChatApp.GrpcService.RegistrationStatus.LoginAlreadyExist)
                    ? loginExist
                    : unknownError;
                await DisplayAlert(errorTitle, error, ok);
                return false;
            }
            return true;
        }

        private async void CreateAccount_Pressed(object sender, EventArgs e)
        {
            if (await Validate(Login.Text, Password.Text, Password2.Text))
            {
                var userData = new UserData()
                {
                    Login = Login.Text,
                    PasswordHash = SHA256.GetStringHash(Password.Text)
                };
                var answer = await chatServiceClient.RegisterNewUserAsync(userData);
                if (await isRegistered(answer))
                    await Navigation.PopModalAsync();
            }
        }

        private void Back_Pressed(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
        }
    }
}