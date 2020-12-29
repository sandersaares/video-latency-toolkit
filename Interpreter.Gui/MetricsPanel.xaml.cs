using Prometheus;
using System;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Vltk.Common;

namespace Vltk.Interpreter.Gui
{
    public partial class MetricsPanel : UserControl
    {
        public MetricsPanel()
        {
            InitializeComponent();

            _metricsPort = App.MetricsPort ?? VltkConstants.InterpreterMetricServerPort;
            _metricsUrl = $"http://localhost:{_metricsPort}/metrics";

            MetricsLink.Inlines.Add(_metricsUrl);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private readonly ushort _metricsPort;
        private readonly string _metricsUrl;

        private IMetricServer? _server;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _server = new MetricServer(_metricsPort);
                _server.Start();

                StatusLabel.Text = "";
                StatusLabel.Visibility = Visibility.Collapsed;
                LinkLabel.Visibility = Visibility.Visible;
                GrantAccessLabel.Visibility = Visibility.Collapsed;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                // Access denied.
                StatusLabel.Visibility = Visibility.Collapsed;
                LinkLabel.Visibility = Visibility.Collapsed;
                GrantAccessLabel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                _server?.Dispose();
                _server = null;

                StatusLabel.Text = ex.Message;

                StatusLabel.Visibility = Visibility.Visible;
                LinkLabel.Visibility = Visibility.Collapsed;
                GrantAccessLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _server?.Dispose();
            _server = null;

            StatusLabel.Text = "";
            StatusLabel.Visibility = Visibility.Visible;
            LinkLabel.Visibility = Visibility.Collapsed;
            GrantAccessLabel.Visibility = Visibility.Collapsed;
        }

        private void AccessGrant_Click(object sender, RoutedEventArgs e)
        {
            var command = $"netsh http add urlacl url=http://+:{_metricsPort}/metrics user={Environment.UserDomainName}\\{Environment.UserName}";

            MessageBox.Show(command, "Execute as Administrator to grant access");
        }

        private void MetricsLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _metricsUrl,
                UseShellExecute = true
            });
        }
    }
}
