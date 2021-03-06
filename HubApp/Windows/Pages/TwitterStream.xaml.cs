﻿using System;
using System.Linq;
using System.Windows.Documents;
using System.Globalization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Diagnostics;
using HubApp.Twitter;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using Tweetinvi;
using HubApp.Utilities;

namespace HubApp
{
    /// <summary>
    /// Interaction logic for TwitterStream.xaml
    /// </summary>
    public partial class TwitterStream : TabablePage
    {
        private const int SLEEP_TIME = 5000;
        private const string DEFAULT_TWITTER_PIC = "https://camo.githubusercontent.com/93c261622c63780cb29c6423b0787c8de460aff6/687474703a2f2f636c6f75642e73636f74742e65652f696d616765732f6e756c6c2e706e67";
        private const string TWITTER_BLUE = "#55ACEE";
        private const string SHOW_PICS = "Show us your pics! ";
        private Queue<ITweet> _tweetQueue;
        private IFilteredStream _tweetStream;
        private string _track = "#MannequinChallenge";

        public TwitterStream()
        {
            InitializeComponent();
            InitializeComponent();
            InitializeTwitterQueue();
            InitializeTwitterStream();
            InitializeTwitterCard();
            InitializeWindowOptions();
        }

        public override void OnPause()
        {
            if (this._tweetStream != null)
            {
                this._tweetStream.PauseStream();
            }
        }

        public override void OnResume()
        {
            if (this._tweetStream != null)
            {
                this._tweetStream.ResumeStream();
            }
        }

        private void InitializeTwitterStream()
        {
            Thread thread = new Thread(TwitterView_StartStreaming);
            thread.IsBackground = true;
            thread.Start();
        }

        public void SetTrack(string track)
        {
            this._track = track;
            this.tags.Inlines.Clear();
            this.tags.Inlines.Add(SHOW_PICS);
            this.tags.Inlines.Add(new Run(this._track)
            {
                Foreground = new BrushConverter().ConvertFromString(TWITTER_BLUE) as SolidColorBrush
            });
            _tweetQueue.Clear();
            _tweetStream.StopStream();
            InitializeTwitterStream();
        }

        private void InitializeTwitterQueue()
        {
            _tweetQueue = new Queue<ITweet>();
            Thread thread = new Thread(TwitterView_Queue);
            thread.IsBackground = true;
            thread.Start();
        }

        private void InitializeTwitterCard()
        {
            //this.twitterMedia.Source = new BitmapImage(new Uri(DEFAULT_TWITTER_PIC));
        }


        private void InitializeWindowOptions()
        {
            this.tags.Inlines.Add(new Run(this._track)
            {
                Foreground = new BrushConverter().ConvertFromString(TWITTER_BLUE) as SolidColorBrush
            });
            this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
        }

        private void TwitterView_StartStreaming()
        {

            TwitterView twitter = new TwitterView();
            this._tweetStream = Stream.CreateFilteredStream();

            // add filter
            _tweetStream.AddTrack(_track);
            // add action event
            _tweetStream.MatchingTweetReceived +=
                  new EventHandler<Tweetinvi.Events.MatchedTweetReceivedEventArgs>(TwitterView_ReceivedPost);
            _tweetStream.StartStreamMatchingAllConditions();
        }

