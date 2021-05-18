using System;

namespace JniGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("JniGen - generate .java and JNI_*.cpp bindings with ease!");
                Console.WriteLine("Usage:");
                Console.WriteLine("JniGen -domain [com] -company [whatever] -product [yourlibrary] -classname [AutogenClassNative] -file <path to .h>");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue . . . ");
                Console.ReadKey(true);
            }
            else
            {
                string domain = "com";
                string company = "whatever";
                string product = "yourlibrary";
                string classname = "AutogenClassNative";
                string file = null;

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-domain":
                            {
                                domain = args[++i];
                                break;
                            }

                        case "-company":
                            {
                                company = args[++i];
                                break;
                            }

                        case "-product":
                            {
                                product = args[++i];
                                break;
                            }

                        case "-classname":
                            {
                                classname = args[++i];
                                break;
                            }

                        case "-file":
                            {
                                file = args[++i];
                                break;
                            }

                        default:
                            {
                                throw new Exception($"Invalid argument '{args[i]}'");
                            }
                    }
                }

                if (file is null)
                {
                    throw new Exception("Filename is not set.");
                }

                Console.WriteLine($"Running JniGen on file {file}");
                new JniGen().SetProperties(domain, company, product, classname).Generate(file);
                Console.WriteLine($"{classname}.java and JNI_{classname}.cpp generated, have fun.");
            }
        }
    }
}
