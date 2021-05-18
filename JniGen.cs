using System.IO;
using System.Text;

namespace JniGen
{
    public class JniGen
    {
        string Domain { get; set; }
        string Company { get; set; }
        string Product { get; set; }
        string ClassName { get; set; }

        public JniGen SetProperties(string domain, string company, string product, string classname)
        {
            Domain = domain;
            Company = company;
            Product = product;
            ClassName = classname;
            return this;
        }

        public void Generate(string fullpath)
        {
            string[] text = File.ReadAllLines(fullpath);
            string java = GenerateJavaFile(text);
            File.WriteAllText($"{ClassName}.java", java);
            string jni = GenerateJNIFile(text);
            File.WriteAllText($"JNI_{ClassName}.cpp", jni);
        }

        public string ReadArgName(string line)
        {
            if (line.Contains(','))
            {
                return line.Substring(0, line.IndexOf(','));
            }
            else
            {
                return line.Substring(0, line.IndexOf(')'));
            }
        }

        public string Peek(string line)
        {
            return line.Substring(0, line.IndexOf(' ') + 1);
        }

        public string Rid(string line)
        {
            return line.Remove(0, line.IndexOf(' ') + 1);
        }

        public string GetRidOfTypes(string line, out bool isString, out string theType, out bool isConst)
        {
            string tt = "";
            while (true)
            {
                string peek = Peek(line);
                string rid = Rid(line);

                line = rid;
                tt += peek;
                if (peek != "const " && peek != "unsigned " && peek != "long ")
                {
                    break;
                }
            }

            isString = tt.StartsWith("const char*") || tt.StartsWith("char*");
            isConst = tt.StartsWith("const ");
            theType = tt.TrimEnd();

            return line;
        }

        public string GenerateJNIFile(string[] text)
        {
            StringBuilder tempout = new StringBuilder();

            foreach (var l in text)
            {
                if (l.StartsWith("dllx "))
                {
                    string line = l;
                    StringBuilder jniCall = new StringBuilder();
                    StringBuilder jniline = new StringBuilder("\tJNIEXPORT ");
                    line = line.Remove(0, "dllx ".Length);
                    bool isString;

                    bool atEnd = false;
                    line = GetRidOfTypes(line, out isString, out string theType, out _);

                    if (theType == "double")
                    {
                        jniline.Append("jdouble ");
                        jniCall.Append("\t\tjdouble __return__{ ");
                    }
                    else
                    {
                        jniline.Append("jstring ");
                        jniCall.Append("\t\tjstring __return__{ jniEnv->NewStringUTF(");
                        atEnd = true;
                    }

                    jniline.Append($"JNICALL Java_{Domain}_{Company}_{Product}_{ClassName}_");
                    string functionname = line.Substring(0, line.IndexOf('('));
                    line = line.Remove(0, functionname.Length + 1);
                    string jnifname = functionname.Replace("_", "_1");
                    jniline.Append(jnifname);
                    jniline.Append("\n\t");
                    jniline.Append("(JNIEnv* jniEnv, jobject jniThis");

                    jniCall.Append(functionname + "(");

                    StringBuilder jniPre = new StringBuilder();
                    StringBuilder jniPost = new StringBuilder();

                    if (line[0] == ')')
                    {
                        jniline.Append(") {");
                        jniCall.Insert(0, "\n");
                        if (atEnd) jniCall.Append(")");
                        jniCall.Append(") };");

                        jniline.Append(jniCall);
                        jniline.Append("\n\t\treturn __return__;");
                        jniline.Append("\n\t}\n");
                    }
                    else
                    {
                        bool didAddTempVar = false;
                        jniline.Append(", ");
                        while (line[0] != ')')
                        {
                            
                            line = GetRidOfTypes(line, out isString, out theType, out bool isConst);
                            string argName = ReadArgName(line);
                            string jniArgName = argName;
                            if (theType == "double")
                            {
                                // ¯\_(ツ)_/¯
                                jniline.Append("jdouble ");
                            }
                            else if (isString)
                            {
                                // GetStringUTFChars and ReleaseStringUTFChars handling
                                jniline.Append("jstring ");
                                if (!didAddTempVar)
                                {
                                    jniPre.Append("\t\tjboolean _isCopy{ JNI_FALSE };\n");
                                    didAddTempVar = true;
                                }

                                jniPre.Append("\t\tconst char* _cstr_" + argName + "{ jniEnv->GetStringUTFChars(" + argName + ", &_isCopy) };\n");
                                jniPost.Append("\t\tjniEnv->ReleaseStringUTFChars(" + argName + ", _cstr_" + argName + ");\n");
                                jniArgName = "_cstr_" + argName;
                            }
                            else
                            {
                                // GetDirectBufferAddress handling
                                jniline.Append("jobject ");
                                string constwrapper = "{0}";
                                if (isConst)
                                    constwrapper = "const_cast<const void*>({0})";
                                string castwrapper = "reinterpret_cast<" + theType + ">({0})";

                                string addrline = string.Format(castwrapper, string.Format(constwrapper, "jniEnv->GetDirectBufferAddress(" + argName + ")"));

                                jniPre.Append("\t\t" + theType + " _raw_" + argName + $"{{ {addrline} }};\n");
                                jniArgName = "_raw_" + argName;
                            }

                            jniline.Append(argName);
                            line = line.Remove(0, argName.Length);
                            if (line.StartsWith(", ")) line = line.Remove(0, ", ".Length);
                            else if (line.StartsWith(',')) line = line.Remove(0, 1);

                            bool shouldAppendSemicolon = !line.StartsWith(')');

                            jniCall.Append(jniArgName);
                            if (shouldAppendSemicolon)
                            {
                                jniCall.Append(", ");
                                jniline.Append(", ");
                            }

                        }

                        if (atEnd) jniCall.Append(")");
                        jniCall.Append(") };\n");
                        jniline.Append(") {\n");
                        jniPost.Append("\t\treturn __return__;");

                        StringBuilder jniFinal = new StringBuilder();
                        jniFinal.Append(jniPre);
                        jniFinal.Append(jniCall);
                        jniFinal.Append(jniPost);
                        jniline.Append(jniFinal);
                        jniline.Append("\n\t}\n");
                    }

                    tempout.Append(jniline);
                    tempout.Append('\n');
                }
            }

            return $"#ifdef __ANDROID__\n/* add your includes here... */\n\nextern \"C\" {{\n#include <jni.h>\n{tempout}}}\n#endif /* __ANDROID__ */\n";
        }

