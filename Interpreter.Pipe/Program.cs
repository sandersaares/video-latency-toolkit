using Koek;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using Vltk.Common;
using ZXing;

namespace Vltk.Interpreter.Pipe
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (!ParseArguments(args))
            {
                Environment.ExitCode = -1;
                return;
            }

            // One NV12 sample consists of approximately 1.5x pixel count of bytes (adjusted for even row count).
            // The math here seems to work but could benefit from expert review.

            // Luminance is easy.
            var luminanceSize = _width * _height;

            // If height is odd, we +1 for the "other" data set.
            // This is actually a quarter of the data size for each, the height adjustment just makes the data line up in aggregate.
            var adjustedHeight = (_height % 2 == 0) ? _height : _height + 1;

            var otherSize = _width * adjustedHeight / 2;
            var sampleSize = luminanceSize + otherSize;

            var inputReader = new BinaryReader(Console.OpenStandardInput());

            var decoder = new BarcodeReader
            {
                AutoRotate = false,
                TryInverted = false,
                Options = new ZXing.Common.DecodingOptions
                {
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.QR_CODE
                    }
                }
            };

            var interpreter = new SignalInterpreter(new DummyHttpClientFactory());
            interpreter.Error += OnError;
            interpreter.LatencyUpdated += OnLatencyUpdated;

            while (true)
            {
                try
                {
                    Console.Error.WriteLine($"Reading sample of {sampleSize} bytes.");

                    byte[] sample;
                    try
                    {
                        sample = inputReader.ReadBytesAndVerify(sampleSize);
                    }
                    catch (EndOfStreamException)
                    {
                        // This is normal if we ran out of input.
                        Console.Error.WriteLine("Input stream was closed. Exiting.");
                        Environment.ExitCode = 0;

#if DEBUG
                        // We exit when we reach end of stream, without waiting for results.
                        // This may be annoying when testing, so wait a bit to ensure we get all outputs.
                        Console.Error.WriteLine("[DEBUG] Delaying exit for a few seconds so you get a moment to see final output.");
                        Thread.Sleep(TimeSpan.FromSeconds(5));
#endif
                        return;
                    }

                    Console.Error.WriteLine("Processing sample.");

                    var result = decoder.Decode(new Nv12LuminanceSource(sample, _width, _height));

                    if (result != null)
                    {
                        SignalPayload payload;

                        try
                        {
                            payload = SignalPayload.Deserialize(result.Text);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Failed to parse signal: " + ex.ToString());
                            continue;
                        }

                        interpreter.Add(payload);
                    }
                    else
                    {
                        Console.Error.WriteLine("No signal found in sample.");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                    Environment.ExitCode = -1;
                    return;
                }
            }
        }

        private static void OnLatencyUpdated(object? sender, SignalInterpreter.ResultEventArgs e)
        {
            var result = new InterpreterResult
            {
                LatencyMilliseconds = e.Latency.TotalMilliseconds
            };

            Console.Out.WriteLine(JsonSerializer.Serialize(result));
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.Error.WriteLine(e.GetException().ToString());
        }

        private static int _width;
        private static int _height;

        private static bool ParseArguments(string[] args)
        {
            var showHelp = false;
            var debugger = false;

            var options = new OptionSet
            {
                "Usage: Vltk.Interpreter.Pipe.exe --width 123 --height 456",
                "Feed NV12 formatted video samples into stdin, extract sequence of latency result JSON objects from stdout.",
                "Example output: { 'LatencyMilliseconds': 12.345 }",
                "",
                { "h|?|help", "Displays usage instructions.", val => showHelp = val != null },
                "",
                "Input format",
                { "width=", "Width of a single input sample.", (int val) => _width = val },
                { "height=", "Height of a single input sample.", (int val) => _height = val },
                "",
                { "debugger", "Requests a debugger to be attached before data processing starts.", val => debugger = val != null, true }
            };

            List<string> remainingOptions;

            try
            {
                remainingOptions = options.Parse(args);

                if (args.Length == 0 || showHelp)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    return false;
                }

                if (_width <= 0)
                    throw new OptionException("You must specify the width of the input samples.", "width");

                if (_height <= 0)
                    throw new OptionException("You must specify the height of the input samples.", "height");
            }
            catch (OptionException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("For usage instructions, use the --help command line parameter.");
                return false;
            }

            if (remainingOptions.Count != 0)
            {
                Console.WriteLine("Unknown command line parameters: {0}", string.Join(" ", remainingOptions.ToArray()));
                Console.WriteLine("For usage instructions, use the --help command line parameter.");
                return false;
            }

            if (debugger)
                Debugger.Launch();

            return true;
        }
    }
}
