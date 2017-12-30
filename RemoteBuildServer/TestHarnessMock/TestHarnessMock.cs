//////////////////////////////////////////////////////////////////////////////////
// TestHarnessMock.cs - executes the library files send from the Build Server   //
//                      ver 2.0                                                 //
//------------------------------------------------------------------------------//
//	Author:			Amritbani Sondhi,										    //
//					Graduate Student, Syracuse University					    //
//					asondhi@syr.edu											    //
//	Application:	CSE 681 Project #2, Fall 2017						    	//
//	Platform:		HP Envy x360, Core i7, Windows 10 Home					    //
//  Environment:    C#, Visual Studio 2017 RC                                   //
//////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Receives dll files from the Build Server, executes the library files and generates logs
 * at \RemoteBuildServer\Storage\TestHarnessStorage\TestLogs
 *
 * Public Methods:
 * ==============
 *      Class TestHarnessMock -
 *      - triggerTestHarness()  : calls the execute method
 *      
 * Private Methods:
 * ==============
 *      Class TestHarnessMock -
 *      - getSpecificFiles()    : gets all the files specified in the path for the pattern mentioned
 *      - executeLibraries()    : used for executing the dll files and saving the logs to the testersLocation
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          TestHarnessMock.cs, DllLoader.cs    
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 2.0 : Dec 06, 2017
 *      ver 1.0 : 05 Oct, 2017
 *          - first release
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DllLoaderDemo;
using MessagePassingComm;
using System.Threading;

namespace Federation
{
    public class TestHarnessMock : ChannelComm
    {
        // Endpoints
        private static string repositoryEndpoint { get; set; } = "http://localhost:8070/IPluggableComm";
        private static string clientEndpoint { get; set; } = "http://localhost:8060/IPluggableComm";
        private static string testHarnessEndpoint { get; set; } = "http://localhost:8090/IPluggableComm";

        // Storage
        private string testHarnessStorage { get; set; } = "..\\..\\..\\Storage\\TestHarnessStorage\\";
        private string repoLogStorage { get; set; } = "..\\..\\..\\Storage\\RepositoryStorage\\Logs\\TestLogs\\";
        private string clientLogStorage { get; set; } = "..\\..\\..\\Storage\\MockClientStorage\\TestLogs\\";
        private static string logLocation { get; set; } = "..\\..\\..\\Storage\\TestHarnessStorage\\TestLogs\\";

        public TestHarnessMock(string baseAddress, int port)
        {
            // checks if Directories are present or not
            if (!Directory.Exists(testHarnessStorage))
                Directory.CreateDirectory(testHarnessStorage);

            // Comm functions
            testHarnessEndpoint = baseAddress + ":" + port.ToString() + "/IPluggableComm";
            base.initializeComm(baseAddress, port);

            Console.Title = "TestHarness with EndPoint: " + testHarnessEndpoint;
        }

        // overriding from abstract base class 'ChannelComm' present in IMPCommService
        // demonstrates how the Repo will handled the messages received by it
        public override void requestHandler(CommMessage commMsg)
        {
            switch (commMsg.type)
            {
                case CommMessage.MessageType.connect:
                    {
                        Console.WriteLine("\n Connected to TestHarness!");
                        break;
                    }
                case CommMessage.MessageType.request:
                    {
                        handleRequestMessages(commMsg);
                        break;
                    }
                case CommMessage.MessageType.reply:
                    {
                        Console.WriteLine("\n TestHarness received a {0} message from {1}! ", commMsg.type, commMsg.from);
                        commMsg.show();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("\n Inside default in Switch-Case of TestHarness");
                        Console.WriteLine("\n You are missing some message case");
                        break;
                    }
            }
        }

