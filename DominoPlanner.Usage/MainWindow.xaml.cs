﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Configuration;

namespace DominoPlanner.Usage
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            KeyDown += (o, e) => KeyPressedHandler(o, e);
            Opened += (o, e) => MainWindow_Initialized();
            /*PipeManager = new NamedPipeManager("DominoPlanner");
            PipeManager.StartServer();
            PipeManager.ReceiveString += HandleNamedPipe_OpenRequest;
            var args = Environment.GetCommandLineArgs();
            string filesToOpen = "";
            if (args != null && args.Length > 1)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < args.Length; i++)
                {
                    sb.AppendLine(args[i]);
                }
                filesToOpen = sb.ToString();
            }
            PipeManager.Write(filesToOpen);*/
#if DEBUG
            //this.AttachDevTools();
#endif
        }
        // ugly hacky workaround for the fact that events don't wait if they are canceled asynchronously
        private bool should_really_close = false;
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!should_really_close)
            {
                e.Cancel = true;
            }
            if (DataContext is MainWindowViewModel mwvm)
            {
                if (await mwvm.CloseAllTabs())
                {
                    if (!should_really_close)
                    {
                        should_really_close = true;
                        this.Close();
                    }
                }
            }
            //PipeManager.StopServer();
        }
        private void KeyPressedHandler(object sender, KeyEventArgs args)
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.KeyPressed(sender, args);
            }
        }
        /*public void HandleNamedPipe_OpenRequest(string filesToOpen)
        {
            Dispatcher.Invoke(() =>
            {
                ((MainWindowViewModel)DataContext).OpenFile(filesToOpen);

                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                this.Topmost = true;
                this.Activate();
                Dispatcher.BeginInvoke(new Action(() => { this.Topmost = false; }));
            });
        }*/
        protected void MainWindow_Initialized()
        {
            if (DataContext is MainWindowViewModel mwvm)
            {
                mwvm.AfterStartupChecks();
            }
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
    }
}
