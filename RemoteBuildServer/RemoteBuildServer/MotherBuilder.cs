//////////////////////////////////////////////////////////////////////////////////////
// MotherBuilder.cs -    creates Process Pool, and handles requests using queues    //
//                       ver 4.0                                                    //
//----------------------------------------------------------------------------------//
//  Source:         Prof. Jim Fawcett, CST 4-187, jfawcett@twcny.rr.com             //
//	Author:			Amritbani Sondhi,										        //
//					Graduate Student, Syracuse University					        //
//					asondhi@syr.edu											        //
//	Application:	CSE 681 Project #3, Fall 2017							        //
//	Platform:		HP Envy x360, Core i7, Windows 10 Home					        //
//  Environment:    C#, Visual Studio 2017 RC                                       //
//////////////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 *      This package demonstrates the functionality of the Mother Builder. It receives the build request messages from
 *      the repository. It first saves all these requests in a Queue, so none of the requests are lost even if there are large
 *      no. of requests coming in. The motherbuilder also sets up the Child Builders, for processing these requests. Once the
 *      communication is established, and it receives a ready message from the Child, it dequeues a request and sends it to the
 *      Child for processing it.
 *      If a quit message is received from the repository, it first Kills all the Child Processes and then closes it's Comm Channel
 * 
 * Public Methods:
 * ==============
 *      Class MotherBuilder -
 *      - MotherBuilder()               : initializes Endpoints and comm objects, creates Child Processes
 *      - createChildProcesses()        : similar to Spawn Proc project provided by Prof. Fawcett
 *      - initializePortList()          : Initializes Ports for Child Processes
 *      - requestHandler()              : overridden from ChannelComm Abstract class, shows how comm messages will
 *                                        be handled
 *      - sendRequestToChildProcs()     : dequeues next Build Request from the queue and sends it to the Child
 *      
 * Build Process:
 * ==============
 *	- Required Files:
 *          RepoMock.cs
 * 	- Build commands:
 *		    devenv RemoteBuildServer.sln
 *		    
 * Maintenance History:
 * ===================
 *      ver 4.0 : Dec 06, 2017
 *      ver 3.0 : Nov 01, 2017
 *      ver 2.0 : Oct 05, 2017
 *      ver 1.0 : Sep 07, 2017
 *          - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MessagePassingComm;
using SWTools;
using System.Threading;

namespace Federation
{
    // Demonstrates how MotherBuilder will create and manage Process Pools, handle request and ready queues and the comm
    public class MotherBuilder : ChannelComm
    {
        // Endpoints
        private static string motherBuilderEndpoint { get; set; } = "http://localhost:8080/IPluggableComm";
        private static string baseAddress { get; set; } = null;
        private static List<int> childPortList { get; set; } = new List<int>();

        private const string fileName = "..\\..\\..\\ChildBuilder\\bin\\Debug\\ChildBuilder.exe";

        // Queues for Requests and Acknowledgement Messages
        private static BlockingQueue<string> readyQueue = null;
        private static BlockingQueue<CommMessage> requestQueue = null;

        private bool isThreadRunning = false;

        // initializes Endpoints and comm objects, creates Child Processes
        public MotherBuilder(string baseAdd, int port, int processPoolSize)
        {
            baseAddress = baseAdd;
            motherBuilderEndpoint = baseAddress + ":" + port.ToString() + "/IPluggableComm";
            initializePortList(port, processPoolSize);

            // creates both queues in the MotherBuilder
            requestQueue = new BlockingQueue<CommMessage>();
            readyQueue = new BlockingQueue<string>();

            // it initializes comm objects as well as starts blocking for receiving messages
            base.initializeComm(baseAddress, port);

            bool resultChildProc = createChildProcesses(baseAddress, processPoolSize);

            Console.Title = "MotherBuilder with Endpoint: " + motherBuilderEndpoint;

            channelCommThread = new Thread(sendRequestToChildProcs);
        }

        // Same a Spawn Proc project provided by Prof. Fawcett
        static bool createChildProcesses(string baseAddress, int processPoolSize)
        {
            bool status = false;
            Process proc = new Process();
            string absFileSpec = Path.GetFullPath(fileName);

            for (int i = 0; i < processPoolSize; i++)
            {
                Console.WriteLine(" =============================================================================================================================================");
                Console.WriteLine("\n Attempting to start Process {0}", i);
                Console.WriteLine("\n ie. {0}", absFileSpec);

                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(fileName);
                    startInfo.Verb = "runas";
                    startInfo.Arguments = baseAddress + " " + childPortList[i].ToString();
                    
                    // Starts a new Process
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n {0}", ex.Message);
                    status = false;
                }
                status = true;
            }
            return status;
        }

        // Initializes Ports for Child Processes
        private void initializePortList(int port, int processPoolSize)
        {
            for (int i = 0; i < processPoolSize; i++)
            {
                port++;
                childPortList.Add(port);
            }
        }

        // overriding from abstract base class 'ChannelComm' present in IMPCommService
        public override void requestHandler(CommMessage commMsg)
        {
            switch (commMsg.type)
            {
                case CommMessage.MessageType.connect:
                    {
                        Console.WriteLine("\n {0} Connected with MotherBuilder! ", commMsg.from);
                        break;
                    }
                case CommMessage.MessageType.request:
                    {
                        handleRequestMessages(commMsg);
                        break;
                    }
                case CommMessage.MessageType.reply:
                    {
                        Console.WriteLine("MotherBuilder received a {0} message from {1}! ", commMsg.type, commMsg.from);
                        break;
                    }
                case CommMessage.MessageType.closeSender:
                    {
                        handleCloseSender(commMsg);
                        break;
                    }
                case CommMessage.MessageType.closeReceiver:
                    {
                        handleCloseReceiver(commMsg);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Inside default in Switch-Case");
                        Console.WriteLine("You are missing some message case");
                        break;
                    }
            }
        }

