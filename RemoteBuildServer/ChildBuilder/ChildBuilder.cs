//////////////////////////////////////////////////////////////////////////////////////
// ChildBuilder.cs :        builds projects using .cs files                         //
//                          ver 4.0                                                 //
//----------------------------------------------------------------------------------//
//  Source:                 Ammar Salman, EECS Department, Syracuse University      //
//                          (313)-788-4694, hoplite.90@hotmail.com                  //
//	Author:			        Amritbani Sondhi,										//
//					        Graduate Student, Syracuse University					//
//					        asondhi@syr.edu											//
//	Application:	        CSE 681 Project #3, Fall 2017							//
//	Platform:		        HP Envy x360, Core i7, Windows 10 Home					//
//  Environment:            C#, Visual Studio 2017 RC                               //
//////////////////////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* ===================
* This package demonstrates receiving CommMessages from the Mother Builder for handling build requests for
* the Remote Build Server.
* The Child Builders will be run on separate processes. Each child builder will receive a Build Request Message from
* the Mother Builder. It will then as the Mock Repository for the files it needs to Compile a Request.
* When the Repository sends appropriate files to it, it will then proceed with processing the Build Request.
* 
* Methods:
* ==============
*      Class ChildBuilder -
*      - ChildBuilder()        - initializes communication objects, endpoints and storage directories
*      - requestHandler()      - overridden from ChannelComm Abstract class, shows how comm messages will
*                                be handled
*      - triggerBuild()        - builds the .cs files sent by the repository
*      
* Build Process:
* ==============
*	- Required Files:
*          ProjectBuildServer.cs
* 	- Build commands:
*		    devenv RemoteBuildServer.sln
*		    
* Maintenance History:
* ===================
*      ver 4.0 : Dec 06, 2017
*      ver 3.0 : Nov 01, 2017
*      ver 2.0 : Oct 05, 2017
*/

using System;
using System.Collections.Generic;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;
using System.IO;
using System.Security;
using MessagePassingComm;
using System.Threading;
using System.Diagnostics;
using System.Linq;

namespace Federation
{
    // Demonstrates how the communication will happen with Child Builder Processes
    public class ChildBuilder : ChannelComm
    {
        // Endpoints
        private static string motherBuilderEndpoint { get; set; } = "http://localhost:8080/IPluggableComm";
        private static string repositoryEndpoint { get; set; } = "http://localhost:8070/IPluggableComm";
        private static string childBuilderEndpoint { get; set; } = null;
        private static string testHarnessEndpoint { get; set; } = "http://localhost:8090/IPluggableComm";

        // Storages
        private static string childBuilderStorage { get; set; } = null;
        private static string testHarnessStorage { get; set; } = "..\\..\\..\\Storage\\TestHarnessStorage\\";
        private static string logLocation { get; set; } = "..\\..\\..\\Storage\\BuilderStorage\\BuildLogs\\";
        private string repoLogStorage { get; set; } = "..\\..\\..\\Storage\\RepositoryStorage\\Logs\\BuildLogs\\";
        private static string buildRequestStorage { get; set; } = "";

        // Files
        private string dllfileToSend { get; set; } = "";
        private static string logFile { get; set; } = "BuildLog_";
        private static string buildRequestFileName { get; set; } = "";
        private static string statusMessage { get; set; } = "";


        public ChildBuilder(string baseAddress, int port)
        {
            // creates separate directories for demonstrating separate Child Building Processes
            childBuilderStorage = "..\\..\\..\\Storage\\BuilderStorage\\Child_" + port;

            string absChildBuilderStorage = Path.GetFullPath(childBuilderStorage);

            // checks if Directories are present or not
            if (!Directory.Exists(absChildBuilderStorage))
                Directory.CreateDirectory(absChildBuilderStorage);

            // Comm functions
            childBuilderEndpoint = baseAddress + ":" + port.ToString() + "/IPluggableComm";
            initializeComm(baseAddress, port);

            Console.Title = "Child Builder with EndPoint: " + childBuilderEndpoint;

            // Sends Acknowledgement to the Mother Builder that it is Open for receiving Build Requests
            CommMessage readyAck = new CommMessage(CommMessage.MessageType.request, motherBuilderEndpoint, childBuilderEndpoint, "Amrit", "", "readyRequest");
            readyAck.show();
            base.commChannel.postMessage(readyAck);
        }

