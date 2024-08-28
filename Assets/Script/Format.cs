
using JetBrains.Annotations;
using System;
using System.Collections.Generic;

[Serializable]
public class JPD_PARAM
{
    public string Type;
    public string Name;
    public string FixedLenOfArray;

    public JPD_PARAM()
    {
        FixedLenOfArray = string.Empty;
    }
}

[Serializable]
public class JPD_MESSAGE
{
    public string Message;
    public string ID;
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
public class JPD_ENUM
{
    public string Name;
    public List<string> Fields;
    public JPD_ENUM(string name) { Name = name; Fields = new List<string>(); }
}

[Serializable]
public class JPD_CONST
{
    public string Type;
    public string Name; 
    public string Value;
}

[Serializable]
public class JPD_CONST_GROUP
{
    public string Name;
    public List<JPD_CONST> Consts;

    public JPD_CONST_GROUP(string name) { Name = name; Consts = new List<JPD_CONST>(); }
}

[Serializable]
public class JPD_SCHEMA
{
    public string SERVER_COMPILE_MODE;
    public string SERVER_OUTPUT_DIR;
    public string CLIENT_COMPILE_MODE;
    public string CLIENT_OUTPUT_DIR;

    public List<JPD_ENUM> JPD_Enums;
    public List<JPD_CONST_GROUP> JPD_ConstGroups;
    public List<JPD_NAMESPACE> JPD_Namespaces;    

    public JPD_SCHEMA()
    {
        JPD_Enums = new List<JPD_ENUM>(); 
        JPD_ConstGroups = new List<JPD_CONST_GROUP>();   
        JPD_Namespaces = new List<JPD_NAMESPACE>();
    }
}