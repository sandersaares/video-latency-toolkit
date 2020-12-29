using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Vltk.Interpreter.Gui
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!ParseArguments(e.Args))
            {
                Environment.Exit(-1);
                return;
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// Custom metrics port or null if no custom port was specified.
        /// </summary>
        public static ushort? MetricsPort { get; set; }

        private static bool ParseArguments(string[] args)
        {
            var showHelp = false;
            var debugger = false;

            var options = new OptionSet
            {
                "Usage: Vltk.Interpreter.Gui.exe --metrics-port 12345",
                "Use a different metrics port to publish metrics from multiple instances concurrently.",
                "",
                { "h|?|help", "Displays usage instructions.", val => showHelp = val != null },
                "",
                { "metrics-port=", "Port number to publish metrics on.", (ushort val) => MetricsPort = val },
                "",
                { "debugger", "Requests a debugger to be attached before data processing starts.", val => debugger = val != null, true }
            };

            List<string> remainingOptions;

            try
            {
                remainingOptions = options.Parse(args);

                if (showHelp)
                {
                    var writer = new StringWriter();
                    options.WriteOptionDescriptions(writer);

                    MessageBox.Show(writer.ToString(), "Usage instructions", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            catch (OptionException ex)
            {
                var text = ex.Message + Environment.NewLine + "For usage instructions, use the --help command line parameter.";
                MessageBox.Show(text, "Invalid arguments provided", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (remainingOptions.Count != 0)
            {
                var error = "Unknown command line parameters: " + string.Join(" ", remainingOptions.ToArray());
                var text = error + Environment.NewLine + "For usage instructions, use the --help command line parameter.";
                MessageBox.Show(text, "Invalid arguments provided", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (debugger)
                Debugger.Launch();

            return true;
        }
    }
}
