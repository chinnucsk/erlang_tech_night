﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using System.IO;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Threading;
using System.Diagnostics;

namespace WordCount
{
    class Program
    {
        private static readonly object _locker = new object();


        static void Main(string[] args)
        {
            var options = new CommandlineOptions();
            Parser.Default.ParseArguments(args, options);

            if(  options.Port !=0 )
            {
                StartServer(options.Port);
                return;
            }

            var directoryPath = Path.GetFullPath(options.Path);
            var filePaths = Directory.EnumerateFiles(directoryPath);

            ulong count = 0;
            var port = 12345;
            Parallel.ForEach(filePaths, path =>
            {
                var self = Process.GetCurrentProcess().MainModule.FileName;
                self = self.Replace(".vshost", "");
                ProcessStartInfo info = new ProcessStartInfo(self);
                lock (_locker)
                {
                    port++;
                    info.Arguments =  "--port=" + port;
                }
                
                info.UseShellExecute = true;
                var serverProcess = Process.Start(info);

                ChannelFactory<IServer> httpFactory =
                        new ChannelFactory<IServer>(
                          new BasicHttpBinding(),
                          new EndpointAddress(
                            "http://localhost:"+port+"/Server"));

                var server = httpFactory.CreateChannel();
                ulong words = 0;
                try
                {
                    words = server.CountWords(path);
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.ToString());
                }
                Console.WriteLine(path + ": " + words);
                lock (_locker)
                {
                    count += words;
                }

                serverProcess.Kill();
            });

            Console.WriteLine();
            Console.WriteLine("all words in all files: " + count);

            Console.ReadKey();
        }

        private static void StartServer(int port)
        {
            var uris = new Uri[] { new Uri("http://localhost:"+port) };
            using (ServiceHost host = new ServiceHost(typeof(Server), uris))
            {
                host.AddServiceEndpoint(typeof(IServer), new BasicHttpBinding(), "Server");

                try
                {
                    host.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.ReadLine();
                }
                while (true)
                {
                    Thread.Sleep(1000);
                }

            }
        }
    }
}