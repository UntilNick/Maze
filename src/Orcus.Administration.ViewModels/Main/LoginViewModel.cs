﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using Orcus.Administration.ViewModels.Utilities;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Unclassified.TxLib;

namespace Orcus.Administration.ViewModels.Main
{
    public class LoginViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private string _errorMessage;
        private bool _isLoggingIn;
        private string _statusMessage;
        private string _username;

        public LoginViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        private DelegateCommand _testCommand;

        public DelegateCommand TestCommand
        {
            get
            {
                return _testCommand ?? (_testCommand = new DelegateCommand(() =>
                {
                    
                }));
            }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        private AsyncRelayCommand<SecureString> _loginCommand;

        public AsyncRelayCommand<SecureString> LoginCommand
        {
            get
            {
                return _loginCommand ?? (_loginCommand = new AsyncRelayCommand<SecureString>(async parameter =>
                {
                    IsLoggingIn = true;

                    IOrcusRestClient client;
                    IReadOnlyList<SourcedPackageIdentity> modules;

                    try
                    {
                        StatusMessage = Tx.T("LoginView:Status.Authenticating");

                        client = await OrcusRestConnector.TryConnect(Username, parameter,
                            new ServerInfo { ServerUri = new Uri("http://localhost:50485/") });

                        StatusMessage = Tx.T("LoginView:Status.RetrieveModules");

                        modules = await ModulesResource.FetchModules(client);

                        if (modules.Any())
                        {
                            StatusMessage = Tx.T("LoginView:Status.LoadModules");

                        }
                    }
                    catch (RestAuthenticationException e)
                    {
                        ErrorMessage = e.GetRestExceptionMessage();
                        IsLoggingIn = false;
                        return;
                    }
                    catch (Exception e)
                    {
                        ErrorMessage = e.Message;
                        IsLoggingIn = false;
                        e.ShowMessageBox();
                        return;
                    }

                    var viewModel = new OverviewViewModel(client);
                    await viewModel.LoadData(s => Debug.Print(s));

                    ShowView.Invoke(this, viewModel);
                }));
            }
        }
    }
}