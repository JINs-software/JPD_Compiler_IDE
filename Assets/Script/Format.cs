
using System.Collections.Generic;

public class JPD_PARAM
{
    public string type;
    public string name;
}

public class JPD_DEFINE
{
    public string Name;
    public string Dir;
    public List<JPD_PARAM> Param;

    public JPD_DEFINE()
    {
        Param = new List<JPD_PARAM> ();
    }
}

public class JPD_ITEM
{
    public string Namespace;
    public string ID;
    public List<JPD_DEFINE> Defines;

    public JPD_ITEM(string namespaceName, string id)
    {
        Namespace = namespaceName;  
        ID = id;    
        Defines = new List<JPD_DEFINE>();   
    }
}

public class JPD
{
    public List<JPD_ITEM> JpdItems;    

    public JPD()
    {
        JpdItems = new List<JPD_ITEM>();
    }
}