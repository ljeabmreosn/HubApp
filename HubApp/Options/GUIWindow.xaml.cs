﻿using System.Windows;
using System.Windows.Controls;
using HubApp.Options.Pages;
using HubApp.Windows.Pages;
using HubApp.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace HubApp
{

    /// <summary>
    /// Interaction logic for GUIWindow.xaml
    /// </summary>
    public partial class GUIWindow : Window
    {
        // private Dictionary<Page, >
        private MainWindow _mainWindow;
        private TwitterStreamOptions _twitterStreamOptions;
        private TriviaOptions _triviaOptions;
        private TabItem _lastSelected;

        private TwitterStream _twitterStreamPage;
        private Trivia _triviaPage;

        private HashSet<TabablePage> _pages;

        public GUIWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            InitializeWindowOptions(mainWindow);
            InitializeFormActions();
        }

        public GUIWindow() { }

        private void SuspendUnselectedTabs(TabablePage page)
        {
            foreach (TabablePage aPage in this._pages)
            {
                if (aPage != null)
                {
                    if (aPage.Equals(page))
                    {
                        Trace.WriteLine("Resume");
                        aPage.OnResume();
                    }
                    else
                    {
                        aPage.OnPause();
                    }
                }
            }
        }

        private void InitializeFormActions()
        {

        }

        private void InitializeWindowOptions(MainWindow mainWindow)
        {
            this._mainWindow = mainWindow;
            this._pages = new HashSet<TabablePage>();
            this._twitterStreamOptions = null;
            this._triviaOptions = null;
            this._twitterStreamOptions = new TwitterStreamOptions(this);
            this._triviaPage = null;
            this._twitterStreamPage = new TwitterStream();
            this._mainWindow.SetWindow(this._twitterStreamPage);
            this.twitterPage.NavigationService.Navigate(this._twitterStreamOptions);
            this.SizeChanged += GUIWindow_SizeChanged;
            this._lastSelected = this.twitter;
        }

        private void GUIWindow_TabSelected(object sender, SelectionChangedEventArgs e)
        {   
            if (this.twitter.IsSelected && !this._lastSelected.Equals(this.twitter))
            {
                this._lastSelected = this.twitter;
                this._twitterStreamOptions = this._twitterStreamOptions ?? new TwitterStreamOptions(this);
                this._twitterStreamPage = this._twitterStreamPage ?? new TwitterStream();
                this.twitterPage.NavigationService.Navigate(this._twitterStreamOptions);
                this._mainWindow.SetWindow(this._twitterStreamPage);
                this._pages.Add(this._twitterStreamPage);
                SuspendUnselectedTabs(this._twitterStreamPage);
            }
            else if (this.trivia.IsSelected && !this._lastSelected.Equals(this.trivia))
            {
                Trace.WriteLine("got");
                this._lastSelected = this.trivia;
                this._triviaOptions = this._triviaOptions ?? new TriviaOptions();
                this._triviaPage = this._triviaPage ?? new Trivia();
                this.triviaPage.NavigationService.Navigate(this._triviaOptions);
                this._mainWindow.SetWindow(this._triviaPage);
                this._pages.Add(this._triviaPage);
                SuspendUnselectedTabs(this._triviaPage);
            }
            else if (this.sports.IsSelected && !this._lastSelected.Equals(this.sports))
            {
                this._lastSelected = this.sports;
                SuspendUnselectedTabs(null);
            }
        }

        public void SetTrack(string track)
        {
            if (this._twitterStreamPage != null)
            {
                this._twitterStreamPage.SetTrack(track);
            }
        }

        private void GUIWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

    }
}
