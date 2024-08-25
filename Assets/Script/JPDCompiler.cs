using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Experimental.GlobalIllumination;

public class JPDCompiler : MonoBehaviour
{
    static JPDCompiler s_Instance;
    public static JPDCompiler Instance { get { Init(); return s_Instance; } }

    public static JPD_SCHEMA JPDSchema { get { return Instance.m_JPD; } }

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

    private int m_CompileMode = 0;
    string m_ServerPath = string.Empty;
    string m_ClientPath = string.Empty;
    JPD_SCHEMA m_JPD = new JPD_SCHEMA();

    string TypeOfHdrCode = "byte";
    string TypeOfHdrMsgLen = "byte";
    string TypeOfHdrMsgType = "byte";
    private byte validCode;
    
    public bool SetVaildCode(int code)
    {
        if(code > 255)
        {
            return false;
        }

        validCode = (byte)code;
        return true;    
    }

    public JPD_NAMESPACE AddJpdNamespace(string name, string id)
    {
        foreach (var item in m_JPD.JPD)
        {
            if (item.Namespace == name)
            {
                return null;
            }
        }

        int namespaceID = m_JPD.JPD.Count;
        JPD_NAMESPACE newNamespace = new JPD_NAMESPACE(name, namespaceID.ToString());
        newNamespace.Namespace = name;
        newNamespace.ID = namespaceID.ToString();
        m_JPD.JPD.Add(newNamespace);

        return newNamespace;
    }

    public void SaveJsonFile(string path)
    {
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
        if (m_JPD.COMPILE_MODE == "RPC")
        {
            return Compile_RPC();
        }
        else if (m_JPD.COMPILE_MODE == "HEADER_ONLY")
        {
            return Compile_HDR_ONLY();
        }

        return false;
    }

    private bool Compile_RPC()
    {
        foreach (var jpdNamespaces in m_JPD.JPD)
        {
            int namespaceID = int.Parse(jpdNamespaces.ID);
            for(int i=0; i< jpdNamespaces.Defines.Count; i++)
            {
                jpdNamespaces.Defines[i].ID = (namespaceID + i).ToString();
            }
        }

        if (!string.IsNullOrEmpty(m_JPD.SERVER_OUTPUT_DIR))
        {
            if (!Compile_RPC_SERVER())
            {
                return false;
            }
        }
        if (!string.IsNullOrEmpty(m_JPD.CLIENT_OUTPUT_DIR))
        {
            if (!Compile_RPC_CLIENT())
            {
                return false;
            }
        }
        return true;
    }

