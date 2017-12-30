//////////////////////////////////////////////////////////////////////////////////////
// IMPCommService.cs - service interface for MessagePassingComm                     //
//                     ver 2.1                                                      //
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
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 * 
 * This package provides:
 * ----------------------
 * - ClientEnvironment   : client-side path and address
 * - ServiceEnvironment  : server-side path and address
 * - IMessagePassingComm : interface used for message passing and file transfer
 * - CommMessage         : class representing serializable messages
 * 
 * - ChannelComm abstract class:
 *      - ChannelComm()         - creates new Thread to process commHandleRequest
 *      - commHandleRequest()   - it waits for a message, if received, it calls the requestHandler to handle the message
 *      - requestHandler()      - provides implementation of how comm messages will be handled
 *      - initializeComm()      - initializes Comm Objects and starts the thread to block of getMessage for receiving messages
 * 
 * Required Files:
 * ---------------
 * - IPCommService.cs         : Service interface and Message definition
 * 
 * Maintenance History:
 * --------------------
 *      ver 3.0 : Nov 01, 2017
 *      - added abstract ChannelComm class
 *      ver 2.0 : 19 Oct 2017
 *      - renamed namespace and ClientEnvironment
 *      - added verbose property to ClientEnvironment
 *      ver 1.0 : 15 Jun 2017
 *      - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace MessagePassingComm
{
    using Command = String;             // Command is key for message dispatching, e.g., Dictionary<Command, Func<bool>>
    using EndPoint = String;            // string is (ip address or machine name):(port number)
    using Argument = String;
    using ErrorMessage = String;
    using RequestMsg = String;

    public struct ClientEnvironment
    {
        public static string fileStorage { get; } = "../../../Storage/ClientFileStore";
        public static long blockSize { get; } = 1024;
        public static string baseAddress { get; set; }
        public static bool verbose { get; set; }
    }

    public struct ServiceEnvironment
    {
        public static string fileStorage  { get; } = "../../../Storage/ServiceFileStore";
        public static string baseAddress { get; set; }
    }

    [ServiceContract(Namespace = "MessagePassingComm")]
    public interface IMessagePassingComm
    {
        /*----< support for message passing >--------------------------*/
        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract
        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/
        [OperationContract]
        bool openFileForWrite(string name, string toPath);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();
    }

    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect,           // initial message sent on successfully connecting
            [EnumMember]
            request,           // request for action from receiver
            [EnumMember]
            reply,             // response to a request
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            closeReceiver      // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/
        public CommMessage(MessageType mt)
        {
            type = mt;
        }

        /*----< constructor requires message type >--------------------*/
        public CommMessage(MessageType type, string to, string from, string author, string requestName, Command command)
        {
            this.type = type;
            this.to = to;
            this.from = from;
            this.author = author;
            this.requestName = requestName;
            this.command = command;
        }

        /*----< data members - all serializable public properties >----*/
        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public string requestName { get; set; }

        [DataMember]
        public Command command { get; set; }

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        public void show()
        {
            Console.WriteLine("\n  CommMessage:");
            Console.WriteLine("    MessageType      : {0}", type.ToString());
            Console.WriteLine("    to               : {0}", to);
            Console.WriteLine("    from             : {0}", from);
            Console.WriteLine("    author           : {0}", author);
            Console.WriteLine("    requestName      : {0}", requestName);
            Console.WriteLine("    command          : {0}", command);
            Console.Write("    arguments            :");
            if (arguments.Count > 0)
                Console.WriteLine("");
            foreach (string arg in arguments)
                Console.WriteLine("\t\t  {0} ", arg);
            Console.WriteLine("");
            Console.WriteLine("    ThreadId    : {0}", threadId);
            Console.WriteLine("    errorMsg    : {0}\n", errorMsg);
        }

        public CommMessage clone()
        {
            CommMessage msg = new CommMessage(MessageType.request);
            msg.type = type;
            msg.to = to;
            msg.from = from;
            msg.author = author;
            msg.requestName = requestName;
            msg.command = command;
            foreach (string arg in arguments)
                msg.arguments.Add(arg);
            return msg;
        }

    }

    abstract public class ChannelComm
    {
        public Thread channelCommThread { get; set; } = null;
        public Comm commChannel { get; set; } = null;

        public ChannelComm()
        {
            this.channelCommThread = new Thread(commHandleRequest);
        }

        // when it is called, it waits for a message, if received, it calls the requestHandler to handle the message
        void commHandleRequest()
        {
            while (true)
            {
                try
                {
                    CommMessage commMsg = commChannel.getMessage();
                    requestHandler(commMsg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Inside commHandleRequest");
                    Console.WriteLine(ex);
                }
            }
        }

        // provides implementation of how comm messages will be handled
        abstract public void requestHandler(CommMessage commMsg);

        // initializes Comm Objects and starts the thread to block of getMessage for receiving messages
        protected void initializeComm(string baseAddress, int port)
        {
            commChannel = new Comm(baseAddress, port);
            this.channelCommThread.Start();
        }
    }

    #if (TEST_COMMMESSAGE)
    class TestCommMessage
    {
        static void Main(string[] args)
        {
            CommMessage sampleMsg = new CommMessage(CommMessage.MessageType.request, "ABC", "XYZ", "Amrit", "sampleRequest");

            Console.WriteLine("/n Calling show() for printing the CommMessage: ");
            sampleMsg.show();
        }
    }
    #endif

}