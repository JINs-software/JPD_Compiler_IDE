using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;

public static class GF
{
    public static string Indent(int cnt)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < cnt; i++) { sb.Append("\t"); }
        return sb.ToString();
    }
}


struct COMM_SERV_HDR
{
	struct COMM_RPC_ID
	{
		List<Tuple<string, int>> Messages;
		public string str(int indent = 0)
		{
			string str = "";
			foreach(Tuple<string, int> pair in Messages)
			{
				str += GF.Indent(indent) + $"static const RpcID RPC_{pair.Item1} = {pair.Item2};\n";
            }
			return str;
		}
    }

	string Namespace;
    COMM_RPC_ID Messages_S2C;
    COMM_RPC_ID Messages_C2S;

	string str()
	{
		string commHdr = $@"
#pragma once

namespace {Namespace}_S2C {{
{Messages_S2C.str()}
	extern RpcID gRpcList[];
	extern int gRpcListCount;
}}

namespace {Namespace}_C2S {{
{Messages_C2S.str()}
	extern RpcID gRpcList[];
	extern int gRpcListCount;
}}
";
		return string.Empty;
	}
}

struct COMM_SERV_CPP
{
    struct COMM_RPC
    {
        public List<string> Messages;
        public string str(int indent = 0)
        {
            string str = "";
            foreach (string msg in Messages)
            {
				str += GF.Indent(indent) + $"RPC_{msg},\n";
            }
            return str;
        }
    }
    string Namespace;
    COMM_RPC Messages_S2C;
    COMM_RPC Messages_C2S;

    string str()
    {
        string commHdr = $@"
#include ""Common_{Namespace}.h""

namespace {Namespace}_S2C {{
	RpcID gRpcList[] = {{
{Messages_S2C.str(2)}
	}};
	int gRpcListCount = {Messages_S2C.Messages.Count};
}}

namespace {Namespace}_C2S {{
	RpcID gRpcList[] = {{
		{Messages_C2S.str(2)}
	}};

	int gRpcListCount = {Messages_C2S.Messages.Count};
}}
";
        return commHdr;
    }
}

struct PROXY_SERV_HDR
{
    struct PROXY_MESSAGE
    {
		string Name;
        public List<Tuple<string, string>> Params;
        public string str(int indent = 0)
        {
            string str = GF.Indent(indent) + $"virtual bool {Name}(HostID remote";
			for(int i=0; i<Params.Count; i++)
			{
				str += $", {Params[i].Item1} {Params[i].Item2}";
			}
			str += ");";
            return str;
        }
    }

    string Namespace;
	List<PROXY_MESSAGE> Messages_S2C;
	List<PROXY_MESSAGE> Messages_C2S;

	string strDeclS2C()
	{
		string ret = "";
		foreach(PROXY_MESSAGE msg in Messages_S2C) { ret += msg.str(2); }
		return ret;
	}
    string strDeclC2S()
    {
        string ret = "";
        foreach (PROXY_MESSAGE msg in Messages_C2S) { ret += msg.str(2); }
        return ret;
    }

    string str()
    {
        string proxyHdr = $@"
#pragma once
#include ""Common_{Namespace}.h""

namespace {Namespace}_S2C {{

	class Proxy : public JNetProxy
	{{
	public: 
{strDeclS2C()}
		virtual RpcID* GetRpcList() override {{ return gRpcList; }}
		virtual int GetRpcListCount() override {{ return gRpcListCount; }}
	}};
}}

namespace {Namespace}_C2S {{

	class Proxy : public JNetProxy
	{{
	public: 
{strDeclC2S()}
		virtual RpcID* GetRpcList() override {{ return gRpcList; }}
		virtual int GetRpcListCount() override {{ return gRpcListCount; }}
	}};
}}
";
        return proxyHdr;
    }
}

struct PROXY_SERV_CPP
{
    struct PROXY_MESSAGE
    {
        string Name;
        public List<Tuple<string, string>> Params;

		public string strParams()
		{
			string ret = "HostID remote";
            foreach (Tuple<string, string> pair in Params)
            {
                ret += $", {pair.Item1} {pair.Item2}";
            }
			return ret;
        }
		public string strSizeOf()
		{
			string ret = "";
            for (int i = 0; i < Params.Count; i++)
            {
                ret += $"sizeof({Params[i].Item2})";
                if (i != Params.Count - 1) ret += " + ";
            }
			return ret;
        }
        public string str(int indent = 0)
        {
			string str = $@"
{GF.Indent(indent)}bool Proxy::ATTACK1({strParams()}) {{
{GF.Indent(indent)}	uint32_t msgLen = {strSizeOf()};
{GF.Indent(indent)}	stJNetSession* jnetSession = GetJNetSession(remote);
{GF.Indent(indent)}	if (jnetSession != nullptr) {{
{GF.Indent(indent)}		JBuffer& buff = jnetSession->sendBuff;
{GF.Indent(indent)}		if (buff.GetFreeSize() >= sizeof(stMSG_HDR) + msgLen) {{
{GF.Indent(indent)}			stMSG_HDR hdr;
{GF.Indent(indent)}			hdr.byCode = 0x89;
{GF.Indent(indent)}			hdr.bySize = {strSizeOf()};
{GF.Indent(indent)}			hdr.byType = RPC_{Name};
{GF.Indent(indent)}			buff << hdr;
";
			foreach(Tuple<string, string> param in Params){
				str += $@"
{GF.Indent(indent)}			buff << {param.Item2};";
			}
			str += $@"
{GF.Indent(indent)}		}}
{GF.Indent(indent)}		else {{
{GF.Indent(indent)}			return false;
{GF.Indent(indent)}		}}
{GF.Indent(indent)}	}}
{GF.Indent(indent)}	else {{
{GF.Indent(indent)}		return false;
{GF.Indent(indent)}	}}
{GF.Indent(indent)}	return true;
{GF.Indent(indent)}}}";
			return str;
        }
    }

    string Namespace;
    List<PROXY_MESSAGE> Messages_S2C;
    List<PROXY_MESSAGE> Messages_C2S;

    string strDecfineS2C()
    {
        string ret = "";
        foreach (PROXY_MESSAGE msg in Messages_S2C) { ret += msg.str(1); }
        return ret;
    }
    string strDefineC2S()
    {
        string ret = "";
        foreach (PROXY_MESSAGE msg in Messages_C2S) { ret += msg.str(1); }
        return ret;
    }

    string str()
    {
        string proxyCpp = $@"
#include ""Proxy_{Namespace}.h""

namespace {Namespace}_S2C {{
{strDecfineS2C()}
}}

namespace {Namespace}_C2S {{
{strDefineC2S()}
}}
";
        return proxyCpp;
    }
}