        private void handleRequestMessages(CommMessage commMsg)
        {
            if (commMsg.command == "buildRequest")
            {
                Console.WriteLine(" =============================================================================================================================================");
                Console.WriteLine("\n MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);
                commMsg.show();

                // Save the Build Request in a queue
                requestQueue.enQ(commMsg);
                if (!isThreadRunning)
                {
                    channelCommThread.Start();
                    isThreadRunning = true;
                }
            }
            else if (commMsg.command == "readyRequest")
            {
                Console.WriteLine(" =============================================================================================================================================");
                Console.WriteLine("\n MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);
                commMsg.show();

                // A ready request received from Child Builder, enQueuing it in the readyQueue
                readyQueue.enQ(commMsg.from);
            }
            else if (commMsg.command == "testRequest")
            {
                Console.WriteLine("\n MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);
            }
            else if (commMsg.command == "showRequest")
            {
                Console.WriteLine("\n MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);
                Console.WriteLine("\n This Message Request should not be handled in MotherBuilder");
                Console.WriteLine("\n Check and change the functionality!");
                commMsg.show();
            }
            else if (commMsg.command == "quitMessage")
            {
                handleQuitMessage(commMsg);
            }
            else
            {
                Console.WriteLine("MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);
                Console.WriteLine("Inside else case for Request Message");
                Console.WriteLine("You are missing some message case!");
            }

        }

        private void handleQuitMessage(CommMessage commMsg)
        {
            Console.WriteLine("\n MotherBuilder received a {0} from {1}! ", commMsg.command, commMsg.from);

            string childBuilderEndpoint = null;
            for (int a = 0; a < childPortList.Count; a++)
            {
                childBuilderEndpoint = baseAddress + ":" + childPortList[a].ToString() + "/IPluggableComm";
                Console.WriteLine("\n Sending Quit Message to Childbuilder {0}", childBuilderEndpoint);

                // Send close Receiver message to all the Child Builders
                commMsg.to = childBuilderEndpoint;
                commMsg.from = motherBuilderEndpoint;

                commChannel.postMessage(commMsg);
            }

            Thread.Sleep(500);
            try
            {
                Console.WriteLine("\n Closing MotherBuilder!");
                commChannel.close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Process.GetCurrentProcess().Kill();
            }

        }

        private void handleCloseSender(CommMessage commMsg)
        {
            Console.WriteLine("MotherBuilder received a {0} message from {1}! ", commMsg.type, commMsg.from);

            string childBuilderEndpoint = null;
            for (int a = 0; a < childPortList.Count; a++)
            {
                childBuilderEndpoint = baseAddress + ":" + childPortList[a].ToString() + "/IPluggableComm";

                // Send Close Sender message to all the childBuilders
                commMsg.to = childBuilderEndpoint;
                commChannel.postMessage(commMsg);
            }

        }
        
        
        private void handleCloseReceiver(CommMessage commMsg)
        {
            Console.WriteLine("MotherBuilder received a {0} message from {1}! ", commMsg.type, commMsg.from);
            string childBuilderEndpoint = null;
            for (int a = 0; a < childPortList.Count; a++)
            {
                childBuilderEndpoint = baseAddress + ":" + childPortList[a].ToString() + "/IPluggableComm";

                // Send close Receiver message to all the Child Builders
                commMsg.to = childBuilderEndpoint;
                commChannel.postMessage(commMsg);
            }
        }
        
        
        // dequeues next Build Request from the queue and sends it to the Child
        private void sendRequestToChildProcs()
        {
            isThreadRunning = true;
            string childProcEndpoint = null;
            while (true)
            {
                // Dequeing from Request Queue when a request is received
                CommMessage commMsg = requestQueue.deQ();

                // change the from -> MotherBuilder before sending
                commMsg.from = motherBuilderEndpoint;

                Console.WriteLine(" =============================================================================================================================================");
                Console.WriteLine("\n Printing Request Message which is assigned to a Child Proc");

                // Dequeing from Ready Queue when a Child Proc is ready to receive a Request
                childProcEndpoint = readyQueue.deQ();

                // send requestMessage to ChildBuilder
                commMsg.to = childProcEndpoint.ToString();
                commMsg.show();

                commChannel.postMessage(commMsg);
            }
        }

        // main method accept no of processes in it's arguments
        static void Main(string[] args)
        {
            Console.Title = "SpawnProc";
            Console.WriteLine("\n =============================================================================================================================================");
            Console.WriteLine(" Mother Builder Functionality: ");
            Console.WriteLine(" ============================================================================================================================================= \n");

            int numOfProc = Convert.ToInt32(args[0]);

            MotherBuilder builderObj = new MotherBuilder("http://localhost", 8080, numOfProc);
            Thread.Sleep(100000000);
            Thread.Sleep(100000000);
            Thread.Sleep(100000000);
            Thread.Sleep(100000000);

            Console.WriteLine("\n Press key to exit");
            Console.ReadKey();
        }
    }
}