        // overriding from abstract base class 'ChannelComm' present in IMPCommService
        public override void requestHandler(CommMessage commMsg)
        {
            // demonstrates how the Child Builder will handle different message types and commands
            switch (commMsg.type)
            {
                case CommMessage.MessageType.connect:
                    {
                        Console.WriteLine("\n Connected to a ChildBuilder!");
                        break;
                    }
                case CommMessage.MessageType.request:
                    {
                        handleRequestMessage(commMsg);
                        break;
                    }
                case CommMessage.MessageType.reply:
                    {
                        handleMessageReply(commMsg);
                        break;
                    }
                case CommMessage.MessageType.closeSender:
                    {
                        Console.WriteLine("ChildBuilder {0} received a {1} type message from {2}! ", childBuilderEndpoint, commMsg.type, commMsg.from);
                        commChannel.close();
                        break;
                    }
                case CommMessage.MessageType.closeReceiver:
                    {
                        Console.WriteLine("ChildBuilder {0} received a {1} type message from {2}! ", childBuilderEndpoint, commMsg.type, commMsg.from);
                        commChannel.close();
                        Process.GetCurrentProcess().Kill();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Inside default in Switch-Case of ChildBuilder {0}", childBuilderEndpoint);
                        Console.WriteLine("You are missing some message case");
                        break;
                    }
            }
        }

        private void handleRequestMessage(CommMessage commMsg)
        {
            if (commMsg.command == "buildRequest")
            {
                handleBuildRequest(commMsg);
            }
            else if (commMsg.command == "dllRequest")
            {
                handleDLLRequest(commMsg);
            }
            else if (commMsg.command == "quitMessage")
            {
                handleQuitMessage(commMsg);
            }

        }

        private void handleBuildRequest(CommMessage commMsg)
        {
            Console.WriteLine("\n ChildBuilder {0} received a {1} command from {2}!", childBuilderEndpoint, commMsg.command, commMsg.from);
            commMsg.show();

            buildRequestFileName = commMsg.requestName;
            buildRequestStorage = childBuilderStorage + "//" + buildRequestFileName;
            buildRequestStorage = Path.GetFullPath(buildRequestStorage);

            if (!Directory.Exists(buildRequestStorage))
                Directory.CreateDirectory(buildRequestStorage);

            //change the command to fileRequest and send it to Repo
            commMsg.to = repositoryEndpoint;
            commMsg.from = childBuilderEndpoint;
            commMsg.command = "fileRequest";

            Console.WriteLine("\n Sending a fileRequest from ChildBuilder {0} to Repository \n", childBuilderEndpoint);
            commMsg.show();
            base.commChannel.postMessage(commMsg);

        }

