﻿// *****************************************************************************
// START DATE: Jan 22, 2012
// *****************************************************************************

using LanExchange.Properties;
using LanExchange.Interfaces;
using LanExchange.SDK;
using System;

using LanExchange.Misc;

namespace LanExchange
{
    public class LanExchangeApp : IDisposable
    {
        private readonly IAppPresenter application;

        public LanExchangeApp()
        {
            // global map interfaces to classes
            App.TR.SetResourceManagerTo<Resources>();
            // load plugins
            LoadPlugins();
            // load settings from cfg-file (must be loaded before plugins)
            App.Config.Load();
            App.Config.Changed += App.Presenter.ConfigOnChanged;
            // load addons
            App.Resolve<IAddonManager>().LoadAddons();
            // init application
            application = App.Resolve<IAppPresenter>();
            application.Init();
        }

        public void Dispose()
        {
        }

        public void Run()
        {
            // create main form
            //App.Presenter.ConfigOnChanged(App.Config, new ConfigChangedArgs(ConfigNames.Language));
            App.MainView = App.Resolve<IMainView>();
            App.Presenter.View = App.MainView;
            App.Presenter.PrepareForm();
            App.MainPages.LoadSettings();
            // run application
            application.Run(App.MainView);
        }

        private static void LoadPlugins()
        {
            var plugins = App.Resolve<IPluginManager>();
            plugins.LoadPlugins();
            // register stage images for icon animation
            AnimationHelper.Register(AnimationHelper.WORKING, Resources.process_working, 16, 16);
        }

    }
}