    private bool Compile_RPC_SERVER()
    {
        foreach(var jpdNamespace in m_JPD.JPD)
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
        MakeComm_Client();
        MakeProxy_Client(m_JPD.JPD);
        MakeStub_Client(m_JPD.JPD);

        return true;
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
    public Dictionary<int, Action<byte[]>> methods = new Dictionary<int, Action<byte[]>>();
    
    protected Dictionary<string, byte> MessageIDs = new Dictionary<string, byte>()
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

";
        foreach(var jpdMessage in jpdNamespace.Defines)
        {
            if(jpdMessage.Dir != "S2C") { continue; }
            string parameters = "";
            for(int i=0; i<jpdMessage.Param.Count; i++)
            {
                string type = TranslateCSharpType(jpdMessage.Param[i].Type);
                parameters += type + " " + jpdMessage.Param[i].Name;
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

    private string MakeClientStubClass(JPD_NAMESPACE jpdNamespace)
    {
        string stubClassDef = $@"
public abstract class Stub_{jpdNamespace.Namespace} : Stub
{{
    public void Init() 
    {{
";
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
                if (type == "Byte")
                {
                    stubClassDef += $@"
        {type} {jpdMessage.Param[i].Name} = payload[offset++];";
                }
                else
                {
                    stubClassDef += $@"
        {type} {jpdMessage.Param[i].Name} = BitConverter.To{type}(payload, offset); offset += sizeof({type});";
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
                paramters += (type + " " + jpdMessage.Param[i].Name);
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
        string commNetworkPath = m_JPD.CLIENT_OUTPUT_DIR + "\\NetworkManager.cs";

        UInt32 ValidCode = 0;

        UInt32 RECV_BUFFER_LENGTH = 0;

        string RpcCSContent = $@"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct stMSG_HDR
{{
    public {TypeOfHdrCode} Code;
    public {TypeOfHdrMsgLen} MsgLen;
    public {TypeOfHdrMsgType} MsgType;
}}

public class RPC : MonoBehaviour
{{
    static RPC s_Instance;
    public static RPC Instance {{ get {{ Init(); return s_Instance; }} }}
    public static Proxy proxy = new Proxy();
    public static {TypeOfHdrCode} ValidCode = {ValidCode};
    private Dictionary<int, Action<byte[]>> StubMethods = new Dictionary<int, Action<byte[]>>();

    NetworkManager m_NetworkManager = new NetworkManager();
    public static NetworkManager Network {{ get {{ return Instance.m_NetworkManager; }} }}

    private void Start()
    {{
        Init();
    }}

    private static void Init()
    {{
        GameObject go = GameObject.Find(""@RPC"");
        if(go == null)
        {{
            go = new GameObject {{ name = ""@RPC"" }};
            go.AddComponent<RPC>();

            DontDestroyOnLoad(go);
            s_Instance = go.GetComponent<RPC>();
        }}
    }}

    public bool Initiate(string serverIP, UInt16 serverPort)
    {{
        return Network.Connect(serverIP, serverPort);
    }}

    public void AttachStub(Stub stub)
    {{
        foreach (var method in stub.methods)
        {{
            StubMethods.Add(method.Key, method.Value);
        }}
    }}

    private void Update()
    {{
        while(Network.ReceivedDataSize() >= Marshal.SizeOf<stMSG_HDR>())
        {{
            stMSG_HDR hdr;
            Network.Peek<stMSG_HDR>(out hdr);
            if (Marshal.SizeOf<stMSG_HDR>() + hdr.MsgLen <= Network.ReceivedDataSize())
            {{
                Network.ReceiveData<stMSG_HDR>(out hdr);
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
    }}
}}
";

        string NetMgrCSContent = $@"
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public class NetBuffer
{{
    private byte[] m_Buffer;
    private int m_Index;

    public int BufferedSize {{ get {{ return m_Index; }} }}
    public int FreeSize {{ get {{ return m_Buffer.Length - m_Index; }} }}

    public NetBuffer(int buffSize)
    {{
        m_Buffer = new byte[buffSize];
    }}

    public bool Peek(byte[] dest, int length, int offset = 0)
    {{
        if(length + offset > m_Index)
        {{
            return false;
        }}

        Array.Copy(m_Buffer, offset, dest, 0, length);
        return true;
    }}

    public bool Write(byte[] source, int length, int offset = 0)
    {{
        if (m_Index + length > m_Buffer.Length)
        {{
            return false;
        }}

        Array.Copy(source, offset, m_Buffer, m_Index, length);
        m_Index += length;
        return true;
    }}
    public bool WriteFront(byte[] source, int length, int offset = 0)
    {{
        if (m_Index + length > m_Buffer.Length)
        {{
            return false;
        }}

        if (m_Index == 0)
        {{
            Write(source, length, offset);
        }}
        else
        {{
            byte[] newBuffer = new byte[m_Buffer.Length];

            Array.Copy(source, 0, newBuffer, 0, length);
            Array.Copy(m_Buffer, 0, newBuffer, length, BufferedSize);
            m_Index = BufferedSize + length;
            m_Buffer = newBuffer;
        }}

        return true;
    }}

    public bool Read(byte[] dest, int length)
    {{
        if (m_Index < length)
        {{
            return false;
        }}

        Array.Copy(m_Buffer, dest, length);

        if (m_Index == length)
        {{
            m_Index = 0;
        }}
        else
        {{
            byte[] newBuffer = new byte[m_Buffer.Length];
            Array.Copy(m_Buffer, length, newBuffer, 0, BufferedSize - length);
            m_Index = BufferedSize - length;
            m_Buffer = newBuffer;
        }}
        return true;
    }}
}}

public class NetworkManager
{{
    private TcpClient m_TcpClient = null;
    private NetworkStream m_Stream = null;

    private NetBuffer m_RecvBuffer = new NetBuffer({RECV_BUFFER_LENGTH});

    private System.Random m_RandKeyMaker = new System.Random();

    public NetworkManager(IPEndPoint ipEndPoint = null)
    {{
        if (ipEndPoint == null)
        {{
            m_TcpClient = new TcpClient();
        }}
        else
        {{
            m_TcpClient = new TcpClient(ipEndPoint);
            m_Stream = m_TcpClient.GetStream();
        }}
    }}

    public bool Connected {{ get {{ return m_TcpClient.Connected; }} }}

    public bool Connect(string serverIP = ""127.0.0.1"", int port = 7777)
    {{
        if (!Connected)
        {{
            try
            {{
                m_TcpClient.Connect(IPAddress.Parse(serverIP), port);
                m_Stream = m_TcpClient.GetStream();
            }}
            catch
            {{
                return false;
            }}
        }}

        return true;
    }}

    public void Disconnect()
    {{
        if (Connected)
        {{
            m_TcpClient.Close();
        }}
    }}

    public void ClearRecvBuffer()
    {{
        while (m_Stream.DataAvailable)
        {{
            byte[] buffer = new byte[1024];
            m_Stream.Read(buffer, 0, buffer.Length);
        }}
    }}

    public void SendBytes(byte[] data)
    {{
        m_Stream.Write(data);
    }}

    public bool ReceiveDataAvailable()
    {{
        return m_Stream.DataAvailable;
    }}

    public int ReceivedDataSize()
    {{
        return m_RecvBuffer.BufferedSize + m_TcpClient.Available;
    }}

    public bool Peek<T>(out T data)
    {{
        int dataSize = Marshal.SizeOf(typeof(T));
        if (m_RecvBuffer.BufferedSize + m_TcpClient.Available < dataSize)
        {{
            data = default(T);
            return false;
        }}

        if(m_RecvBuffer.BufferedSize < dataSize)
        {{
            int resSize = dataSize - m_RecvBuffer.BufferedSize; 
            if(m_RecvBuffer.FreeSize < resSize)
            {{
                data = default(T);
                return false;
            }}

            byte[] buffer = new byte[resSize];  
            m_Stream.Read(buffer, 0, buffer.Length);
            m_RecvBuffer.Write(buffer, resSize, 0); 
        }}

        byte[] bytes = new byte[dataSize];
        m_RecvBuffer.Peek(bytes, bytes.Length, 0);
        data = BytesToMessage<T>(bytes);

        return true;
    }}

    public byte[] ReceiveBytes(int length)
    {{
        byte[] bytes = new byte[length];
        if (m_RecvBuffer.BufferedSize + m_TcpClient.Available < length)
        {{
            return null;
        }}

        if(m_RecvBuffer.BufferedSize >= length)
        {{
            m_RecvBuffer.Read(bytes, length);
        }}
        else
        {{
            int buffedSize = m_RecvBuffer.BufferedSize;
            m_RecvBuffer.Read(bytes, buffedSize);
            m_Stream.Read(bytes, buffedSize, length - buffedSize);
        }}
        return bytes;
    }}

    public bool ReceiveData<T>(out T data)
    {{
        data = default(T);

        if (ReceivedDataSize() < Marshal.SizeOf<T>())
        {{
            return false;
        }}

        byte[] receivedBytes = new byte[Marshal.SizeOf<T>()];
        if(m_RecvBuffer.BufferedSize >= receivedBytes.Length)
        {{
            m_RecvBuffer.Read(receivedBytes, receivedBytes.Length);
        }}
        else
        {{
            int bufferedSize = m_RecvBuffer.BufferedSize;
            m_RecvBuffer.Read(receivedBytes, bufferedSize);
            m_Stream.Read(receivedBytes, bufferedSize, receivedBytes.Length - bufferedSize);
        }}

        data = BytesToMessage<T>(receivedBytes);    
        return true;
    }}

    private byte[] MessageToBytes<T>(T str)
    {{
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {{
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
        }}
        catch (Exception ex)
        {{
            Debugger.Break();
        }}
        finally
        {{
            // 할당받은 네이티브 메모리 해제
            Marshal.FreeHGlobal(ptr);
        }}

        return arr;
    }}

    public void MessageToBytes<T>(T str, byte[] dest)
    {{
        int size = Marshal.SizeOf(str);

        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {{
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, dest, 0, size);
        }}
        catch (Exception ex)
        {{
            Debugger.Break();
        }}
        finally
        {{
            // 할당받은 네이티브 메모리 해제
            Marshal.FreeHGlobal(ptr);
        }}
    }}

    public T BytesToMessage<T>(byte[] bytes)
    {{
        T str = default(T);
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);

        try
        {{
            Marshal.Copy(bytes, 0, ptr, size);
            str = Marshal.PtrToStructure<T>(ptr);
        }}
        catch (Exception ex)
        {{
            Debugger.Break();
        }}
        finally
        {{
            Marshal.FreeHGlobal(ptr);
        }}

        return str;
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
        using (FileStream fsNet = OpenFile(commNetworkPath))
        {
            using(StreamWriter sw = new StreamWriter(fsNet))
            {
                sw.Write(NetMgrCSContent);
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
    Dictionary<string, {TypeOfHdrMsgType}> MessageIDs = new Dictionary<string, {TypeOfHdrMsgType}>()
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
            parmeters += type + " " + jpdMessage.Param[i].Name;
            sizeofParamters += "Marshal.SizeOf(" + jpdMessage.Param[i].Name + ")";
            if (i != jpdMessage.Param.Count - 1)
            {
                parmeters += ", ";
                sizeofParamters += " + ";
            }
        }
        string funcDef = $@"
    public void {jpdMessage.Message}({parmeters})
    {{
        stMSG_HDR hdr = new stMSG_HDR();
        hdr.Code = RPC.ValidCode;
        hdr.MsgLen = ({TypeOfHdrMsgLen})({sizeofParamters});
        hdr.MsgType = MessageIDs[""{jpdMessage.Message}""];

        byte[] bytes = new byte[Marshal.SizeOf(hdr) + hdr.MsgLen];

        byte[] bytesHdr = new byte[Marshal.SizeOf(hdr)];
        RPC.Network.MessageToBytes<stMSG_HDR>(hdr, bytesHdr);

        int offset = 0;
        Buffer.BlockCopy(bytesHdr, 0, bytes, offset, Marshal.SizeOf(hdr)); offset += Marshal.SizeOf(hdr);";

        foreach(var param in jpdMessage.Param)
        {
            string csharType = TranslateCSharpType(param.Type); 
            if(csharType == "Byte")
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
    }}
";

        return funcDef;
    }

    string TranslateCSharpType(string type)
    {
        string ret = string.Empty;
        switch (type)
        {
            case "BYTE":
                ret = "Byte";
                break;
            case "CHAR":
                ret = "Char";
                break;
            case "UINT16":
                ret = "UInt16";
                break;
            case "INT16":
                ret = "Int16";
                break;
            case "UINT32":
                ret = "UInt32";
                break;
            case "INT32":
                ret = "Int32";
                break;
            case "UINT64":
                ret = "UInt64";
                break;
            case "INT64":
                ret = "Int64";
                break;
            default:
                ret = type; 
                break;
        }

        return ret;
    }

    private bool Compile_HDR_ONLY()
    {

        return true;
    }

    private FileStream OpenFile(string path)
    {
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
    }
}
