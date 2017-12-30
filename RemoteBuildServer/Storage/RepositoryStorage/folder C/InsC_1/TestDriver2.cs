using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoApp
{
    public class TestDriver2 : ITest
    {
        bool testAdd()
        {
            bool status=false;
            try
            {
                Demo1App2 addObj = new Demo1App2();
                int a=4, b=5, result=0;
                result = addObj.addInputs(a,b);
                Console.WriteLine(" addinputs() in Demo1App2 returns: {0}\tAddition of: {1} and {2}", result, a, b);
                status = true;
            }
            catch (Exception ex)
            {
                Console.Write("\n An error occured while trying to build Demo1App2 \n Details: {0}\n\n", ex.Message);
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
                Demo2App2 subObj = new Demo2App2();
                int a = 10, b = 5, result = 0;
                uint num1 = 14;
                a= num1;
                result = subObj.subtractInputs(a, b);
                Console.WriteLine(" subtractInputs() in Demo2App2 returns: {0}\tSubtraction of: {1} from {2}", result, b, a);
                status = true;
            }
            catch (Exception ex)
            {
                Console.Write("\n An error occured while trying to build Demo2App2 \n Details: {0}\n\n", ex.Message);
                Console.WriteLine(" ==========================================================================================\n");
                status = false;
            }
            return status;
        }

        public bool test()
        {
            Console.WriteLine("\n ==========================================================================");
            Console.WriteLine(" DemoApp2 for TestDriver2 is being executed! \n");

            bool result1 = testAdd();
            bool result2 = testSubtract();

            Console.WriteLine(" Demonstrating: the Test2 libraries will have a compilation error");
            Console.WriteLine("\t Tried to build through .cs files for 'Build Fail' case, so no dlls will be created");
            Console.WriteLine(" ==========================================================================");

            return result1 && result2;
        }
    }
}
