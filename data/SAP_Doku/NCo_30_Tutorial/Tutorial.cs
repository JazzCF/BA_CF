using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SAP.Middleware.Connector.Examples
{
    public class Tutorial
    {
        private const string EXAMPLE_METHOD_NAME_PREFIX = "Example";

        public static void Main(string[] args)
        {
            Type[] exampleClasses = new Type[] { typeof(StepByStepClient), typeof(StepByStepServer), typeof(SampleDestinationConfiguration) };

            bool keepGoing = true;

            do
            {
                Console.WriteLine("\n===== TUTORIAL EXAMPLES =====");
                for (int i = 0; i < exampleClasses.Length; i++)
                {
                    Console.Write((i + 1).ToString());
                    Console.Write(' ');
                    Console.WriteLine(OutputName(exampleClasses[i].Name));
                }
                Console.WriteLine("E Exit");
                Console.WriteLine("T Change Trace Directory (currently is {0})", RfcTrace.TraceDirectory);
                Console.Write("\nYour Choice: ");
                string input = Console.ReadLine();
                bool inputRecognized = true;
                int n;

                if (input.Equals("E", StringComparison.OrdinalIgnoreCase))
                {
                    keepGoing = false;
                }
                else if (input.Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Write("New trace directory: ");
                    string traceDir = Console.ReadLine();
                    try
                    {
                        RfcTrace.TraceDirectory = traceDir;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                else if (int.TryParse(input, out n) && n >= 1 && n <= exampleClasses.Length)
                {
                    keepGoing = SubMenu(exampleClasses[n - 1]);
                }
                else
                {
                    inputRecognized = false;
                }

                if (!inputRecognized)
                {
                    Console.WriteLine("\nEnter 1 - {0} for an example, 'T' or 't' to change the current trace directory, 'E' or 'e' to exit", exampleClasses.Length.ToString());
                }
            }
            while (keepGoing);

            Console.WriteLine("\nBYE");
        }

        private static bool SubMenu(Type exampleClass)
        {
            MethodInfo[] methods = exampleClass.GetMethods();
            List<MethodInfo> examples = new List<MethodInfo>();
            MethodInfo setUp = null, tearDown = null;
            foreach (MethodInfo method in methods)
            {
                // assume all parameterless, void, static methods starting with "Example" to be relevant (by convention)
                if (method.IsStatic && method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                {
                    if (method.Name.StartsWith(EXAMPLE_METHOD_NAME_PREFIX))
                    {
                        examples.Add(method);
                    }
                    else if (method.Name.Equals("SetUp"))
                    {
                        setUp = method;
                    }
                    else if (method.Name.Equals("TearDown"))
                    {
                        tearDown = method;
                    }
                }
            }

            if (!Invoke(setUp))
            {
                return true;
            }

            string input;
            bool keepGoing = true;
            do
            {
                int i = 1;
                Console.WriteLine();
                Console.Write("~~~ ");
                Console.Write(OutputName(exampleClass.Name));
                Console.WriteLine(" ~~~");
                foreach (MethodInfo method in examples)
                {
                    Console.Write(i.ToString());
                    Console.Write(' ');
                    Console.WriteLine(OutputName(method.Name));
                    i++;
                }
                Console.WriteLine("R Return to Main Menu");
                Console.WriteLine("E Exit");
                Console.Write("\nYour Choice: ");
                input = Console.ReadLine();
                int n;
                bool inputValid = false;
                if (int.TryParse(input, out n))
                {
                    if (n >= 1 && n <= examples.Count)
                    {
                        inputValid = true;
                        Invoke(examples[n - 1]);
                    }
                }
                else if ("R".Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    inputValid = true;
                }
                else if ("E".Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    inputValid = true;
                    keepGoing = false;
                }
                if (!inputValid)
                {
                    Console.WriteLine("\n--- Enter a number 1 - {0} or A or R ---", examples.Count.ToString());
                }
            }
            while (!"R".Equals(input, StringComparison.OrdinalIgnoreCase) && keepGoing);

            Invoke(tearDown);

            return keepGoing;
        }

        private static string OutputName(string internalName)
        {
            StringBuilder sb = new StringBuilder();
            char previous = (char)0;
            for (int i = internalName.StartsWith(EXAMPLE_METHOD_NAME_PREFIX) ? EXAMPLE_METHOD_NAME_PREFIX.Length : 0; i < internalName.Length; i++)
            {
                char c = internalName[i];
                if (c == '_')
                {
                    sb.Append(' ');
                }
                else if (Char.IsUpper(c))
                {
                    if (sb.Length > 0 && !Char.IsUpper(previous))
                    {
                        sb.Append(' ');
                    }
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
                previous = c;
            }
            return sb.ToString();
        }

        private static bool Invoke(MethodInfo method)
        {
            if (method == null)
            {
                return true;
            }
            try
            {
                method.Invoke(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>>> ERROR: {0} threw {1} <<<<", method.Name, ex.GetType().Name);
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}