        private void TwitterView_Queue()
        {
            if (_tweetQueue.Count != 0)
            {
                // get first tweet out of queue
                ITweet tweet = _tweetQueue.Dequeue();
                var link = tweet.Url;


                Trace.WriteLine(tweet.Url);

                // go on Window thread and change the contents of the View 
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    AddProfilePic(tweet);
                    AddName(tweet);
                    AddTweetText(tweet);

                    //this.tweetMediaBg.Background =
                    //    new BrushConverter().ConvertFromString(
                    //        string.Format("#{0}", user.ProfileBackgroundColor)
                    //    ) as SolidColorBrush;


                    if (tweet.Media[0].MediaType.Equals("video") && tweet.Media[0].VideoDetails.Variants[0].URL.Last().Equals('4'))
                    {
                        AddMediaVideo(tweet);
                    }
                    else
                    {
                        AddMediaPic(tweet);
                    }


                }));
            }
            Thread.Sleep(SLEEP_TIME);
            TwitterView_Queue();
        }

        private void TwitterView_ReceivedPost(object sender, Tweetinvi.Events.MatchedTweetReceivedEventArgs args)
        {
            // add to the queue only if there is a picture in the tweet
            if (args.Tweet.Media.Count > 0) // && !args.Tweet.IsRetweet
            {
                _tweetQueue.Enqueue(args.Tweet);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size newSize = e.NewSize;
            double height = newSize.Height, width = newSize.Width;
            this.tweetText.FontSize = (height + width) / 125;
        }

        private void mediaElement_OnMediaEnded(object sender, RoutedEventArgs e)
        {
            // start video over
            this.bgvideo.Position = new TimeSpan(0, 0, 1);
            this.bgvideo.Play();
        }

        private void AddTweetText(ITweet tweet)
        {
            this.tweetText.Inlines.Clear();

            List<EntityHelper> entities = TweetHelper.ParseTweet(tweet);

            // map bytes to interpretable text
            int[] textMappings = new int[tweet.Text.Length + 1];
            var textElementsEnumerator = StringInfo.GetTextElementEnumerator(tweet.Text);
            int lastIndex = 0;
            int byteLocation = 0;

            while (textElementsEnumerator.MoveNext())
            {
                int textIndex = textElementsEnumerator.ElementIndex;
                int textLength = textElementsEnumerator.Current.ToString().Length;
                for (int j = 0; j < textLength; j++)
                {
                    textMappings[byteLocation++] = textIndex;
                }
                lastIndex = textIndex;
            }
            textMappings[byteLocation] = ++lastIndex;

            // add colored text to textblock based on content
            int last = 0;
            foreach (var item in entities)
            {
                try
                {
                    if (last <= item.Indice[0])
                    {
                        this.tweetText.Inlines.Add(new Run(new StringInfo(tweet.Text).SubstringByTextElements(last, textMappings[item.Indice[0]] - last)));
                        this.tweetText.Inlines.Add(new Run(new StringInfo(tweet.Text).SubstringByTextElements(textMappings[item.Indice[0]], textMappings[item.Indice[1]] - textMappings[item.Indice[0]]))
                        {
                            Foreground = new BrushConverter().ConvertFromString(TWITTER_BLUE) as SolidColorBrush
                        });
                    }
                    last = textMappings[item.Indice[1]];
                }
                catch
                {
                    // it's an encoding problem
                    // fail silenty...
                    this.tweetText.Inlines.Clear();
                    this.tweetText.Text = tweet.Text;
                    break;
                }
            }
        }

        private void AddName(ITweet tweet)
        {
            var user = tweet.CreatedBy;
            var screenName = user.ScreenName;

            this.realNameAndScreenName.Inlines.Clear();
            this.realNameAndScreenName.Inlines.Add(new Run(user.ToString()));
            this.realNameAndScreenName.Inlines.Add(new Run(" "));
            this.realNameAndScreenName.Inlines.Add(new Run(string.Format("@{0}", screenName))
            {
                Foreground = Brushes.Gray,
                FontWeight = FontWeights.Light,

            });
        }

        private void AddProfilePic(ITweet tweet)
        {
            var userPic = tweet.CreatedBy.ProfileImageUrlHttps;
            this.profilePic.Source = new BitmapImage(new Uri(userPic));
        }

        private void AddMediaVideo(ITweet tweet)
        {
            this.twitterMedia.Visibility = Visibility.Hidden;
            this.twitterVideo.Visibility = Visibility.Visible;

            this.twitterMedia.Source = null;

            // Convert https to http if need be
            this.twitterVideo.Source = new Uri(tweet.Media[0].VideoDetails.Variants[0].URL.Remove(4, 1));
        }

        private void AddMediaPic(ITweet tweet)
        {
            var media = tweet.Media.Count == 0 ? DEFAULT_TWITTER_PIC : tweet.Media[0].MediaURL;

            this.twitterVideo.Visibility = Visibility.Hidden;
            this.twitterMedia.Visibility = Visibility.Visible;

            this.twitterVideo.Source = null;
            this.twitterMedia.Source = new BitmapImage(new Uri(media));
        }
    }
}
