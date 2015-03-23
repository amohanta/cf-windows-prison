﻿using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace HP.WindowsPrison.Tests.Rules
{
    [TestClass]
    public class TestNetworkAndFirewall
    {
        Prison prison = null;

        [ClassInitialize]
        public static void PrisonInit(TestContext context)
        {
            Prison.Init();
        }

        [TestInitialize]
        public void PrisonTestSetup()
        {
            prison = new Prison();
            prison.Tag = "prtst";
        }

        [TestCleanup]
        public void PrisonTestCleanup()
        {
            if (prison != null)
            {
                prison.Destroy();
                prison.Dispose();
                prison = null;
            }
        }

        [TestMethod]
        [Ignore]
        public void LimitUploadSpeed()
        {
            // Arrange
            PrisonConfiguration prisonRules = new PrisonConfiguration();
            prisonRules.Rules = RuleTypes.Network;
            prisonRules.NetworkOutboundRateLimitBitsPerSecond = 8 * 1024 * 100;
            prisonRules.PrisonHomeRootPath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Wait a bit for the rule to take effect.
            Thread.Sleep(5000);

            // Act
            string exe = Utilities.CreateExeForPrison(
         @"
FtpWebRequest request = (FtpWebRequest)WebRequest.Create(""ftp://127.0.0.1/prisontests/uploadtest.txt"");
request.ConnectionGroupName = ""MyGroupName"";
request.UseBinary = true;
request.KeepAlive = true;
request.Method = WebRequestMethods.Ftp.UploadFile;

// This example assumes the FTP site uses anonymous logon.
request.Credentials = new NetworkCredential(""user"", ""password"");

request.ContentLength = 1024 * 1024;

Stream requestStream = request.GetRequestStream();

Stopwatch timer = Stopwatch.StartNew();

for (int i = 0; i < request.ContentLength / 256; i++)
{
    timer.Stop();

    byte[] data = new byte[256];
    Random random = new Random();
    random.NextBytes(data);

    timer.Start();

    requestStream.Write(data, 0, data.Length);
}
requestStream.Close();

FtpWebResponse response = (FtpWebResponse)request.GetResponse();
response.Close();

timer.Stop();

if ((1024 / timer.Elapsed.TotalSeconds) > 110)
{
    return 1;
}
", prison);

            Process process = prison.Execute(exe);
            process.WaitForExit();


            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }

        [TestMethod]
        [Ignore]
        public void AllowUnlimitedUploadSpeed()
        {
            // Arrange
            PrisonConfiguration prisonRules = new PrisonConfiguration();
            prisonRules.Rules = RuleTypes.None;
            prisonRules.NetworkOutboundRateLimitBitsPerSecond = 8 * 1024 * 100;
            prisonRules.PrisonHomeRootPath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Wait a bit for the rule to take effect.
            Thread.Sleep(5000);

            // Act
            string exe = Utilities.CreateExeForPrison(
         @"
FtpWebRequest request = (FtpWebRequest)WebRequest.Create(""ftp://127.0.0.1/prisontests/uploadtest.txt"");
request.ConnectionGroupName = ""MyGroupName"";
request.UseBinary = true;
request.KeepAlive = true;
request.Method = WebRequestMethods.Ftp.UploadFile;

// This example assumes the FTP site uses anonymous logon.
request.Credentials = new NetworkCredential(""user"", ""password"");

request.ContentLength = 1024 * 1024;

Stream requestStream = request.GetRequestStream();

Stopwatch timer = Stopwatch.StartNew();

for (int i = 0; i < request.ContentLength / 256; i++)
{
    timer.Stop();

    byte[] data = new byte[256];
    Random random = new Random();
    random.NextBytes(data);

    timer.Start();

    requestStream.Write(data, 0, data.Length);
}
requestStream.Close();

FtpWebResponse response = (FtpWebResponse)request.GetResponse();
response.Close();

timer.Stop();

if ((1024 / timer.Elapsed.TotalSeconds) > 110)
{
    return 1;
}
", prison);

            Process process = prison.Execute(exe);
            process.WaitForExit();


            // Assert
            Assert.AreNotEqual(0, process.ExitCode);
        }

        [TestMethod]
        [Ignore]
        public void AllowLargerUploadSpeedOnSecondPort()
        {
            // Arrange
            PrisonConfiguration prisonRules = new PrisonConfiguration();
            prisonRules.Rules = RuleTypes.Httpsys | RuleTypes.Network;
            prisonRules.NetworkOutboundRateLimitBitsPerSecond = 8 * 1024 * 100;
            prisonRules.AppPortOutboundRateLimitBitsPerSecond = 8 * 1024 * 200;
            prisonRules.UrlPortAccess = 56444;
            prisonRules.PrisonHomeRootPath = @"C:\Workspace\dea_security\PrisonHome";

            prison.Lockdown(prisonRules);

            // Act
            string exe = Utilities.CreateExeForPrison(
         @"

HttpListener actualServer = null;
int port = 56444;

int actualServerPort = port;
actualServer = new HttpListener();
actualServer.Prefixes.Add(string.Format(""http://*:{0}/"", port));
actualServer.Start();


byte[] reply = new byte[1024 * 1024];
Random rnd = new Random();
rnd.NextBytes(reply);

Console.WriteLine(""Done loading"");

int requests = 0;
while (requests < 2)
{
    HttpListenerContext context = actualServer.GetContext();
    context.Response.StatusCode = 200;
    if (requests == 0)
    {
        context.Response.OutputStream.Write(reply, 0, reply.Length);
    }
    context.Response.OutputStream.Close();
    requests++;
}
if (actualServer != null)
{
    actualServer.Stop();
}

FtpWebRequest request = (FtpWebRequest)WebRequest.Create(""ftp://127.0.0.1/prisontests/uploadtest.txt"");
request.ConnectionGroupName = ""MyGroupName"";
request.UseBinary = true;
request.KeepAlive = true;
request.Method = WebRequestMethods.Ftp.UploadFile;

// This example assumes the FTP site uses anonymous logon.
request.Credentials = new NetworkCredential(""user"", ""password"");

request.ContentLength = 1024 * 1024;

Stream requestStream = request.GetRequestStream();

Stopwatch timer = Stopwatch.StartNew();

for (int i = 0; i < request.ContentLength / 256; i++)
{
    timer.Stop();

    byte[] data = new byte[256];
    Random random = new Random();
    random.NextBytes(data);

    timer.Start();

    requestStream.Write(data, 0, data.Length);
}
requestStream.Close();

FtpWebResponse response = (FtpWebResponse)request.GetResponse();
response.Close();

timer.Stop();
            
if ((1024 / timer.Elapsed.TotalSeconds) > 110)
{
    return 1;
}
", prison);

            Process process = prison.Execute(exe);

            // Wait a bit for everything to be setup.
            Thread.Sleep(5000);

            Stopwatch timer = Stopwatch.StartNew();

            WebClient client = new WebClient();
            client.Proxy = new WebProxy("http://192.168.1.119:8080");

            byte[] data = client.DownloadData("http://10.0.0.10:56444/");
            timer.Stop();

            Assert.IsTrue(
                ((1024 / timer.Elapsed.TotalSeconds) < 210) &&
                ((1024 / timer.Elapsed.TotalSeconds) > 110),
                string.Format("Downloaded {0} bytes in {1} seconds, at a rate of {2} KB/s", data.Length, timer.Elapsed.TotalSeconds, 1024 / timer.Elapsed.TotalSeconds));

            client.DownloadData("http://localhost:56444/");
      
            process.WaitForExit();
            // Assert
            Assert.AreEqual(0, process.ExitCode);
        }
    }
}
