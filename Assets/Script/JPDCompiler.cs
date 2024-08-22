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
        }

        DontDestroyOnLoad(go);
        s_Instance = go.GetComponent<JPDCompiler>();
    }

    private int m_CompileMode = 0;
    string m_ServerPath = string.Empty;
    string m_ClientPath = string.Empty;
    JPD_SCHEMA m_JPD = new JPD_SCHEMA();

    int     MsgHdrSize = 3;
    string  MsgLengthFieldType = "BYTE";
    string  MsgIdFieldType = "BYTE";
    int     MsgLengthFieldOffset = 1;
    int     MsgIdFieldOffset = 2;

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
                sw.WriteLine("); {");
                sw.WriteLine("\t\tuint32_t msgLen = " + sizeofStr(msg.Param));
                sw.WriteLine("\t\tstJNetSession* jnetSession = GetJNetSession(remote);");
                sw.WriteLine("\t\tif (jnetSession != nullptr) {");
                sw.WriteLine("\t\t\tJBuffer& buff = jnetSession->sendBuff;");
                sw.WriteLine("\t\t\tif (buff.GetFreeSize() >= msgLen) {");
                
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
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + "{");
        sw.WriteLine();
        sw.WriteLine("\tclass Stub : public JNetStub");
        sw.WriteLine("\t{");
        sw.WriteLine("\tpublic:");
        foreach(var msg in jpdNamespace.Defines)
        {
            string fdef = msg.Message + "(HostID remote";
            foreach(var param in msg.Param)
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

        sw.WriteLine();
        sw.WriteLine("\t\tRpcID* GetRpcList() override { return gRpcList; }");
        sw.WriteLine("\t\tint GetRpcListCount() override { return gRpcListCount; }");
        sw.WriteLine("\t\tbool ProcessReceivedMessage(HostID remote, JBuffer& jbuff) override;");
        sw.WriteLine("\t};");
        sw.WriteLine("}");
        sw.WriteLine();
    }

    private void MakeStubCpp_Server(StreamWriter sw, JPD_NAMESPACE jpdNamespace, string direction)
    {
        sw.WriteLine("namespace " + jpdNamespace.Namespace + "_" + direction + " {");
        sw.WriteLine();
        sw.WriteLine("\tbool Stub::ProcessReceivedMessage(HostID remote, JBuffer& jbuff) {");
        sw.WriteLine("\t\twhile(true) {");
        sw.WriteLine("\t\t\tif (jbuff.GetUseSize() < " + MsgHdrSize + ") {");
        sw.WriteLine(("\t\t\t\treturn false;"));
        sw.WriteLine("\t\t\t}");
        sw.WriteLine(("\t\t\t" + MsgLengthFieldType + " msgLen;"));
        sw.WriteLine(("\t\t\t" + MsgIdFieldType + " msgID;"));
        sw.WriteLine("\t\t\tjbuff.Peek(" + MsgLengthFieldOffset + ", reinterpret_cast<BYTE*>(&msgLen), sizeof(msgLen));");
        sw.WriteLine("\t\t\tjbuff.Peek(" + MsgIdFieldOffset + ", reinterpret_cast<BYTE*>(&msgID), sizeof(msgID));");
        sw.WriteLine("\t\t\tif (jbuff.GetUseSize() < " + MsgHdrSize + " + msgLen) {");
        sw.WriteLine("\t\t\t\treturn false;");
        sw.WriteLine("\t\t\t}");
        sw.WriteLine();
        sw.WriteLine("\t\t\tswitch(static_cast<RpcID>(msgID)) {");
        foreach(var msg in jpdNamespace.Defines)
        {
            if(msg.Dir == direction)
            {
                string fcall = msg.Message + "(remote";
                sw.WriteLine("\t\t\tcase RPC_" + msg.Message + ":");
                sw.WriteLine("\t\t\t{");
                foreach(var param in msg.Param)
                {
                    fcall += ", " + param.Name;
                    sw.WriteLine("\t\t\t\t" + param.Type + " " + param.Name + ";");
                    sw.WriteLine("\t\t\t\tjbuff >> " + param.Name + ";");
                }
                fcall += ")";
                sw.WriteLine("\t\t\t\t" + fcall + ";");
                sw.WriteLine("\t\t\t}");
                sw.WriteLine("\t\t\tbreak");
            }
            sw.WriteLine("\t\t\t}");
        }

        sw.WriteLine("\t\t}");
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
                sw.WriteLine("\tstatic const RpcID RPC_" + msg.Message + " = " + ID + i + ";");
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
        return true;
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
