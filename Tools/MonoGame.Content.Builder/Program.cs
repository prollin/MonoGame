﻿// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MonoGame.Content.Builder
{
    class Program
    {
        class AssertListener : TraceListener
        {
            public override void Write(string message)
            {
                Console.Write(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }

            public override void Fail(string message, string detailMessage)
            {
                Console.WriteLine(message);

                if (!string.IsNullOrEmpty(detailMessage))
                    Console.WriteLine(detailMessage);
            }
        }

        static int Main(string[] args)
        {
            // We force all stderr to redirect to stdout
            // to avoid any out of order console output.
            Console.SetError(Console.Out);

            // Hook in our own trace listener so that errors
            // from asserts appear in the output logging instead
            // of having silent failures.
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new AssertListener());

            if (!Environment.Is64BitProcess && Environment.OSVersion.Platform != PlatformID.Unix)
            {
                Console.Error.WriteLine("The MonoGame content tools only work on a 64bit OS.");
                return -1;
            }

            var content = new BuildContent();

            var versionString = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            // Parse the command line.
            var parser = new MGBuildParser(content)
            {
                Title = $"MonoGame Content Builder: v{versionString}\n" +
                        "Builds optimized game content for MonoGame projects."
            };

            if (!parser.Parse(args))
                return -1;           
            
            // Launch debugger if requested.
            if (content.LaunchDebugger)
            {
                try {
                    System.Diagnostics.Debugger.Launch();
                } catch (NotImplementedException) {
                    // not implemented under Mono
                    Console.Error.WriteLine("The debugger is not implemented under Mono and thus is not supported on your platform.");
                }
            }
            if (content.WaitForDebuggerToAttach)
            {
                var currentProcess = Process.GetCurrentProcess();
                Console.WriteLine($"Waiting for debugger to attach ({currentProcess.MainModule.FileName} PID {currentProcess.Id}).  Press any key to continue without debugger.");
                while (!Debugger.IsAttached)
                {
                    if (Console.KeyAvailable)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }

            // Print a startup message.            
            var buildStarted = DateTime.Now;
            if (!content.Quiet)
                Console.WriteLine("Build started {0}\n", buildStarted);

            // Let the content build.
            int successCount, errorCount;
            content.Build(out successCount, out errorCount);

            // Print the finishing info.
            if (!content.Quiet)
            {
                Console.WriteLine("\nBuild {0} succeeded, {1} failed.\n", successCount, errorCount);
                Console.WriteLine("Time elapsed {0:hh\\:mm\\:ss\\.ff}.", DateTime.Now - buildStarted);
            }

            // Return the error count.
            return errorCount;
        }
    }
}
