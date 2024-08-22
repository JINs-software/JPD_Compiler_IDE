
using System;
using System.Collections.Generic;

[Serializable]
public class JPD_PARAM
{
    public string Type;
    public string Name;
}

[Serializable]
public class JPD_MESSAGE
{
    public string Message;
    public string Dir;
    public List<JPD_PARAM> Param;

    public JPD_MESSAGE()
    {
        Param = new List<JPD_PARAM> ();
    }
}

[Serializable]
public class JPD_NAMESPACE
{
    public string Namespace;
    public string ID;
    public List<JPD_MESSAGE> Defines;

    public JPD_NAMESPACE(string namespaceName, string id)
    {
        Namespace = namespaceName;  
        ID = id;    
        Defines = new List<JPD_MESSAGE>();   
    }
}

[Serializable]
public class JPD_SCHEMA
{
    public string COMPILE_MODE;
    public string SERVER_OUTPUT_DIR;
    public string CLIENT_OUTPUT_DIR;
    public List<JPD_NAMESPACE> JPD;    

    public JPD_SCHEMA()
    {
        JPD = new List<JPD_NAMESPACE>();
    }
}