using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System.IO;

namespace UnCodefuscator
{
    class Program
    {
        private static ModuleDefMD asm;
        private static int stringfixcounter = 0, removedTypecounter = 0, antiDe4dotCounter = 0;
        private static bool showAll = false;

        static void Main(string[] args)
        {
            Console.Title = "UnCodefuscator";
            Console.WriteLine("UnCodefuscator - Deobfuscator for Codefuscator | by misonothx");
            Console.WriteLine(" |- https://github.com/miso-xyz/UnCodefuscator/");
            Console.WriteLine();
            asm = ModuleDefMD.Load(args[0]);
            showAll = args.Contains("-showAll");
            removeJunkTypes();
            if (showAll) { Console.WriteLine(); }
            retrieveSystemTypes();
            if (showAll) { Console.WriteLine(); }
            fixStrings();
            ModuleWriterOptions moduleWriterOptions = new ModuleWriterOptions(asm);
            moduleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            moduleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            NativeModuleWriterOptions nativeModuleWriterOptions = new NativeModuleWriterOptions(asm, true);
            nativeModuleWriterOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
            nativeModuleWriterOptions.Logger = DummyLogger.NoThrowInstance;
            if (showAll) { Console.WriteLine(); }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("###################################################");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("    " + stringfixcounter + " Strings Fixed (Base64 Encoded)");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("    " + removedTypecounter + " Removed Types (" + antiDe4dotCounter + " AntiDe4dots Removed)");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("###################################################");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Now saving...");
            try
            {
                asm.Write(Path.GetFileNameWithoutExtension(args[0]) + "-UnCodefuscated" + Path.GetExtension(args[0]));
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to save! (" + ex.Message + ")");
                goto end_;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Successfully saved!");
        end_:
            Console.ResetColor();
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }

        static void PrintRemoved(string name, string type)
        {
            if (!showAll) { return; }
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("[Removed]: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("'" + name + "'");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(" (" + type + ")");
            Console.ResetColor();
        }

        static void PrintFixed(string from, string to, string type)
        {
            if (!showAll) { return; }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("[Fixed]: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("'" + from + "'");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("'" + to + "'");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" (" + type + ")");
            Console.ResetColor();
        }

        static void retrieveSystemTypes()
        {
            foreach (TypeDef t_ in asm.Types)
            {
                if (t_.BaseType != null)
                {
                    if (t_.BaseType.Name.Contains("ApplicationBase")) { PrintFixed(t_.Name, "MyApplication", "class name"); t_.Name = "MyApplication"; continue; }
                    if (t_.BaseType.Name.Contains("ApplicationSettingsBase")) { PrintFixed(t_.Name, "MySettings", "class name"); t_.Name = "MySettings"; continue; }
                    if (t_.BaseType.Name == "Computer") { PrintFixed(t_.Name, "MyComputer", "class name"); t_.Name = "MyComputer"; continue; }
                }
                if (t_.HasCustomAttributes)
                {
                    foreach (CustomAttribute CA in t_.CustomAttributes)
                    {
                        if (CA.HasConstructorArguments)
                        {
                            foreach (CAArgument CAArgs in CA.ConstructorArguments)
                            {
                                if (CAArgs.Value.ToString().Contains("System.Resources")) { PrintFixed(t_.Name, "Resources", "class name"); t_.Name = "Resources"; continue; }
                            }
                        }
                    }
                }
                if (t_.HasProperties)
                {
                    foreach (PropertyDef prop in t_.Properties)
                    {
                        if (prop.HasCustomAttributes)
                        {
                            foreach (CustomAttribute CA in prop.CustomAttributes)
                            {
                                if (CA.HasConstructorArguments)
                                {
                                    foreach (CAArgument CAArgs in CA.ConstructorArguments)
                                    {
                                        if (CAArgs.Value.ToString().Contains("My.Settings")) { PrintFixed(t_.Name, "MySettingsProperty", "class name"); t_.Name = "MySettingsProperty"; continue; }
                                    }
                                }
                            }
                        }
                    }
                }
                if (t_.HasNestedTypes)
                {
                    foreach (TypeDef t__ in t_.NestedTypes)
                    {
                        if (t__.Name == "RemoveNamespaceAttributesClosure") { PrintFixed(t_.Name, "InternalXmlHelper", "class name"); t_.Name = "InternalXmlHelper"; continue; }
                        if (t__.Name == "MyWebServices") { PrintFixed(t_.Name, "MyProject", "class name"); t_.Name = "MyProject"; continue; }
                    }
                }
                if (!t_.HasMethods) { continue; }
                foreach (MethodDef methods in t_.Methods)
                {
                    if (!methods.HasBody) { continue; }
                    if (methods.Name == "StringDecryptor") { t_.Name = "CodefuscatorUtilities"; break; }
                }
            }
        }

        static void removeJunkTypes()
        {
            List<string> attribs = new List<string>() { "YanoAttribute", "Xenocode.Client.Attributes.AssemblyAttributes.ProcessedByXenocode", "PoweredByAttribute", "ObfuscatedByGoliath", "NineRays.Obfuscator.Evaluation", "NetGuard", "dotNetProtector", "DotNetPatcherPackerAttribute", "DotNetPatcherObfuscatorAttribute", "DotfuscatorAttribute", "CryptoObfuscator.ProtectedWithCryptoObfuscatorAttribute", "BabelObfuscatorAttribute", "BabelAttribute", "AssemblyInfoAttribute" };
            for (int x = 0; x < asm.Types.Count; x++)
            {
                if (attribs.Contains(asm.Types[x].Name) && attribs.Contains(asm.Types[x].Namespace)) { antiDe4dotCounter++; removedTypecounter++; PrintRemoved(asm.Types[x].Name, "Class (Known AntiDe4dot)"); asm.Types.RemoveAt(x); x--; }
                if (asm.Types[x].Name.Contains("ERROR!!!")) { removedTypecounter++; PrintRemoved(asm.Types[x].Name, "Class"); asm.Types.RemoveAt(x); x--; }
                if (asm.Types[x].HasInterfaces)
                {
                    foreach (InterfaceImpl interface_ in asm.Types[x].Interfaces)
                    {
                        if (interface_.Interface.Name.Contains(asm.Types[x].Name))
                        {
                            removedTypecounter++;
                            antiDe4dotCounter++;
                            PrintRemoved(interface_.Interface.Name, "Interface Type");
                            asm.Types.RemoveAt(x);
                            x--;
                        }
                    }
                }
                if (asm.Types[x].HasMethods)
                {
                    for (int x_ = 0; x_ < asm.Types[x].Methods.Count; x_++)
                    {
                        MethodDef methods = asm.Types[x].Methods[x_];
                        if (methods.HasBody)
                        {
                            if (methods.Body.Instructions[methods.Body.Instructions.Count - 2].OpCode.Equals(OpCodes.Ldc_I4) && methods.Body.Instructions.Count >= 3)
                            {
                                PrintRemoved(asm.Types[x].Name + "." + methods.Name, "method");
                                asm.Types[x].Methods.RemoveAt(x_);
                                x_--;
                                removedTypecounter++;
                            }
                        }
                    }
                }
                if (!asm.Types[x].HasMethods) { removedTypecounter++; PrintRemoved(asm.Types[x].Name, "Empty Class"); asm.Types.RemoveAt(x); x--; }
            }
        }

        static void fixStrings()
        {
            foreach (TypeDef t_ in asm.Types)
            {
                if (!t_.HasMethods) { continue; }
                foreach (MethodDef methods in t_.Methods)
                {
                    methods.Body.KeepOldMaxStack = true;
                    if (!methods.HasBody) { continue; }
                    for (int x = 0; x < methods.Body.Instructions.Count; x++)
                    {
                        Instruction inst = methods.Body.Instructions[x];
                        if (inst.OpCode.Equals(OpCodes.Ldstr) && methods.Body.Instructions[x + 1].OpCode.Equals(OpCodes.Call))
                        {
                            string str = inst.Operand.ToString();
                            checkInst:
                            if (methods.Body.Instructions[x + 1].Operand != null && methods.Body.Instructions[x+1].OpCode.Equals(OpCodes.Call))
                            {
                                if (methods.Body.Instructions[x + 1].Operand.ToString().Contains("StringDecryptor"))
                                {
                                    if (methods.Body.Instructions[x + 1].Operand.ToString().Contains("StringDecryptor"))
                                    {
                                        str = Encoding.UTF8.GetString(Convert.FromBase64String(str));
                                        methods.Body.Instructions.RemoveAt(x + 1);
                                        goto checkInst;
                                    }
                                }
                            }
                            PrintFixed(inst.Operand.ToString(), str, "string");
                            inst.Operand = str;
                            stringfixcounter++;
                        }
                    }
                }
            }
        }
    }
}
