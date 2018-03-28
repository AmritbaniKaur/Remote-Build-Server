# Remote-Build-Server

Developed a robust Federation, consisting of a Mock GUI Client, a Mock Repository, a Remote Build Server and a Test Harness. Each component is a REST Service which communicates with each other by exchanging messages through proper channels. The Client selects and keeps files in the repository and asks the Build Server to compile and the Test Harness to execute. Appropriate Log files and results are sent to the Client. Built using C# .Net, WCF and WPF; Individual Project

----------------------------------------------------------------------------------

The Project Demonstrates:

Req 1 : prepared using C#, the .Net Frameowrk, and Visual Studio 2017

Req 2 : includes a Message-Passing Communication Service built with WCF. Built on Comm Prototype 3.

Req 3 : supports accessing build requests by Pool Processes from the mother Builder process, 
	sending and receiving build requests, and sending and receiving files.

Req 4 : provides a Repository server that supports client browsing to find files to build, 
	builds an XML build request string and sends that and the cited files to the Build Server.
	
Req 5 : provides a Process Pool component that creates a specified number of processes on command.

Req 6 : uses message-passing communication to access messages from the mother Builder process.

Req 7 : attempts to build each library, cited in a retrieved build request, logging warnings and errors.

Req 8 : If the build succeeds, sends a test request and libraries to the Test Harness for execution, 
	and sends the build log to the repository.

Req 9 : The Test Harness attempts to load each test library it receives and execute it. 
	It submits the results of testing to the Repository.

Req 10: includes a Graphical User Interface, built using WPF.

Req 11:	The GUI client is a separate process, implemented with WPF and using message-passing communication. 
	It provides mechanisms to get file lists from the Repository, and select files for packaging into a test library1, 
	e.g., a test element specifying driver and tested files, added to a build request structure. 
	It provides the capability of repeating that process to add other test libraries to the build request structure.

Req 12:	The client sends build request structures to the repository for storage and transmission to the Build Server.

Req 13: The client is able to request the repository to send a build request in its storage to the Build Server for build processing.

----------------------------------------------------------------------------------

Folder Details:
--------------
\RemoteBuildServer\Storage\

	- BuilderStorage : 
		- Separate folders will be created according to Child Builder Endpoints
		- Creates Build Request specific folders too
		- Logs\ : saves the log files generated during the compilation process

	- TestHarnessStorage : 
		- stores the dll library files received from the Build Server for execution
		- Logs\ : saves the log files generated during the execution process

	- MockClientStorage : 
		- \CodeFiles\ : 
			Contains Sample Directory Structure present at Client side to create Build Requests from
			A copy of this folder is in the main RemoteBuildServer folder

		- \BuildRequests\ : 
			created by the GUI will be saved here
		
		- \Logs\ : 
			saves the Logs received from the Build Server and the Test Harness

	- RepositoryStorage :
		- \BuildRequests\ : 
			Build Requests sent by the client will be stored here
		- \RepositoryStorage\ : 
			< .cs files> : Currently these are not uploaded by the Client. A copy of this folder is in the main RemoteBuildServer folder
		- \Logs\ : 
			saves the Logs received from the Build Server and the Test Harness

----------------------------------------------------------------------------------
