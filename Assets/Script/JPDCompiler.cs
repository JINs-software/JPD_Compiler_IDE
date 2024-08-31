using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Experimental.GlobalIllumination;

public class JPDCompiler : MonoBehaviour
{
    static JPDCompiler s_Instance;
    public static JPDCompiler Instance { get { Init(); return s_Instance; } }

    public static JPD_SCHEMA JPDSchema { get { return Instance.m_JPD; } }

    public static byte ValidCode
    {
        get { return Instance.validCode; }
        set { Instance.validCode = value; }
    }
    public static bool EnDecodeFlag
    {
        get { return Instance.endecodeFlag; }
        set { Instance.endecodeFlag = value; }
    }
    public static bool SimpleHdrMode
    {
        get { return Instance.simpleHdrMode; }
        set { Instance.simpleHdrMode = value; }
    }

    private void Start()
    {
        Init();
    }
    private static void Init()
    {
        GameObject go = GameObject.Find("@JPDCompiler");
        if (go == null)
        {
            go = new GameObject { name = "@JPDCompiler" };
            go.AddComponent<JPDCompiler>();

            DontDestroyOnLoad(go);
        }
        s_Instance = go.GetComponent<JPDCompiler>();
    }

    JPD_SCHEMA m_JPD = new JPD_SCHEMA();

    byte    validCode;
    bool    endecodeFlag;
    bool    simpleHdrMode;
    
    public JPD_NAMESPACE AddJpdNamespace(string name, string id)
    {
        foreach (var item in m_JPD.JPD_Namespaces)
        {
            if (item.Namespace == name)
            {
                return null;
            }
        }

        int namespaceID = m_JPD.JPD_Namespaces.Count;
        JPD_NAMESPACE newNamespace = new JPD_NAMESPACE(name, namespaceID.ToString());
        newNamespace.Namespace = name;
        newNamespace.ID = namespaceID.ToString();
        m_JPD.JPD_Namespaces.Add(newNamespace);

        return newNamespace;
    }

    public void SaveJsonFile(string path)
    {
        // ID 부여
        AllocJpdMessageID();

        string json = JsonUtility.ToJson(m_JPD, true);
        File.WriteAllText(path, json);
    }

    public void LoadJson(string path)
    {
        //TextAsset textAsset = Resources.Load<TextAsset>(path);
        // => Asset/Resource 산하가 아닌 외부 경로의 json 파일 읽기
        string json = File.ReadAllText(path);
        JPD_SCHEMA jpd = JsonUtility.FromJson<JPD_SCHEMA>(json);

        m_JPD = jpd;
    }

    public bool Complie()
    {
        AllocJpdMessageID();

        if (!string.IsNullOrEmpty(m_JPD.SERVER_OUTPUT_DIR))
        {
            if (m_JPD.SERVER_COMPILE_MODE == "RPC")
            {
                if (!Compile_RPC_SERVER())
                {
                    return false;
                }
            }
            else if (m_JPD.SERVER_COMPILE_MODE == "HEADER_ONLY")
            {
                if (!Compile_HDR_ONLY_SERVER())
                {
                    return false;
                }
            }
        }

        if (!string.IsNullOrEmpty(m_JPD.CLIENT_OUTPUT_DIR))
        {
            if (m_JPD.CLIENT_COMPILE_MODE == "RPC")
            {
                if(!Compile_RPC_CLIENT())
                {
                    return false;   
                }
            }
            else if(m_JPD.CLIENT_COMPILE_MODE == "HEADER_ONLY")
            {
                throw new NotImplementedException();
            }
        }

        return true;
    }

    private void AllocJpdMessageID()
    {
        foreach (var jpdNamespaces in m_JPD.JPD_Namespaces)
        {
            int namespaceID = int.Parse(jpdNamespaces.ID);
            for (int i = 0; i < jpdNamespaces.Defines.Count; i++)
            {
                jpdNamespaces.Defines[i].ID = (namespaceID + i).ToString();
            }
        }
    }