        private void handleDLLRequest(CommMessage commMsg)
        {
            Console.WriteLine("\n ChildBuilder received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
            commMsg.show();

            testHarnessEndpoint = commMsg.from;

            // The message will go as a reply to the TestHarness
            CommMessage replyMsg = new CommMessage(CommMessage.MessageType.request);

            replyMsg.to = testHarnessEndpoint;
            replyMsg.from = childBuilderEndpoint;
            replyMsg.author = commMsg.author;
            replyMsg.command = "dllReply";
            replyMsg.requestName = commMsg.requestName;
            replyMsg.arguments = commMsg.arguments;
            replyMsg.threadId = commMsg.threadId;
            replyMsg.errorMsg = commMsg.errorMsg;

            // populate the list of files
            dllfileToSend = commMsg.arguments[0];

            Console.WriteLine("\n ChildBuilder replies {0} with the files requested", replyMsg.to);
            sendDllFiles(dllfileToSend);

            Console.WriteLine("\n Sending the following files to {0} \n", testHarnessStorage);
            base.commChannel.postMessage(replyMsg);

        }

        private void handleQuitMessage(CommMessage commMsg)
        {
            Console.WriteLine("\n ChildBuilder {0} received a {1} command from {2}! ", childBuilderEndpoint, commMsg.command, commMsg.from);
            commMsg.show();
            Console.WriteLine("\n Closing ChildBuilder Process - {0}!", childBuilderEndpoint);

            try
            {
                commChannel.close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Process.GetCurrentProcess().Kill();
            }

        }

        private void handleMessageReply(CommMessage commMsg)
        {
            Console.WriteLine("ChildBuilder {0} received a {1} message from {2}! ", childBuilderEndpoint, commMsg.type, commMsg.from);
            commMsg.show();

            if (commMsg.command == "fileRequest")
            {
                triggerBuild(commMsg.arguments);
            }
            else
            {
                Console.WriteLine("\n The reply received was not a fileRequest command. Hence it didn't build");
            }

        }

        private void sendDllFiles(string dllfileToSend)
        {
            // specify the list of files which are being sent
            Console.WriteLine("\n\t {0}", dllfileToSend);

            // Change the requestName without the .xml
            string buildRequestFolder = dllfileToSend;
            string sTo = ".dll";
            int pTo = buildRequestFolder.IndexOf(sTo);
            buildRequestFolder = buildRequestFolder.Substring(0, pTo);
            string fromStorage = Path.Combine(childBuilderStorage, buildRequestFolder);

            try
            {
                // Send files to the ChildBuilder
                Thread.Sleep(100);
                base.commChannel.postFile(dllfileToSend, fromStorage, testHarnessStorage, testHarnessEndpoint);
                Console.WriteLine("\n\t File Sent Successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n An Exception Occured: {0}", ex.Message);
            }
        }

        // Processes Build Requests
        public void triggerBuild(List<string> arguments)
        {   demo1();
            //Check if files are present in the folder
            foreach (string file in arguments)
            {   string absFilePath = buildRequestStorage + "\\" + file;
                absFilePath = Path.GetFullPath(absFilePath);
                if (File.Exists(absFilePath))
                {Console.WriteLine("\n ChildBuilder {0} received {1} file from Repository!", childBuilderEndpoint, file);}
                else
                {Console.WriteLine("\n {0} file didn't come from the repository!", file);}
            }
            //get all the.xml files ie.BuildRequests with their full paths
            List<string> buildRequestFileList = new List<string>();
            buildRequestFileList = getSpecificFiles(buildRequestStorage, "*.xml");
            // create a file for Saving Build Logs
            createLogFile();
            //Build all the TestRequest xml files
            bool status = startBuild(buildRequestFileList);
            if (status == true)
            {                // Send the DLL file to Test Harness
                sendTestRequestToTestHarness();
                // Send Log File to the Repository
                string logFileName = Path.GetFileName(buildRequestFileName);
                logFileName = logFileName.Replace("BuildRequest_", "BuildLog_");
                logFileName = logFileName.Replace(".xml", ".log");
                logFileName = logFileName + ".log";
                string logFolder = "BuildLogs\\";
                string relLogLocation = Path.Combine(childBuilderStorage, logFolder);
                if (!Directory.Exists(repoLogStorage))
                {                    Directory.CreateDirectory(repoLogStorage); }

                Console.WriteLine("\n Sending Build Log File to the Repository through Comm");
                Thread.Sleep(100);
                commChannel.postFile(logFileName, relLogLocation, repoLogStorage, repositoryEndpoint);
                demo2();
                //--------------------- send statusMessage to Client by post file through communication
            }
            // Send an acknowledgement to the Mother Builder that this Child Builder is ready to receive the next build request
            CommMessage readyAck = new CommMessage(CommMessage.MessageType.request, motherBuilderEndpoint, childBuilderEndpoint, "Amrit", "", "readyRequest");
            base.commChannel.postMessage(readyAck);
            Console.WriteLine("\n Sending Ready Message from {0} triggerBuild", childBuilderEndpoint);
        }

        private void demo1()
        {
            Console.WriteLine("\n =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Triggering ProjectBuildServer!");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");
        }
        private void demo2()
        {
            Console.WriteLine("\n =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Child Build Completed!");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");
        }

        //gets all the files specified in the path for the pattern mentioned
        private List<string> getSpecificFiles(string path, string pattern)
        {
            List<string> fileList = new List<string>();

            string[] tempFiles = Directory.GetFiles(path, pattern);
            for (int i = 0; i < tempFiles.Length; ++i)
            {
                tempFiles[i] = Path.GetFullPath(tempFiles[i]);
            }
            fileList.AddRange(tempFiles);

            return fileList;
        }

        // creates a Build Log File
        static void createLogFile()
        {
            string logFolder = "BuildLogs/";
            logLocation = Path.Combine(childBuilderStorage, logFolder);
            // Create a LogFile Path
            logLocation = System.IO.Path.GetFullPath(logLocation);
            string logFileName = buildRequestFileName;
            logFileName = logFileName.Replace("BuildRequest_", "BuildLog_");
            logFileName = logFileName + ".log";
            logFile = System.IO.Path.Combine(logLocation, logFileName);

            if (!Directory.Exists(logLocation))
                Directory.CreateDirectory(logLocation);
        }

        // triggers the Build Process from the xml file and gets the status
        static bool startBuild(List<string> testRequestFileList)
        {
            bool status = false;
            // Build all the TestRequest xml files
            foreach (string file in testRequestFileList)
            {
                try
                {
                    Console.WriteLine("\n Building {0}", file);
                    BuildXml(file);
                    Console.WriteLine("\n --------------------------------------------------------------------------------------------------");
                    status = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n An error occured while trying to build the xml file: {0}", file);
                    Console.WriteLine(" Details: {0}\n\n", ex.Message);
                }
            }
            return status;
        }

        /* 
         * This method uses MSBuild to build a .xml file. The xml file is configured to build as Debug/AnyCPU
         * In the xml file, the OutputPath is set to "\RemoteBuildServer\Storage\BuilderStorage" and 
         * will be build into a DLL library
         */
        static void BuildXml(string buildRequestFiles)
        {
            Process proc = new Process();
            Console.WriteLine("\n buildRequestFile Name: {0}", buildRequestFiles);

            buildRequestFileName = Path.GetFileName(buildRequestFiles); buildRequestFileName = buildRequestFileName.Substring(0, buildRequestFileName.Length - 4); // removes .xml from the name
            buildRequestFileName = buildRequestFileName.Replace(":", "-"); buildRequestFileName = buildRequestFileName.Replace("/", "-"); buildRequestFileName = buildRequestFileName.Replace(" ", "_");

            string builderStorageDir = buildRequestStorage + "/"; string buildRequestDir = ""; buildRequestDir = Path.Combine(childBuilderStorage, buildRequestFileName); buildRequestDir = Path.GetFullPath(buildRequestDir); string currentDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(buildRequestStorage)){ Directory.SetCurrentDirectory(buildRequestDir); } else{    Console.WriteLine("Invalid Build Request Directory");}
            string dll = buildRequestFileName + ".dll";
            string compileCommand = "/target:library /out:" + dll + " " + "*.cs"; //+ compileCommand;
            // csc details
            string cscPath = "..\\..\\..\\..\\packages\\Microsoft.Net.Compilers.2.4.0\\tools\\csc.exe";
            cscPath = Path.GetFullPath(cscPath);
            Console.WriteLine(" =============================================================================================================================================");
            Console.WriteLine("\n Attempting to start Compile Process: ");
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.Verb = "runas";
                startInfo.FileName = cscPath;
                startInfo.Arguments = compileCommand;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;         // redirect the launched process's i/p, o/p and error o/p
                startInfo.WorkingDirectory = buildRequestDir;
                // Starts a new Process
                Process p = Process.Start(startInfo);
                string result = p.StandardOutput.ReadToEnd();
                // Save the Status
                if (result.Contains("error"))
                {   statusMessage = "An Error Occurred!"; // it will automatically notify 
                }
                else if (result.Contains("warning"))
                {   statusMessage = "Build Succeeded with Warnings!";
                    using (StreamWriter writetext = new StreamWriter(logFile, append: true))
                    {   writetext.WriteLine(statusMessage);
                        writetext.Close();}}
                else {   statusMessage = "Build Succeeded!";}
                result = result + statusMessage;
                Console.WriteLine(result);
                // save in a logfile
                using (StreamWriter writetext = new StreamWriter(logFile, append: true))
                {   writetext.WriteLine(result);
                    writetext.Close(); }
            }
            catch (Exception ex)
            {   Console.WriteLine("\n {0}", ex.Message); }
            Directory.SetCurrentDirectory(currentDir); // set directory to the original directory
        }

