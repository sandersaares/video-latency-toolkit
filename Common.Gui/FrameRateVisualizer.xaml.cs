﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Vltk.Common.Gui
{
    public partial class FrameRateVisualizer : UserControl
    {
        public void AddEvent() => _counter.AddEvent();

        public FrameRateVisualizer()
        {
            InitializeComponent();

            _refreshTimer.Tick += OnTimerTick;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            FpsLabel.Text = _counter.MovingAverageFps.ToString("N0", CultureInfo.InvariantCulture);
        }

        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };

        private readonly FpsCounter _counter = new FpsCounter();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
        }
    }
}
