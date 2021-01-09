/*
 * C# Injector Tool Developed by TL074051
   --------------------------------------
 ->Makes a call to a rest service,which in return sends the dll to be injected.
 ->The Dll are injected with refrence to the send method in sender dll.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using CshapInstrumenter;
using CshapInstrumenter.Formatter;

namespace inject

{
    class Program
    {
        public class class_name
        {
            public string namnamespaceName { get; set; }
            public string className { get; set; }

        }
        public class Files
        {
            public string fileName { get; set; }
            public List<class_name> classes { get; set; }
        }

        public class Project
        {
            public string projectName { get; set; }
            public string endStateName { get; set; }
            public List<Files> files { get; set; }
        }

        public class RootObject
        {
            public string solutionName { get; set; }
            public string domain { get; set; }
            public List<Project> projects { get; set; }
        }

        public class TimeoutWebClient : WebClient
        {
            public int Timeout { get; set; }

            public TimeoutWebClient()
            {
                Timeout = 600000;
            }

            public TimeoutWebClient(int timeout)
            {
                Timeout = timeout;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                request.Timeout = Timeout;
                return request;
            }
        }
        //Send solutionName ,domain name, eod creation date
        static void Main(String[] args)
        {
            String solutionName = args[0];
            String eodCreationDate = args[1];
            String domainName = args[2];
           

            Console.WriteLine("Solution Name:"+solutionName+" Domain Name:"+domainName+" EOD creation Date is:"+eodCreationDate);

            const string source_directory = @"C:\\Program Files\\Cerner\\";
            const string backup = @"C:\\Program Files\\Cerner\\backup\\";
            const string senderfile = @"C:\\Program Files\\Cerner\\ATSsender.dll";
            const string log_file = @"C:\\ATS\\SBIlog.txt";
            
            string json;
            string sourcefile;
            string source;
            var sender_method = (dynamic)null;

            Int32 count = 10;
            char[] pspearator = { ' ', '/', '.' };
            char[] spearator = { ' ', ':', '(', ')' };

            Formatter format = new Formatter();

            //call the service and get the json with data of files to be injected
            using (TimeoutWebClient wc = new TimeoutWebClient())
            {
                //json = wc.DownloadString("https://ats.cerner.com:8443/solutioninfo/getSolutionData?solutionName=PathNet -- Anatomic Pathology&domain=SOLM64&eodCreationDate=02-06-2020 00:00:00.000");
                json = wc.DownloadString("https://ats.cerner.com:8443/solutioninfo/getSolutionData?solutionName="+solutionName+"&domain="+domainName+"&eodCreationDate="+eodCreationDate+ "&isMerged=true");
                /*
                if ("PathNet -- Anatomic Pathology".Equals(solutionName))
                {
                    json = File.ReadAllText("C:\\ATS\\Injector\\anatomic_pathology.json");
                }
                else if("Person Management".Equals(solutionName)) {
                    json = File.ReadAllText("C:\\ATS\\Injector\\person_management.json");
                }
                else {
                    json = File.ReadAllText("C:\\ATS\\Injector\\anatomic_pathology.json");
                }
                */
            }
            RootObject r = JsonConvert.DeserializeObject<RootObject>(json);

            //load ATSsenderfile
            AssemblyDefinition Sender_assembly = AssemblyDefinition.ReadAssembly(senderfile, new ReaderParameters { ReadWrite = true });
            var Sender_module = Sender_assembly.MainModule;
            TypeDefinition[] sender_types = Sender_module.Types.ToArray();

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(source_directory);

            //load refrence to the send fucntion of sender dll
            foreach (var type in sender_types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.Name == "send")
                    {
                        sender_method = method;
                    }
                }
            }


            foreach (Project projects in r.projects)
            {
                Console.WriteLine("Instrumentation Started......");
                sourcefile = source_directory + projects.endStateName;
                source = projects.endStateName;


                if (File.Exists(sourcefile))
                {
                    //create backup of the source DLL
                    File.Copy(sourcefile, backup + Path.GetFileName(source)); // handle exception dude to this folder and files inside // after coping the dll is renamed with diffrent casing

                    //load DLL reference
                    AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(sourcefile, new ReaderParameters { ReadWrite = true, AssemblyResolver = resolver });
                    var module = assembly.MainModule;
                    TypeDefinition[] types = module.GetTypes().ToArray();

                    foreach (Files dll in projects.files)
                    {
                        foreach (class_name clas in dll.classes)
                        {
                            //gives class name
                            foreach (var type in types)
                            {
                                string[] splitClassName = type.ToString().Split('.');
                                string clsName = splitClassName[splitClassName.Length - 1];
                                string clsNameFromFile = "";

                                //This change is to support instrumentation both before or after separation of classname and namespace
                                if (clas.className != null)
                                {
                                    string[] splitClassNameFromFile = clas.className.Split('.');
                                    clsNameFromFile = splitClassNameFromFile[splitClassNameFromFile.Length - 1];
                                }


                                //check if if we require this class to be injected
                                if (clsName == clsNameFromFile)
                                {
                                    
                                    Console.WriteLine(type.ToString());

                                    foreach (var method in type.Methods)
                                    {

                                        try
                                        {
                                            string methodname = method.FullName.ToString();

                                            Console.WriteLine(methodname);
                                            //Ignore setters and getters
                                            if (!method.IsGetter && !method.IsSetter)
                                            {
                                                string variable = "";
                                                //find all arguments
                                                foreach (var Param in method.Parameters)
                                                {

                                                    string parameter_type = Param.ParameterType.ToString();
                                                    //In case of generic this garbage value gets appended
                                                    parameter_type = parameter_type.Replace("`1", "");

                                                    string variable_type = "";
                                                    //This implementation is specfic to Generic in variable type e.g Memlib<MainModule.usrdefMainOrder> nSourceTable
                                                    if (parameter_type.Contains("<") && parameter_type.Contains(">"))
                                                    {
                                                        String[] list = parameter_type.Split('<');
                                                        string pre = list[0];
                                                        string post = list[1];
                                                        String[] preList = pre.Split(pspearator, count);
                                                        pre = preList[preList.Count() - 1];
                                                        String[] postList = post.Split(pspearator, count);
                                                        post = postList[postList.Count() - 1];
                                                        variable_type = pre + " <" + post;

                                                    }
                                                    else
                                                    {
                                                        String[] parameter_list = parameter_type.Split(pspearator, count);
                                                        variable_type = parameter_list[parameter_list.Count() - 1];
                                                    }
                                                    variable_type = variable_parser(variable_type);
                                                    var param_name = Param.Name;
                                                    if (Param.HasConstant)
                                                    {
                                                        param_name = param_name + " = " + Param.Constant;
                                                    }
                                                    variable_type = variable_type + " " + param_name + ", ";
                                                    variable = variable + variable_type;
                                                }

                                                // Removing comma and space
                                                if (variable.Length > 2)
                                                {
                                                    variable = variable.Remove(variable.Length - 1);
                                                    variable = variable.Remove(variable.Length - 1);
                                                }
                                                String[] strlist = methodname.Split(spearator, count);

                                                string final;
                                                string signature = format.Format("(" + variable + ")");
                                                //string signature = "(" + variable + ")";
                                                string[] nameSpaceAndClassName = type.ToString().Split('.');
                                                string clazz = nameSpaceAndClassName[nameSpaceAndClassName.Length - 1];
                                                string nameSpace_ = type.ToString().Replace("." + clazz, "");
                                                final = strlist[3] + ';' + type.ToString() + ';' + dll.fileName + ';' + source + ";" + signature;
                                                string methodName = strlist[3];

                                                // for constructors
                                                final = methodName + ';' + clazz + ';' + nameSpace_ + ';' + dll.fileName + ';' + source + ";" + signature;


                                                //injection log
                                                File.AppendAllText(log_file, final + Environment.NewLine);

                                                //load IL processor                        
                                                var processor2 = method.Body.GetILProcessor();
                                                var call = processor2.Create(OpCodes.Call, method.Module.Import(sender_method));
                                                var ldstr = processor2.Create(OpCodes.Ldstr, final);

                                                //Inject the code
                                                processor2.InsertBefore(method.Body.Instructions[0], ldstr);
                                                processor2.InsertAfter(method.Body.Instructions[0], call);

                                                //Console.WriteLine("Successfully Injected:" + source);
                                                Console.WriteLine("-----------------------------------------------------------------------------------------------");
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            File.AppendAllText(log_file, e + Environment.NewLine);
                                        }
                                    } //before the try

                                }
                            }
                        }
                    }
                    //write back the new Injected source file
                    assembly.MainModule.AssemblyReferences.Add(AssemblyNameReference.Parse(Sender_assembly.FullName));
                    assembly.Write();
                    Console.WriteLine("--- Finished Injecting DLL ---");
                }
                else
                    Console.WriteLine("");
                //Console.WriteLine(sourcefile + " doesnt exsit in cerner directory");
            }
            Console.ReadLine();
        }



        static string variable_parser(string variable_type)
        {
            variable_type = variable_type.ToLower();
            //Rules for variable names
            switch (variable_type)
            {
                case "int32":
                    variable_type = "int";
                    break;
                case "int32&":
                    variable_type = " ref int";
                    break;
                //nullable type
                case "int32>":
                    variable_type = "int?";
                    break;
                case "single":
                    variable_type = "float";
                    break;
                case "single>":
                    variable_type = "float?";
                    break;
                case "boolean":
                    variable_type = "bool";
                    break;
                case "boolean>":
                    variable_type = "bool?";
                    break;
                case "boolean&":
                    variable_type = "ref bool";
                    break;
                case "uint32":
                    variable_type = "uint";
                    break;
                case "uint32&":
                    variable_type = "ref uint";
                    break;
                case "uint32>":
                    variable_type = "uint?";
                    break;
                case "int16":
                    variable_type = "short";
                    break;
                case "int16>":
                    variable_type = "short?";
                    break;
                case "int16&":
                    variable_type = "ref short";
                    break;
                case "int64":
                    variable_type = "long";
                    break;
                case "int64>":
                    variable_type = "long?";
                    break;
                case "int64&":
                    variable_type = "ref long";
                    break;
                case "uint64":
                    variable_type = "ulong";
                    break;
                case "uint64>":
                    variable_type = "ulong?";
                    break;
                case "uint64&":
                    variable_type = "ref ulong";
                    break;
                case "double>":
                    variable_type = "double?";
                    break;
                default:
                    if (variable_type.Contains("&"))
                    {
                        variable_type = variable_type.Replace("&", "");
                        variable_type = "ref " + variable_type;
                    }
                    break;
            }
            return variable_type;
        }
    }
}