        // sends all the dll files created, to the TestHarness
        private void sendTestRequestToTestHarness()
        {
            List<string> libraryFileList = new List<string>();
            string fromLibraryPath = Path.GetFullPath(buildRequestStorage);

            // get all the .dll files ie. library files with their full paths
            libraryFileList = getSpecificFiles(fromLibraryPath, "*.dll");

            foreach (string file in libraryFileList)
            {
                Console.WriteLine("\n ---------------------------------------------------");
                Console.WriteLine("Dll FileName : {0} \n", file);
                CommMessage msg = new CommMessage(CommMessage.MessageType.request);
                msg.from = childBuilderEndpoint;
                msg.to = testHarnessEndpoint;
                msg.command = "testRequest";

                // Change the reqName without the .xml
                string reqName = Path.GetFileName(file);
                string sTo = ".dll";
                int pTo = reqName.IndexOf(sTo);
                reqName = reqName.Substring(0, pTo);
                msg.requestName = reqName;

                msg.arguments.Add(Path.GetFileName(file));
                msg.show();
                commChannel.postMessage(msg);
            }
        }

        // command line will be in the form of:
        // http://localhost 8081
        static void Main(string[] args)
        {
            string baseAddress = args[0].ToString();
            int port = Convert.ToInt32(args[1]);

            string endpt = baseAddress + port.ToString();
            Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine(" ChildBuilder with Endpoint {0} Functionality: ", endpt);
            Console.WriteLine(" ============================================================================================================================================= \n");

            // Calls Build Server and gets it running
            ChildBuilder childBuildObj = new ChildBuilder(baseAddress, port);

            Thread.Sleep(100000000);

            Console.WriteLine(" =========================================================================================");
            Console.WriteLine(" Done Building project Build Server");

            Console.ReadLine();
        }
    }
}
