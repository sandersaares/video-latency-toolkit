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

            MetricsLink.Inlines.Add(_metricsUrl);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static readonly string _metricsUrl = $"http://localhost:{VltkConstants.InterpreterMetricServerPort}/metrics";

        private IMetricServer? _server;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _server = new MetricServer(VltkConstants.InterpreterMetricServerPort);
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
            var command = $"netsh http add urlacl url=http://+:{VltkConstants.InterpreterMetricServerPort}/metrics user={Environment.UserDomainName}\\{Environment.UserName}";

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