    private bool Compile_HDR_ONLY_SERVER()
    {
        string protocolHdr = m_JPD.SERVER_OUTPUT_DIR + "\\Protocol.h";

        string ProtocolHdrContent = $@"
#pragma once
#include <minwindef.h>

";
        foreach(var jpdConstGroup in m_JPD.JPD_ConstGroups)
        {
            ProtocolHdrContent += $@"
struct {jpdConstGroup.Name}
{{";
            foreach(var jpdConst in jpdConstGroup.Consts)
            {
                ProtocolHdrContent += $@"
    static const {jpdConst.Type} {jpdConst.Name} = {jpdConst.Value};";
            }
            ProtocolHdrContent += $@"
}};
";
        }

        foreach (var jpdEnum in m_JPD.JPD_Enums)
        {
            ProtocolHdrContent += $@"
enum class {jpdEnum.Name}
{{";
            foreach (var field in jpdEnum.Fields)
            {
                ProtocolHdrContent += $@"
    {field},";
            }
            ProtocolHdrContent += $@"
}};
";
        }

        string typeOfMsgType = "";
        if(simpleHdrMode) { typeOfMsgType = "BYTE"; }
        else { typeOfMsgType = "WORD";  }

        foreach(var jpdNamespace in m_JPD.JPD_Namespaces)
        {
            ProtocolHdrContent += $@"
namespace {jpdNamespace.Namespace}
{{";
            foreach (var jpdMessage in jpdNamespace.Defines)
            {
                ProtocolHdrContent += $@"
    static const {typeOfMsgType} {jpdMessage.Dir}_{jpdMessage.Message} = {jpdMessage.ID};";
            }

            ProtocolHdrContent += $@"

#pragma pack(push, 1)
";
            foreach(var jpdMessage in jpdNamespace.Defines)
            {
                if (jpdMessage.Param.Count == 0)
                {
                    ProtocolHdrContent += $@"
    struct MSG_{jpdMessage.Dir}_{jpdMessage.Message} {{
        WORD type;
    }};
";
                }
                else
                {
                    ProtocolHdrContent += $@"
    struct MSG_{jpdMessage.Dir}_{jpdMessage.Message}
    {{
        WORD type;";
                    foreach (var parameters in jpdMessage.Param)
                    {
                        if (!string.IsNullOrEmpty(parameters.FixedLenOfArray))
                        {
                            ProtocolHdrContent += $@"
        {parameters.Type} {parameters.Name}[{parameters.FixedLenOfArray}];";
                        }
                        else
                        {
                            ProtocolHdrContent += $@"
        {parameters.Type} {parameters.Name};";
                        }
                    }

                    ProtocolHdrContent += $@"
    }};
";
                }
            }

            ProtocolHdrContent += $@"
#pragma pack(pop)
}};
";
        }

        using (FileStream fs = OpenFile(protocolHdr))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(ProtocolHdrContent);
            }
        }

        return true;
    }

    private bool Compile_RPC_SERVER()
    {
        foreach(var jpdNamespace in m_JPD.JPD_Namespaces)
        {
            MakeComm_Server(jpdNamespace);
            MakeProxy_Server(jpdNamespace);
            MakeStub_Server(jpdNamespace);
        }
        return true;
    }

    private void MakeProxy_Server(JPD_NAMESPACE jpdNamespace)
    {
        string proxyHdr = m_JPD.SERVER_OUTPUT_DIR + "\\Proxy_" + jpdNamespace.Namespace + ".h";
        string proxyCpp = m_JPD.SERVER_OUTPUT_DIR + "\\Proxy_" + jpdNamespace.Namespace + ".cpp";

        using (FileStream fsHdr = OpenFile(proxyHdr))
        {
            using (StreamWriter swHdr = new StreamWriter(fsHdr))
            {
                swHdr.WriteLine("#pragma once");
                swHdr.WriteLine("#include \"Common_" + jpdNamespace.Namespace + ".h\"");
                swHdr.WriteLine();

                MakeProxyHdr_Server(swHdr, jpdNamespace, "S2C");
                MakeProxyHdr_Server(swHdr, jpdNamespace, "C2S");
            }
        }

        using (FileStream fsCpp = OpenFile(proxyCpp))
        {
            using (StreamWriter swCpp = new StreamWriter(fsCpp))
            {
                swCpp.WriteLine("#include \"Proxy_" + jpdNamespace.Namespace + ".h\"");
                swCpp.WriteLine();

                MakeProxyCpp_Server(swCpp, jpdNamespace, "S2C");
                MakeProxyCpp_Server(swCpp, jpdNamespace, "C2S");
            }
        }
    }

    private void MakeProxyHdr_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");
        sw.WriteLine();
        sw.WriteLine("\tclass Proxy : public JNetProxy");
        sw.WriteLine("\t{");
        sw.WriteLine("\tpublic: ");
        foreach(var msg in jpdNamespace.Defines)
        {
            if(msg.Dir == direction)
            {
                sw.Write("\t\tvirtual bool " + msg.Message + "(HostID remote");
                foreach(var param in msg.Param)
                {
                    sw.Write(", " + param.Type + " " + param.Name);
                }
                sw.WriteLine(");");
            }
        }
        sw.WriteLine();
        sw.WriteLine("\t\tvirtual RpcID* GetRpcList() override { return gRpcList; }");
        sw.WriteLine("\t\tvirtual int GetRpcListCount() override { return gRpcListCount; }");
        sw.WriteLine(("\t};"));
        sw.WriteLine("}");
        sw.WriteLine();

    }
    private void MakeProxyCpp_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");
        foreach(var msg in jpdNamespace.Defines)
        {
            if(msg.Dir == direction)
            {
                sw.Write("\tbool Proxy::" + msg.Message + "(HostID remote");
                foreach(var param in msg.Param)
                {
                    sw.Write(", " + param.Type + " " + param.Name);
                }
                sw.WriteLine(") {");
                sw.WriteLine("\t\tuint32_t msgLen = " + sizeofStr(msg.Param));
                sw.WriteLine("\t\tstJNetSession* jnetSession = GetJNetSession(remote);");
                sw.WriteLine("\t\tif (jnetSession != nullptr) {");
                sw.WriteLine("\t\t\tJBuffer& buff = jnetSession->sendBuff;");
                sw.WriteLine("\t\t\tif (buff.GetFreeSize() >= sizeof(stMSG_HDR) + msgLen) {");

                sw.WriteLine("\t\t\t\tstMSG_HDR hdr;");
                sw.WriteLine("\t\t\t\thdr.byCode = " + validCode + ";");
                sw.WriteLine("\t\t\t\thdr.bySize = " + sizeofStr(msg.Param));
                sw.WriteLine("\t\t\t\thdr.byType = RPC_" + msg.Message + ";");
                sw.WriteLine("\t\t\t\tbuff << hdr;");
                foreach (var param in msg.Param)
                {
                    sw.WriteLine("\t\t\t\tbuff << " + param.Name + ";");
                }
                sw.WriteLine("\t\t\t}");
                sw.WriteLine("\t\t\telse {");
                sw.WriteLine("\t\t\t\treturn false;");
                sw.WriteLine("\t\t\t}");
                sw.WriteLine("\t\t}");
                sw.WriteLine("\t\telse {");
                sw.WriteLine("\t\t\treturn false;");
                sw.WriteLine("\t\t}");
                sw.WriteLine();
                sw.WriteLine("\t\treturn true;");
                sw.WriteLine("\t}");
            }
        }
        sw.WriteLine("}");
    }
    private string sizeofStr(List<JPD_PARAM> paramList)
    {
        string str = "";
        for(int i=0; i<paramList.Count; i++)
        {
            str += "sizeof(" + paramList[i].Name + ")";
            if(i == paramList.Count - 1)
            {
                str += ";";
            }
            else
            {
                str += " + ";
            }
        }

        return str;
    }

    private void MakeStub_Server(JPD_NAMESPACE jpdNamespace)
    {
        string stubHdr = m_JPD.SERVER_OUTPUT_DIR + "\\Stub_" + jpdNamespace.Namespace + ".h";
        string stubCpp = m_JPD.SERVER_OUTPUT_DIR + "\\Stub_" + jpdNamespace.Namespace + ".cpp";

        using (FileStream fsHdr = OpenFile(stubHdr))
        {
            using (StreamWriter swHdr = new StreamWriter(fsHdr))
            {
                swHdr.WriteLine("#pragma once");
                swHdr.WriteLine("#include \"Common_" + jpdNamespace.Namespace + ".h\"");
                swHdr.WriteLine();

                MakeStubHdr_Server(swHdr, jpdNamespace, "S2C");
                MakeStubHdr_Server(swHdr, jpdNamespace, "C2S");
            }
        }

        using (FileStream fsCpp = OpenFile(stubCpp))
        {
            using (StreamWriter swCpp = new StreamWriter(fsCpp))
            {
                swCpp.WriteLine("#include \"Stub_" + jpdNamespace.Namespace + ".h\"");
                swCpp.WriteLine();

                MakeStubCpp_Server(swCpp, jpdNamespace, "S2C");
                MakeStubCpp_Server(swCpp, jpdNamespace, "C2S");
            }
        }
    }

    private void MakeStubHdr_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");
        sw.WriteLine();
        sw.WriteLine("\tclass Stub : public JNetStub");
        sw.WriteLine("\t{");
        sw.WriteLine("\tpublic:");
        foreach(var msg in jpdNamespace.Defines)
        {
            if (msg.Dir == direction)
            {
                string fdef = msg.Message + "(HostID remote";
                foreach (var param in msg.Param)
                {
                    //sw.Write(", " + param.Type + " " + param.Name);
                    fdef += (", " + param.Type + " " + param.Name);
                }
                fdef += ")";

                sw.Write("\t\tvirtual bool ");
                sw.Write(fdef);
                sw.WriteLine(" { return false; }");
                sw.WriteLine("#define JPDEC_" + jpdNamespace.Namespace + "_" + direction + "_" + msg.Message + " bool " + fdef);
                sw.WriteLine("#define JPDEF_" + jpdNamespace.Namespace + "_" + direction + "_" + msg.Message + "(DerivedClass) bool DerivedClass::" + fdef);
            }
        }

        sw.WriteLine();
        sw.WriteLine("\t\tRpcID* GetRpcList() override { return gRpcList; }");
        sw.WriteLine("\t\tint GetRpcListCount() override { return gRpcListCount; }");
        sw.WriteLine("\t\tvoid ProcessReceivedMessage(HostID remote, JBuffer& jbuff) override;");
        sw.WriteLine("\t};");
        sw.WriteLine("}");
        sw.WriteLine();
    }

    private void MakeStubCpp_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");
        sw.WriteLine();
        sw.WriteLine("\tvoid Stub::ProcessReceivedMessage(HostID remote, JBuffer& jbuff) {");
        //sw.WriteLine("\t\twhile(true) {");
        sw.WriteLine("\t\tif (jbuff.GetUseSize() < sizeof(stMSG_HDR)) {");
        sw.WriteLine(("\t\t\treturn;"));
        sw.WriteLine("\t\t}");
        sw.WriteLine(("\t\tstMSG_HDR hdr;"));
        sw.WriteLine("\t\tjbuff.Peek(&hdr);");
        sw.WriteLine("\t\tif (sizeof(hdr) + hdr.bySize > jbuff.GetUseSize()) {");
        sw.WriteLine("\t\t\treturn;");
        sw.WriteLine("\t\t}");
        sw.WriteLine("\t\tjbuff.Dequeue((BYTE*)&hdr, sizeof(hdr));");
        sw.WriteLine("\t\tswitch(static_cast<RpcID>(hdr.byType)) {");
        foreach(var msg in jpdNamespace.Defines)
        {
            if(msg.Dir == direction)
            {
                string fcall = msg.Message + "(remote";
                sw.WriteLine("\t\tcase RPC_" + msg.Message + ":");
                sw.WriteLine("\t\t{");
                foreach(var param in msg.Param)
                {
                    fcall += ", " + param.Name;
                    sw.WriteLine("\t\t\t" + param.Type + " " + param.Name + ";");
                    sw.WriteLine("\t\t\tjbuff >> " + param.Name + ";");
                }
                fcall += ")";
                sw.WriteLine("\t\t\t" + fcall + ";");
                sw.WriteLine("\t\t}");
                sw.WriteLine("\t\tbreak;");
            }
        }
        sw.WriteLine("\t\t}");
        //sw.WriteLine("\t\t}");
        sw.WriteLine("\t}");
        sw.WriteLine("}");
        sw.WriteLine();
    }

    private void MakeComm_Server(JPD_NAMESPACE jpdNamespace)
    {
        string commHdr = m_JPD.SERVER_OUTPUT_DIR + "\\Common_" + jpdNamespace.Namespace + ".h";
        string commCpp = m_JPD.SERVER_OUTPUT_DIR + "\\Common_" + jpdNamespace.Namespace + ".cpp";

        using (FileStream fsHdr = OpenFile(commHdr))
        {
            using(StreamWriter swHdr = new StreamWriter(fsHdr))
            {
                swHdr.WriteLine("#pragma once");
                swHdr.WriteLine();

                MakeCommHdr_Server(swHdr, jpdNamespace, "S2C");
                MakeCommHdr_Server(swHdr, jpdNamespace, "C2S");
            }
        }

        using (FileStream fsCpp = OpenFile(commCpp))
        {
            using (StreamWriter swCpp = new StreamWriter(fsCpp))
            {
                swCpp.WriteLine("#include \"Common_" + jpdNamespace.Namespace + ".h\"");
                swCpp.WriteLine();

                MakeCommCpp_Server(swCpp, jpdNamespace, "S2C");
                MakeCommCpp_Server(swCpp, jpdNamespace, "C2S");
            }
        }
    }

    private void MakeCommHdr_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");

        int ID = int.Parse(jpdNamespace.ID);
        for (int i = 0; i < jpdNamespace.Defines.Count; i++)
        {
            JPD_MESSAGE msg = jpdNamespace.Defines[i];
            if (msg.Dir == direction)
            {
                sw.WriteLine("\tstatic const RpcID RPC_" + msg.Message + " = " + (ID + i).ToString() + ";");
            }
        }

        sw.WriteLine();
        sw.WriteLine("\textern RpcID gRpcList[];");
        sw.WriteLine("\textern int gRpcListCount;");
        sw.WriteLine("}");
        sw.WriteLine();
    }
    private void MakeCommCpp_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");

        bool setList = false;
        for (int i = 0; i < jpdNamespace.Defines.Count; i++)
        {
            JPD_MESSAGE msg = jpdNamespace.Defines[i];
            if (msg.Dir == direction)
            {
                setList = true;
                break;
            }
        }
        int cnt = 0;

        if (setList)
        {
            sw.WriteLine("\tRpcID gRpcList[] = {");
            int ID = int.Parse(jpdNamespace.ID);

            for (int i = 0; i < jpdNamespace.Defines.Count; i++)
            {
                JPD_MESSAGE msg = jpdNamespace.Defines[i];
                if (msg.Dir == direction)
                {
                    sw.WriteLine("\t\tRPC_" + msg.Message + ",");
                    cnt++;
                }
            }
            sw.WriteLine("\t};");
            sw.WriteLine();
        }

        sw.WriteLine("\tint gRpcListCount = " + cnt + ";");
        sw.WriteLine("}");
        sw.WriteLine();
    }

    private bool Compile_RPC_CLIENT()
    {
        Make_MessaegeDefine();
        Make_ConstGroups();
        Make_Enums();
        MakeComm_Client();
        MakeProxy_Client(m_JPD.JPD_Namespaces);
        MakeStub_Client(m_JPD.JPD_Namespaces);

        return true;
    }

    private void Make_MessaegeDefine()
    {
        if(m_JPD.JPD_Namespaces.Count == 0)
        {
            return;
        }

        string messageDefineCs = m_JPD.CLIENT_OUTPUT_DIR + "\\JPD_MESSAGE.cs";
        string JpdMessageDefineContent = $@"
using System;
using System.Runtime.InteropServices;
";
        foreach(var jpdNamespace in m_JPD.JPD_Namespaces)
        {
            JpdMessageDefineContent += $@"
    namespace {jpdNamespace.Namespace}
    {{
";
            foreach(var jpdMessage in jpdNamespace.Defines)
            {
                JpdMessageDefineContent += $@"
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class MSG_{jpdMessage.Dir}_{jpdMessage.Message}
        {{
";
                foreach(var paramter in jpdMessage.Param)
                {
                    JpdMessageDefineContent += $@"
            public {TranslateCSharpType(paramter.Type)} {paramter.Name};
";
                }
                JpdMessageDefineContent += $@"
        }}
";
            }
            JpdMessageDefineContent += $@"
    }}
";
        }
    }

    private void Make_ConstGroups()
    {
        if(m_JPD.JPD_ConstGroups.Count == 0)
        {
            return;
        }

        string constGroupsCs = m_JPD.CLIENT_OUTPUT_DIR + "\\JPD_CONST.cs";
        string JpdConstContent = $@"
";
        foreach (var constGroup in m_JPD.JPD_ConstGroups)
        {
            JpdConstContent += $@"
static class {constGroup.Name}
{{";
            foreach(var jpdConst in constGroup.Consts)
            {
                JpdConstContent += $@"
    public const {jpdConst.Type} {jpdConst.Name} = {jpdConst.Value};";
            }
            JpdConstContent += $@"
}};
";
        }

        using (FileStream fs = OpenFile(constGroupsCs))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(JpdConstContent);
            }
        }
    }

    private void Make_Enums()
    {
        if(m_JPD.JPD_Enums.Count == 0)
        {
            return;
        }

        string enumCs = m_JPD.CLIENT_OUTPUT_DIR + "\\JPD_ENUM.cs";
        string JpdEnumContent = $@"
";
        foreach(var jpdEnum in m_JPD.JPD_Enums)
        {
            JpdEnumContent += $@"
public enum {jpdEnum.Name}
{{";
            foreach(var field in jpdEnum.Fields)
            {
                JpdEnumContent += $@"
    {field},";
            }
            JpdEnumContent += $@"
}};
";
        }

        using (FileStream fs = OpenFile(enumCs))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(JpdEnumContent);
            }
        }
    }

    private void MakeStub_Client(List<JPD_NAMESPACE> jpdNamespaces)
    {
        string stubPath = m_JPD.CLIENT_OUTPUT_DIR + "\\Stub.cs";
        string StubContent = $@"
using System;
using System.Collections.Generic;
using UnityEngine;


public class Stub : MonoBehaviour
{{
    public Dictionary<UInt16, Action<byte[]>> methods = new Dictionary<UInt16, Action<byte[]>>();
    
    protected Dictionary<string, UInt16> MessageIDs = new Dictionary<string, UInt16>()
    {{";
        foreach(var jpdNamespace in jpdNamespaces)
        {
            foreach (var jpdMessage in jpdNamespace.Defines)
            {
                if (jpdMessage.Dir == "S2C")
                {
                    StubContent += $@"
        {{""{jpdMessage.Message}"", {jpdMessage.ID}}},";
                }
            }
        }
        StubContent += $@"
    }};
}}
";
        List<string> stubClassDefs = new List<string>();    
        foreach(var jpdNamespace in jpdNamespaces)
        {
            string stubClass = MakeClientStubClass(jpdNamespace);
            stubClassDefs.Add(stubClass);   
        }
    
        using(FileStream fs = OpenFile(stubPath))
        {
            using(StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(StubContent);

                foreach(var stubClassDef in stubClassDefs)
                {
                    sw.Write(stubClassDef);
                }
            }
        }

        foreach (var jpdNamespace in jpdNamespaces)
        {
            MakeClientStubFile(jpdNamespace);
        }
    }

    private void MakeClientStubFile(JPD_NAMESPACE jpdNamespace)
    {
        string path = m_JPD.CLIENT_OUTPUT_DIR + "\\" + jpdNamespace.Namespace + ".cs";

        string stubFileContent = $@"
using System;

public class {jpdNamespace.Namespace} : Stub_{jpdNamespace.Namespace}
{{
    private void Start() 
    {{
        base.Init();
    }}

    private void OnDestroy()
    {{
        base.Clear();  
    }}

";
        foreach(var jpdMessage in jpdNamespace.Defines)
        {
            if(jpdMessage.Dir != "S2C") { continue; }
            string parameters = "";
            for(int i=0; i<jpdMessage.Param.Count; i++)
            {
                string type = TranslateCSharpType(jpdMessage.Param[i].Type);
                if (string.IsNullOrEmpty(jpdMessage.Param[i].FixedLenOfArray))
                {
                    parameters += $"{type} {jpdMessage.Param[i].Name}";
                }
                else
                {
                    parameters += $"{type}[] {jpdMessage.Param[i].Name}";
                }
                if(i !=  jpdMessage.Param.Count - 1)
                {
                    parameters += ", "; 
                }
            }
            stubFileContent += $@"
    protected override void {jpdMessage.Message}({parameters}) 
    {{
        throw new NotImplementedException(""{jpdMessage.Message}"");
    }}
";
        }
        stubFileContent += $@"
}}
";

        using (FileStream fs = OpenFile(path))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(stubFileContent);
            }
        }
    }

    // Stub.cs 에 선언되는 abstract stub 클래스
    private string MakeClientStubClass(JPD_NAMESPACE jpdNamespace)
    {
        string stubClassDef = $@"
public abstract class Stub_{jpdNamespace.Namespace} : Stub
{{
    public void Init() 
    {{";
        foreach(var jpdMessage in jpdNamespace.Defines)
        {
            if (jpdMessage.Dir == "S2C")
            {
                stubClassDef += $@"
        methods.Add(MessageIDs[""{jpdMessage.Message}""], {jpdMessage.Message});";
            }
        }
        stubClassDef += $@"
        RPC.Instance.AttachStub(this);
    }}

    public void Clear()
    {{
        RPC.Instance.DetachStub(this);
    }}
";
        foreach (var jpdMessage in jpdNamespace.Defines)
        {
            if(jpdMessage.Dir != "S2C") { continue; }
            stubClassDef += $@"
    public void {jpdMessage.Message}(byte[] payload)
    {{
        int offset = 0;";
            string parameters = "";
            for(int i=0; i< jpdMessage.Param.Count; i++)
            {
                parameters += jpdMessage.Param[i].Name;
                if(i != jpdMessage.Param.Count - 1)
                {
                    parameters += ", ";
                }

                string type = TranslateCSharpType(jpdMessage.Param[i].Type);

                if (string.IsNullOrEmpty(jpdMessage.Param[i].FixedLenOfArray))
                {
                    if (type == "byte")
                    {
                        stubClassDef += $@"
        {type} {jpdMessage.Param[i].Name} = payload[offset++];";
                    }
                    else if (type == "float")
                    {
                        stubClassDef += $@"
        {type} {jpdMessage.Param[i].Name} = BitConverter.ToSingle(payload, offset); offset += sizeof({type});";
                    }
                    else
                    {
                        stubClassDef += $@"
        {type} {jpdMessage.Param[i].Name} = BitConverter.To{type}(payload, offset); offset += sizeof({type});";
                    }
                }
                else   // Array
                {
                    stubClassDef += $@"
        {type}[] {jpdMessage.Param[i].Name} = new {type}[{jpdMessage.Param[i].FixedLenOfArray}];
        Buffer.BlockCopy(payload, offset, {jpdMessage.Param[i].Name}, 0, sizeof({type}) * {jpdMessage.Param[i].FixedLenOfArray});
        offset += sizeof({type}) * {jpdMessage.Param[i].FixedLenOfArray};";
                }
            }
            stubClassDef += $@"
        {jpdMessage.Message}({parameters});
    }}
";
        }

        foreach (var jpdMessage in jpdNamespace.Defines)
        {
            if(jpdMessage.Dir != "S2C") { continue; }
            string paramters = "";
            for (int i = 0; i < jpdMessage.Param.Count; i++)
            {
                string type = TranslateCSharpType(jpdMessage.Param[i].Type);
                if (string.IsNullOrEmpty(jpdMessage.Param[i].FixedLenOfArray))
                {
                    paramters += $"{type} {jpdMessage.Param[i].Name}";
                }
                else
                {
                    paramters += $"{type}[] {jpdMessage.Param[i].Name}";
                }
                if(i != jpdMessage.Param.Count - 1)
                {
                    paramters += ", ";  
                }
            }
            stubClassDef += $@"
    protected abstract void {jpdMessage.Message}({paramters});
";
        }

        stubClassDef += $@"
}}
";
        return stubClassDef;
    }

    private void MakeComm_Client()
    {
        string commRpcPath = m_JPD.CLIENT_OUTPUT_DIR + "\\RPC.cs";
        string endecode = "";
        if (endecodeFlag)
        {
            endecode = "true";
        }
        else
        {
            endecode = "false";
        }

        // RPC.cs
        string RpcCSContent = "";
        if (simpleHdrMode)
        {
            RpcCSContent += "#define SIMPLE_PACKET";
        }
        else
        {
            RpcCSContent += "//#define SIMPLE_PACKET";
        }
        RpcCSContent += $@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RPC : MonoBehaviour
{{
    static RPC s_Instance;
    public static RPC Instance {{ get {{ Init(); return s_Instance; }} }}
    public static Proxy proxy = new Proxy();
    public static byte ValidCode = {validCode};
    public static bool EnDecodeFlag = {endecode};
    private Dictionary<UInt16, Action<byte[]>> StubMethods = new Dictionary<UInt16, Action<byte[]>>();

    NetworkManager m_NetworkManager = new NetworkManager();
    public static NetworkManager Network {{ get {{ return Instance.m_NetworkManager; }} }}

    private string ServerIP;
    private UInt16 ServerPort;

    private void Start()
    {{
        Init();
    }}

    private static void Init()
    {{
        GameObject go = GameObject.Find(""@RPC"");
        if (go == null)
        {{
            go = new GameObject {{ name = ""@RPC"" }};
            go.AddComponent<RPC>();

            DontDestroyOnLoad(go);
            s_Instance = go.GetComponent<RPC>();
        }}
    }}

    public bool Initiate(string serverIP, UInt16 serverPort)
    {{
        ServerIP = serverIP;
        ServerPort = serverPort;   

        if (!Network.Connected)
        {{
            return Network.Connect(serverIP, serverPort);
        }}

        return true;
    }}

    public void AttachStub(Stub stub)
    {{
        foreach (var method in stub.methods)
        {{
            if (StubMethods.ContainsKey(method.Key))
            {{
                StubMethods.Remove(method.Key);
            }}
            StubMethods.Add(method.Key, method.Value);
        }}
    }}

    public void DetachStub(Stub stub)
    {{
        foreach(var method in stub.methods)
        {{
            if (StubMethods.ContainsKey(method.Key))
            {{
                StubMethods.Remove(method.Key);
            }}
        }}
    }}

    public NetworkManager AllocNewClientSession()
    {{
        NetworkManager newSession = new NetworkManager();
        if(newSession != null)
        {{
            if(newSession.Connect(ServerIP, ServerPort))
            {{
                return newSession;
            }}
        }}

        return null;
    }}

    private void Update()
    {{
#if SIMPLE_PACKET
        while (Network.ReceivedDataSize() >= Marshal.SizeOf<JNET_PROTOCOL.SIMPLE_MSG_HDR>())
        {{
            JNET_PROTOCOL.SIMPLE_MSG_HDR hdr;
            Network.Peek<JNET_PROTOCOL.SIMPLE_MSG_HDR>(out hdr);
            if (Marshal.SizeOf<JNET_PROTOCOL.SIMPLE_MSG_HDR>() + hdr.MsgLen <= Network.ReceivedDataSize())
            {{
                Network.ReceiveData<JNET_PROTOCOL.SIMPLE_MSG_HDR>(out hdr);
                byte[] payload = Network.ReceiveBytes(hdr.MsgLen);
                if (StubMethods.ContainsKey(hdr.MsgType))
                {{
                    StubMethods[hdr.MsgType].Invoke(payload);
                }}
            }}
            else
            {{
                break;
            }}
        }}
#else
        while (Network.ReceivedDataSize() >= Marshal.SizeOf<JNET_PROTOCOL.MSG_HDR>())
        {{
            byte[] payload;
            if (Network.ReceivePacketBytes(out payload, EnDecodeFlag))
            {{
                UInt16 msgType = BitConverter.ToUInt16(payload, 0);
                if (StubMethods.ContainsKey(msgType))
                {{
                    StubMethods[msgType].Invoke(new ArraySegment<byte>(payload, sizeof(UInt16), payload.Length - sizeof(UInt16)).ToArray());
                }}
            }}
            else
            {{
                break;
            }}
        }}
#endif
    }}
}}
";
        using(FileStream fsRpc = OpenFile(commRpcPath))
        {
            using(StreamWriter sw = new StreamWriter(fsRpc))
            {
                sw.Write(RpcCSContent);
            }
        }
    }

    private void MakeProxy_Client(List<JPD_NAMESPACE> jpdNamespaces)
    {
        string proxyPath = m_JPD.CLIENT_OUTPUT_DIR + "\\Proxy.cs";
        UInt32 ValidCode = 0;

        string intro = $@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class Proxy
{{
    Dictionary<string, UInt16> MessageIDs = new Dictionary<string, UInt16>()
    {{
";
        foreach (var jpdNamespace in jpdNamespaces)
        {
            foreach (var jpdMessage in jpdNamespace.Defines)
            {
                if (jpdMessage.Dir == "C2S")
                {
                    intro += $@"
        {{""{jpdMessage.Message}"", {jpdMessage.ID}}},";
                }
            }
        }
        intro += $@"
    }};

";

        string outro = $@"
}}
";

        List<string> proxyFuncDefs = new List<string>();
        foreach (var jpdNamespace in jpdNamespaces)
        {
            foreach(var jpdMessage in jpdNamespace.Defines)
            {
                if (jpdMessage.Dir == "C2S")
                {
                    string defStr = MakeClientProxyFunc(jpdMessage);
                    proxyFuncDefs.Add(defStr);
                }
            }
        }

        using (FileStream fs = OpenFile(proxyPath))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(intro);

                foreach (var proxyFuncDef in proxyFuncDefs)
                {
                    sw.Write(proxyFuncDef);
                }

                sw.Write(outro);
            }
        }
    }

    private string MakeClientProxyFunc(JPD_MESSAGE jpdMessage)
    {
        string parmeters = "";
        string sizeofParamters = "";
        for(int i=0; i<jpdMessage.Param.Count; i++)
        {
            string type = TranslateCSharpType(jpdMessage.Param[i].Type);
            if (string.IsNullOrEmpty(jpdMessage.Param[i].FixedLenOfArray))
            {
                parmeters += $"{type} {jpdMessage.Param[i].Name}";
                sizeofParamters += $"sizeof({type})";
            }
            else {
                parmeters += $"{type}[] {jpdMessage.Param[i].Name}";
                sizeofParamters += $"sizeof({type}) * {jpdMessage.Param[i].FixedLenOfArray}";
            }

            parmeters += ", ";
            if (i != jpdMessage.Param.Count - 1)
            {
                //parmeters += ", ";
                sizeofParamters += " + ";
            }
        }

        string funcDef = $@"
    public void {jpdMessage.Message}({parmeters} NetworkManager sesion = null)
    {{";
        if (simpleHdrMode)
        {
            funcDef += $@"
        stMSG_HDR hdr = new stMSG_HDR();
        hdr.Code = RPC.ValidCode;
        hdr.MsgLen = (Byte)({sizeofParamters});
        hdr.MsgType = MessageIDs[""{jpdMessage.Message}""];

        byte[] bytes = new byte[Marshal.SizeOf(hdr) + hdr.MsgLen];

        byte[] bytesHdr = new byte[Marshal.SizeOf(hdr)];
        RPC.Network.MessageToBytes<stMSG_HDR>(hdr, bytesHdr);

        int offset = 0;
        Buffer.BlockCopy(bytesHdr, 0, bytes, offset, Marshal.SizeOf(hdr)); offset += Marshal.SizeOf(hdr);";

            foreach (var param in jpdMessage.Param)
            {
                string csharType = TranslateCSharpType(param.Type);
                if (csharType == "byte")
                {
                    funcDef += $@"
        bytes[offset++] = {param.Name};";
                }
                else
                {
                    funcDef += $@"
        Buffer.BlockCopy(BitConverter.GetBytes({param.Name}), 0, bytes, offset, sizeof({csharType})); offset += sizeof({csharType});";
                }
            }

            funcDef += $@"
        RPC.Network.SendBytes(bytes);          
";
        }
        else
        {
            if(jpdMessage.Param.Count > 0)
            {
                sizeofParamters = " + " + sizeofParamters;
            }
            funcDef += $@"
        UInt16 type = MessageIDs[""{jpdMessage.Message}""];
        byte[] payload = new byte[sizeof(UInt16){sizeofParamters}];
        int offset = 0;
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, payload, offset, sizeof(UInt16)); offset += sizeof(UInt16);";
            foreach(var  param in jpdMessage.Param)
            {
                string csharType = TranslateCSharpType(param.Type);  
                if (string.IsNullOrEmpty(param.FixedLenOfArray))
                {
                    if(csharType == "byte")
                    {
                        funcDef += $@"
        payload[offset++] = {param.Name};";
                    }
                    else
                    {
                        funcDef += $@"
        Buffer.BlockCopy(BitConverter.GetBytes({param.Name}), 0, payload, offset, sizeof({csharType})); offset += sizeof({csharType});";
                    }
                }
                else
                {
                    funcDef += $@"
        Buffer.BlockCopy({param.Name}, 0, payload, offset, {param.Name}.Length); offset += sizeof({csharType}) * {param.FixedLenOfArray};";
                }
            }
        }
        funcDef += $@"
        if (sesion == null) {{ RPC.Network.SendPacketBytes(payload, RPC.EnDecodeFlag); }}
        else {{ sesion.SendPacketBytes(payload, RPC.EnDecodeFlag); }}
    }}
