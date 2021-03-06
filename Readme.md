# Video latency toolkit

![](Screenshot.png)

How do you accurately measure end to end video latency? This toolkit provides:

1. The signal generator - an app that generates a machine-readable image encoding the current wall clock time.
1. The signal interpreter - an app that reads the image and determines the latency.

The generator can emit:

* A real-time updated window (for feeding input via screen capture).

The interpreter can process:

* A screen capture (for capturing a video player window).
* A stream of decoded video frames in NV12 format (stdin NV12 -> stdout JSON).

The processed video samples can include other content besides the time signal - the interpreter knows how to find the embedded signal from among other content. Just ensure that the entire signal generator window is visible in the output.

# Wall clock synchronization

Clock synchronization between generator and interpreter is assumed. Results will be invalid if time is out of sync.

There is built-in support for automatic clock synchronization by:

1. Referencing a deployed instance of [dash-timeserver](https://github.com/sandersaares/dash-timeserver).
2. Or specifying an NTP server with the URL `ntp://ntp.example.com`.

Alternatively, running both apps on the same PC can yield acceptable results (though may be logically difficult).

# Processing

There is some processing latency - perform a "dry run" by providing generator output directly to the interpreter to determine the procecssing latency on your system.

# Metrics

Interpreter.Gui exposes the latency in the form of Prometheus metrics, on the http://server:19891/metrics URL. You can use the `metrics-port` command line argument to specify a different port, allowing multiple instances of the app to publish metrics concurrently.

# System requirements

Requires .NET 5.0 on Windows.
