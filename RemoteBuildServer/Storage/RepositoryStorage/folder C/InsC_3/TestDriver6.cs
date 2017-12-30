using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public class TestDriver1 : ITest
    {
        bool testAdd()
        {
            bool status=false;
            try
            {
                Demo1App1 addObj = new Demo1App1();
                int a=4, b=5, result=0;
                result = addObj.addInputs(a,b);
                Console.WriteLine(" addinputs() in Demo1App1 returns: {0}\t\tAddition of: {1} and {2}", result, a, b);
                status = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n An error occured while trying to build Demo1App1 \n Details: {0}\n\n", ex.Message);
                Console.WriteLine(" ==========================================================================================\n");
                status = false;
            }
            return status;
        }

        bool testSubtract()
        {
            bool status = false;
            try
            {
                Demo2App1 subObj = new Demo2App1();
                int a = 10, b = 5, result = 0;
                result = subObj.subtractInputs(a, b);
                Console.WriteLine(" subtractInputs() in Demo2App1 returns: {0}\tSubtraction of: {1} from {2}", result, b, a);
                status = true;
            }
            catch (Exception ex)
            {
                Console.Write("\n An error occured while trying to build Demo2App1 \n Details: {0}\n\n", ex.Message);
                Console.WriteLine(" ==========================================================================================\n");
                status = false;
            }
            return status;
        }

        public bool test()
        {
            Console.WriteLine("\n ==========================================================================");
            Console.WriteLine(" DemoApp1 for TestDriver1 is being executed!\n");

            bool result1 = testAdd();
            bool result2 = testSubtract();

            Console.WriteLine(" Demonstrating: the Test1 libraries executed successfully");
            Console.WriteLine(" Built and Loaded through .cs files for 'All Pass' case");
            Console.WriteLine(" ==========================================================================\n");

            return result1 && result2;
        }
    }
}
