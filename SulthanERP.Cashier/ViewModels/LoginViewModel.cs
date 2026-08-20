using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SulthanERP.Cashier.Services;
using System.Net;

namespace SulthanERP.Cashier.ViewModels;

public partial class LoginViewModel(
    ApiService api,
    Action onSuccess) : ObservableObject
{
    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public int UserId { get; private set; }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Enter username and password.";
            return;
        }

        try
        {
            IsLoading = true;

            var response = await api.PostAsync(
                "Auth/login",
                new
                {
                    userName = UserName,
                    password = Password
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            if (!response.IsSuccessful)
            {
                ErrorMessage =
                    $"Login failed. Server returned {(int)response.StatusCode}.";
                return;
            }

            var token = ApiService.ReadString(
                response.Content,
                "token",
                "accessToken",
                "jwtToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage =
                    "Login response did not include an access token.";
                return;
            }

            UserId =
                ApiService.ReadInt(
                    response.Content,
                    "userId",
                    "id")
                ?? ApiService.ReadUserIdFromJwt(token)
                ?? 0;

            api.SetAccessToken(token);

            onSuccess();
        }
        catch
        {
            ErrorMessage =
                "Unable to connect to Sulthan ERP Server. " +
                "Please check the server address and network connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}