        private void handleRequestMessages(CommMessage commMsg)
        {
            if (commMsg.command == "testRequest")
            {
                Console.WriteLine("\n TestHarness received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
                commMsg.show();

                CommMessage fileReq = new CommMessage(CommMessage.MessageType.request);
                fileReq = commMsg.clone();

                // Request the childbuilder for the .dll file
                fileReq.command = "dllRequest";
                fileReq.to = commMsg.from;
                fileReq.from = testHarnessEndpoint;

                Console.WriteLine("\n TestHarness asks {0} for files! ", commMsg.from);
                fileReq.show();
                commChannel.postMessage(fileReq);
            }
            else if (commMsg.command == "quitMessage")
            {
                Console.WriteLine("\n TestHarness received a {0} from EndPoint {1}! ", commMsg.command, commMsg.from);
                commMsg.show();
            }
            else if (commMsg.command == "dllReply")
            {
                Console.WriteLine("\n TestHarness received a {0} message from {1}! ", commMsg.command, commMsg.from);
                commMsg.show();

                // check if .dll file is received
                string dllName = commMsg.arguments[0];
                string absDllName = Path.Combine(testHarnessStorage, dllName);
                absDllName = Path.GetFullPath(absDllName);

                if (File.Exists(absDllName))
                {
                    Console.WriteLine("\n File received!");
                    triggerTestHarness(absDllName);
                }
                else
                {
                    Console.WriteLine("\n {0} file not received in TestHarness", dllName);
                }
            }
        }
        // gets all the files specified in the path for the pattern mentioned
        private List<string> getSpecificFiles(string path, string pattern)
        {
            if (pattern == "")
                pattern = "*.*";

            List<string> fileList = new List<string>();

            string[] tempFiles = Directory.GetFiles(path, pattern); // gets all the files using Directory
            for (int i = 0; i < tempFiles.Length; ++i)
            {
                tempFiles[i] = Path.GetFullPath(tempFiles[i]);      // replaces relative paths with the absolute paths
            }
            fileList.AddRange(tempFiles);

            return fileList;
        }

        // used for executing the dll files and saving the logs to the testersLocation
        [STAThread]
        private void executeLibraries(string absDllName)
        {
            // invoke loading the dlls for execution
            DllLoaderExec dllDemObj = new DllLoaderExec();

            // convert testers relative path to absolute path
            DllLoaderExec.testersLocation = Path.GetFullPath(DllLoaderExec.testersLocation);
            Console.Write(" Loading Test Modules from:\n\t {0}", DllLoaderExec.testersLocation);

            // run load and tests and saves the logs
            string result = dllDemObj.loadAndExerciseTesters(absDllName);

            Console.WriteLine(" {0}", result);
        }

        // calls the execute method
        public void triggerTestHarness(string absDllName)
        {
            Console.WriteLine(" =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Triggering Mock Test Harness!");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");

            // Invoke loading the dlls and execute
            executeLibraries(absDllName);

            // Send Log File to the Repository
            string logFileName = Path.GetFileName(absDllName);
            logFileName = logFileName.Replace("BuildRequest_", "TestLog_");
            logFileName = logFileName.Replace(".dll", ".log");

            string relLogLocation = "..\\..\\..\\Storage\\TestHarnessStorage\\TestLogs\\";

            if(!Directory.Exists(repoLogStorage))
            {
                Directory.CreateDirectory(repoLogStorage);
            }

            Console.WriteLine("\n Sending Test Log File to the Repository through Comm");

            Thread.Sleep(100);
            commChannel.postFile(logFileName, relLogLocation, repoLogStorage, repositoryEndpoint);

            Thread.Sleep(100);
            commChannel.postFile(logFileName, relLogLocation, clientLogStorage, repositoryEndpoint);

            // Send the Test status to the Client
            CommMessage logMsg = new CommMessage(CommMessage.MessageType.request);
            logMsg.to = clientEndpoint;
            logMsg.from = testHarnessEndpoint;
            logMsg.author = "TestHarness";
            logMsg.command = "testStatusMessage";
            logMsg.requestName = logFileName;
            commChannel.postMessage(logMsg);

            Console.WriteLine(" =========================================================================================");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(" Test Harness functionality Completed!");
            Console.WriteLine(" Demonstrated: getting dll files from the Build Server ");
            Console.WriteLine("\t And executing .dll files and creating log files which are present at: ");
            Console.WriteLine("\t RemoteBuildServer --> Storage --> TestHarnessStorage --> TestLogs");
            Console.ResetColor();
            Console.WriteLine(" =========================================================================================");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine(" Test Harness Functionality: ");
            Console.WriteLine(" ============================================================================================================================================= \n");

            TestHarnessMock testHarnessObj = new TestHarnessMock("http://localhost", 8090);

            System.Threading.Thread.Sleep(100000000);
            System.Threading.Thread.Sleep(100000000);
            System.Threading.Thread.Sleep(100000000);
            System.Threading.Thread.Sleep(100000000);

            Console.ReadKey();
        }

    }
}