";

        return funcDef;
    }

    string TranslateCppType(string type)
    {
        string ret = string.Empty;

        if (type == "byte" || type == "Byte" || type == "UINT8" || type == "uint8")
        {
            ret = "BYTE";
        }
        else if (type == "char" || type == "Char" || type == "INT8" || type == "int8")
        {
            ret = "CHAR";
        }
        else if (type == "USHORT" || type == "ushort" || type == "UInt16" || type == "uint16")
        {
            ret = "UINT16";
        }
        else if (type == "SHORT" || type == "short" || type == "Int16" || type == "int16")
        {
            ret = "INT16";
        }
        else if (type == "UINT" || type == "uint" || type == "UInt32" || type == "uint32")
        {
            ret = "UINT32";
        }
        else if (type == "INT" || type == "int" || type == "Int32" || type == "int32")
        {
            ret = "INT32";
        }
        else if (type == "UInt64" || type == "uint64")
        {
            ret = "UINT64";
        }
        else if (type == "Int64" || type == "int64")
        {
            ret = "INT64";
        }
        else
        {
            ret = type;
        }

        return ret;
    }

    string TranslateCSharpType(string type)
    {
        string ret = string.Empty;

        if(type == "FLOAT" || type == "Float")
        {
            ret = "float";
        }
        else if(type == "DOUBLE" || type == "Double")
        {
            ret = "double";
        }
        if(type == "BYTE" || type == "byte" || type =="UINT8" || type == "uint8")
        {
            ret = "byte";
        }
        else if(type == "CHAR" || type == "char" || type == "INT8" || type == "int8")
        {
            //ret = "char";
            // => C#의 Char, char은 2바이트
            ret = "byte";
        }
        else if(type == "USHORT" || type == "ushort" || type == "UINT16" || type == "uint16")
        {
            ret = "UInt16";
        }
        else if (type == "SHORT" || type == "short" || type == "INT16" || type == "int16")
        {
            ret = "Int16";
        }
        else if (type == "UINT" || type == "uint" || type == "UINT32" || type == "uint32")
        {
            ret = "UInt32";
        }
        else if (type == "INT" || type == "int" || type == "INT32" || type == "int32")
        {
            ret = "Int32";
        }
        else if (type == "UINT64" || type == "uint64")
        {
            ret = "UInt64";
        }
        else if (type == "INT64" || type == "int64")
        {
            ret = "Int64";
        }
        else
        {
            ret = type;
        }

        return ret;
    }

    private FileStream OpenFile(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
    }
}