        public string GenerateJavaFile(string[] text)
        {
            StringBuilder tempout = new StringBuilder();

            foreach (var l in text)
            {
                if (l.StartsWith("dllx "))
                {
                    string line = l;
                    StringBuilder javaline = new StringBuilder("\tpublic native ");
                    line = line.Remove(0, "dllx ".Length);
                    bool isString;

                    line = GetRidOfTypes(line, out isString, out string theType, out _);

                    if (theType == "double")
                    {
                        javaline.Append("double ");
                    }
                    else
                    {
                        javaline.Append("String ");
                    }

                    string functionname = line.Substring(0, line.IndexOf('('));
                    javaline.Append(functionname);
                    javaline.Append('(');
                    line = line.Remove(0, functionname.Length + 1);

                    // no arguments
                    if (line[0] == ')')
                    {
                        javaline.Append(");");
                    }
                    else
                    {
                        while (line[0] != ')')
                        {
                            line = GetRidOfTypes(line, out isString, out theType, out _);
                            if (theType == "double")
                            {
                                javaline.Append("double ");
                            }
                            else if (isString)
                            {
                                javaline.Append("String ");
                            }
                            else
                            {
                                javaline.Append("ByteBuffer ");
                            }

                            string argName = ReadArgName(line);

                            javaline.Append(argName);
                            line = line.Remove(0, argName.Length);
                            if (line.StartsWith(", ")) line = line.Remove(0, ", ".Length);
                            else if (line.StartsWith(',')) line = line.Remove(0, 1);


                            bool shouldAppendSemicolon = !line.StartsWith(')');
                            if (shouldAppendSemicolon)
                            {
                                javaline.Append(", ");
                            }

                        }

                        javaline.Append(");");
                    }

                    tempout.Append(javaline);
                    tempout.Append('\n'); // always use unix newlines.
                }
            }

            return $"package {Domain}.{Company}.{Product};\n\n// add your imports here...\n\npublic class {ClassName} {{\n\tpublic {ClassName}() {{\n\t\tSystem.loadLibrary(\"your-library-here\");\n\t}}\n\n{tempout}}} // {ClassName}\n";
        }
    }
}
