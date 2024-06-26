using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Http;
using Google.Apis.Oauth2.v2;
using Google.Apis.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace TogglDesktop.ViewModels
{
    public class LoginViewModel : ReactiveValidationViewModel<LoginViewModel>
    {
        private ValidationHelper _emailValidation;
        private ValidationHelper _passwordValidation;
        private ValidationHelper _passwordSignupValidation;
        private ValidationHelper _selectedCountryValidation;
        private ValidationHelper _isTosCheckedValidation;
        private HttpClientFactory _httpClientFactory;

        public LoginViewModel(Action loginWithSSO)
            : base(false, RxApp.TaskpoolScheduler)
        {
            Toggl.OnDisplayCountries += OnDisplayCountries;
            Toggl.OnSettings += OnSettings;
            this.WhenAnyValue(x => x.SelectedConfirmAction,
                    x => x.HasFlag(ConfirmAction.LogIn) ? "Log in" : "Sign up")
                .ToPropertyEx(this, x => x.ConfirmButtonText);
            this.WhenAnyValue(x => x.SelectedConfirmAction,
                    x => x.HasFlag(ConfirmAction.LogIn) ? "Log in with Google" : "Sign up with Google")
                .ToPropertyEx(this, x => x.GoogleLoginButtonText);
            this.WhenAnyValue<LoginViewModel,string, ConfirmAction>(x => x.SelectedConfirmAction,
                    x =>
                    {
                        return x switch
                        {
                            ConfirmAction.LogInAndLinkSSO => "Cancel and go back",
                            ConfirmAction.SignUp => "Back to Log in",
                            _ => "Sign up for free"
                        };
                    })
                .ToPropertyEx(this, x => x.SignupLoginToggleText);
            this.ObservableForProperty(x => x.SelectedConfirmAction)
                .Where(x => x.Value == ConfirmAction.SignUp)
                .Take(1)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(_ => Toggl.GetCountries());
            ConfirmLoginSignupCommand = ReactiveCommand.CreateFromTask(ConfirmLoginSignupAsync);
            ConfirmGoogleLoginSignupCommand = ReactiveCommand.Create(ConfirmGoogleLoginSignup);
            LoginWithSSO = ReactiveCommand.Create(loginWithSSO);
            IsLoginSignupExecuting = ConfirmLoginSignupCommand.IsExecuting
                .CombineLatest(ConfirmGoogleLoginSignupCommand.IsExecuting,
                    (isExecuting1, isExecuting2) => isExecuting1 || isExecuting2);
            IsLoginSignupExecuting
                .ToPropertyEx(this, x => x.IsLoading);
            IsLoginSignupExecuting
                .Select(x => !x)
                .ToPropertyEx(this, x => x.IsViewEnabled);

            // password rules
            var passwordObservable = this.WhenAnyValue(x => x.Password).Where(x => x != null);
            passwordObservable.Select(PasswordEx.IsEightCharactersOrMore)
                .ToPropertyEx(this, x => x.IsEightCharactersOrMore);
            passwordObservable.Select(PasswordEx.IsLowercaseAndUppercase)
                .ToPropertyEx(this, x => x.IsLowercaseAndUppercase);
            passwordObservable.Select(PasswordEx.IsAtLeastOneNumber)
                .ToPropertyEx(this, x => x.IsAtLeastOneNumber);
            var canShowPasswordStrength = this.WhenAnyValue(x => x.IsPasswordFocused,
                x => x.SelectedConfirmAction,
                (isFocused, confirmAction) => isFocused && confirmAction == ConfirmAction.SignUp);
            var shouldHidePasswordStrength = passwordObservable.Select(PasswordEx.AllRulesSatisfied)
                .Delay(satisfied => satisfied ? Observable.Timer(TimeSpan.FromSeconds(1)) : Observable.Return(0L));
            canShowPasswordStrength.CombineLatest(shouldHidePasswordStrength, (canShow, shouldHide) => canShow && !shouldHide)
                .ToPropertyEx(this, x => x.ShowPasswordStrength);
            SelectedConfirmAction = ConfirmAction.LogIn;
            InitializeValidation();
        }
        public ReactiveCommand<Unit, bool> ConfirmLoginSignupCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfirmGoogleLoginSignupCommand { get; }

        public ReactiveCommand<Unit, Unit> LoginWithSSO { get; }
        public IObservable<bool> IsLoginSignupExecuting { get; }

        [Reactive]
        public IList<CountryViewModel> Countries { get; private set; }

        [Reactive]
        public CountryViewModel SelectedCountry { get; set; }

        [Reactive]
        public ConfirmAction SelectedConfirmAction { get; set; }

        [Reactive]
        public string Email { get; set; }

        [Reactive]
        public string Password { get; set; }

        public string SSOConfirmationCode { get; set; }

        public string SSOEmail { get; set; }

        public Subject<Unit> FocusEmail { get; } = new Subject<Unit>();
        public Subject<Unit> FocusPassword { get; } = new Subject<Unit>();
        public Subject<Unit> FocusCountrySelection { get; } = new Subject<Unit>();
        public Subject<Unit> FocusTosCheckbox { get; } = new Subject<Unit>();

        [Reactive]
        public bool IsTosChecked { get; set; }

        [Reactive]
        public bool ShowLoginError { get; set; }

        [Reactive]
        public bool IsPasswordFocused { get; set; }

        public bool ShowPasswordStrength { [ObservableAsProperty] get; }
        public string ConfirmButtonText { [ObservableAsProperty] get; }
        public string GoogleLoginButtonText { [ObservableAsProperty] get; }
        public string SignupLoginToggleText { [ObservableAsProperty] get; }
        public bool IsLoading { [ObservableAsProperty] get; }
        public bool IsViewEnabled { [ObservableAsProperty] get; }
        public bool IsEightCharactersOrMore { [ObservableAsProperty] get; }
        public bool IsLowercaseAndUppercase { [ObservableAsProperty] get; }
        public bool IsAtLeastOneNumber { [ObservableAsProperty] get; }

        private void InitializeValidation()
        {
            _emailValidation = this.ValidationRule(
                x => x.Email,
                email => email.IsValidEmailAddress(),
                "Please enter a valid email");
            _passwordValidation = this.ValidationRule(
                x => x.Password,
                password => !string.IsNullOrEmpty(password),
                "A password is required");
            _passwordSignupValidation = this.ValidationRule(
                x => x.Password,
                PasswordEx.AllRulesSatisfied,
                string.Empty);
            _selectedCountryValidation = this.ValidationRule(
                x => x.SelectedCountry,
                selectedCountry => selectedCountry != null,
                "Please select country");
            _isTosCheckedValidation = this.ValidationRule(
                x => x.IsTosChecked,
                isTosChecked => isTosChecked,
                "Please accept the terms");
        }

        private bool PerformValidation(bool isGoogleLogin = false)
        {
            ShowLoginError = false;

            if (!isGoogleLogin && !_emailValidation.IsValid)
            {
                FocusEmail.OnNext(Unit.Default);
            }
            else if (!isGoogleLogin && !_passwordValidation.IsValid)
            {
                FocusPassword.OnNext(Unit.Default);
            }
            else if (SelectedConfirmAction == ConfirmAction.SignUp && !isGoogleLogin && !_passwordSignupValidation.IsValid)
            {
                FocusPassword.OnNext(Unit.Default);
            }
            else if (SelectedConfirmAction == ConfirmAction.SignUp && !_selectedCountryValidation.IsValid)
            {
                FocusCountrySelection.OnNext(Unit.Default);
            }
            else if (SelectedConfirmAction == ConfirmAction.SignUp && !_isTosCheckedValidation.IsValid)
            {
                FocusTosCheckbox.OnNext(Unit.Default);
            }
            else
            {
                return true;
            }
            ActivateValidation();

            return false;
        }

        private async Task<bool> ConfirmLoginSignupAsync()
        {
            if (!PerformValidation())
            {
                return false;
            }

            var success = false;
            if (SelectedConfirmAction.HasFlag(ConfirmAction.LinkSSO) && SSOEmail == Email)
                Toggl.SetNeedEnableSSO(SSOConfirmationCode);
            switch (SelectedConfirmAction)
            {
                case ConfirmAction.LogInAndLinkSSO:
                case ConfirmAction.LogIn:
                    success = await ConfirmAsync(Toggl.Login);
                    break;
                case ConfirmAction.SignUp:
                    success = await ConfirmAsync(Toggl.Signup);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (SelectedConfirmAction.HasFlag(ConfirmAction.LinkSSO))
            {
                Toggl.ResetEnableSSO();
                SelectedConfirmAction &= ~ConfirmAction.LinkSSO;
            }

            return success;
        }

        private async void ConfirmGoogleLoginSignup()
        {
            if (!PerformValidation(isGoogleLogin: true))
            {
                return;
            }

            if (SelectedConfirmAction.HasFlag(ConfirmAction.LinkSSO))
                Toggl.SetNeedEnableSSO(SSOConfirmationCode);
            switch (SelectedConfirmAction)
            {
                case ConfirmAction.LogInAndLinkSSO:
                case ConfirmAction.LogIn:
                    await GoogleLoginAsync();
                    break;
                case ConfirmAction.SignUp:
                    await GoogleSignupAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (SelectedConfirmAction.HasFlag(ConfirmAction.LinkSSO))
            {
                Toggl.ResetEnableSSO();
                SelectedConfirmAction &= ~ConfirmAction.LinkSSO;
            }
        }

        private void OnDisplayCountries(List<Toggl.TogglCountryView> list)
        {
            var countriesVm = list.Select(c => new CountryViewModel(c)).ToArray();
            Dispatcher.CurrentDispatcher.Invoke(() => { Countries = countriesVm; });
        }

        private void OnSettings(bool open, Toggl.TogglSettingsView settings)
        {
            _httpClientFactory = HttpClientFactoryFromProxySettings(settings);
        }

        public async Task GoogleSignupAsync()
        {
            await GoogleAuth(accessToken => Toggl.GoogleSignup(accessToken, SelectedCountry?.ID ?? default), "Signup");
        }

        public async Task GoogleLoginAsync()
        {
            await GoogleAuth(accessToken => Toggl.GoogleLogin(accessToken), "Login");
        }

        private async Task GoogleAuth(Action<string> authAction, string authProcessName)
        {
            try
            {
                var credential = await ObtainGoogleUserCredentialAsync();
                authAction(credential.Token.AccessToken);
                await credential.RevokeTokenAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("access_denied") ||
                    (ex.InnerException != null &&
                     ex.InnerException.Message.Contains("access_denied")))
                {
                    Toggl.NewError($"{authProcessName} process was canceled", true);
                }
                else
                {
                    Toggl.NewError(ex.Message, false);
                }
            }
        }

        private async Task<UserCredential> ObtainGoogleUserCredentialAsync()
        {
            var initializer = new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = "426090949585-uj7lka2mtanjgd7j9i6c4ik091rcv6n5.apps.googleusercontent.com",
                    ClientSecret = "6IHWKIfTAMF7cPJsBvoGxYui"
                },
                HttpClientFactory = _httpClientFactory
            };
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                initializer,
                new[]
                {
                    Oauth2Service.Scope.UserinfoEmail,
                    Oauth2Service.Scope.UserinfoProfile
                },
                "user",
                CancellationToken.None);
            var isTokenExpired = credential.Token.IsExpired(SystemClock.Default);
            if (isTokenExpired)
            {
                await credential.RefreshTokenAsync(CancellationToken.None);
            }

            return credential;
        }

        private async Task<bool> ConfirmAsync(Func<string, string, long, bool> confirmAction)
        {
            var email = Email;
            var password = Password;
            var selectedCountryId = SelectedCountry?.ID ?? -1;
            return await Task.Run(() => confirmAction(email, password, selectedCountryId));
        }

        private static HttpClientFactory HttpClientFactoryFromProxySettings(Toggl.TogglSettingsView settings)
        {
            var proxyHttpClientFactory = new ProxySupportedHttpClientFactory
            {
                UseProxy = settings.UseProxy
            };
            if (settings.AutodetectProxy)
            {
                proxyHttpClientFactory.Proxy = WebRequest.DefaultWebProxy;
            }
            else if (settings.UseProxy)
            {
                if (!Uri.CheckHostName(settings.ProxyHost).Equals(UriHostNameType.Unknown))
                {
                    var proxy = new WebProxy(settings.ProxyHost + ":" + settings.ProxyPort, true);

                    if (!string.IsNullOrEmpty(settings.ProxyUsername))
                    {
                        proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
                    }
                    proxyHttpClientFactory.Proxy = proxy;
                }
            }

            return proxyHttpClientFactory;
        }

        public void SetLinkSSOMode(string confirmationCode, string email)
        {
            if (!string.IsNullOrEmpty(confirmationCode))
            {
                SelectedConfirmAction = ConfirmAction.LogInAndLinkSSO;
                SSOConfirmationCode = confirmationCode;
                SSOEmail = email;
            }
            else
                SelectedConfirmAction = ConfirmAction.LogIn;
        }

        public class CountryViewModel
        {
            private readonly Toggl.TogglCountryView _countryView;
            public CountryViewModel(Toggl.TogglCountryView countryView)
            {
                _countryView = countryView;
            }

            public string Name => _countryView.Name;
            public long ID => _countryView.ID;
        }

        private class ProxySupportedHttpClientFactory : HttpClientFactory
        {
            public bool UseProxy { get; set; }
            public IWebProxy Proxy { get; set; }
            protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args)
            {
                var webRequestHandler = new WebRequestHandler
                {
                    UseProxy = this.UseProxy,
                    UseCookies = false
                };
                if (webRequestHandler.UseProxy)
                {
                    webRequestHandler.Proxy = Proxy;
                }

                return webRequestHandler;
            }
        }

        public void Reset()
        {
            SnoozeValidation();
            
            Email = null;
            Password = null;
            SelectedCountry = null;
            IsTosChecked = false;
        }
    }

    [Flags]
    public enum ConfirmAction
    {
        LogIn = 1,
        LinkSSO = 2,
        LogInAndLinkSSO = LogIn | LinkSSO,
        SignUp = 4,
    }
}