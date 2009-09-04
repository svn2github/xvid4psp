/// 
/// Class to help with assembly information programmatically.
/// 

using System.Reflection;

public class AssemblyInfoHelper
{
    private System.Type myType;

    /// 
    /// Default Constructor
    /// 
    public AssemblyInfoHelper()
    {
        myType = typeof(AssemblyInfoHelper);
    }

    /// 
    /// Unencrypted name of the assembly.
    /// 
    public string AsmName
    {
        get
        {
            return myType.Assembly.GetName().Name.ToString();
        }
    }

    /// 
    /// Full, or display name of the assembly.
    /// 
    public string AsmFQName
    {
        get
        {
            return myType.Assembly.GetName().FullName.ToString();
        }
    }
    /// 
    /// Gets the location of the assembly as specified originally, for example, in an System.Reflection.AssemblyName object.
    /// 
    public string CodeBase
    {
        get
        {
            return myType.Assembly.CodeBase;
        }
    }
    /// 
    /// Copyright information for this assembly.
    /// 
    public string Copyright
    {
        get
        {
            System.Type at = typeof(AssemblyCopyrightAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyCopyrightAttribute ct = (AssemblyCopyrightAttribute)r[0];
            return ct.Copyright;
        }
    }

    /// 
    /// Company Name for this assembly.
    /// 
    public string Company
    {
        get
        {
            System.Type at = typeof(AssemblyCompanyAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyCompanyAttribute ct = (AssemblyCompanyAttribute)r[0];
            return ct.Company;
        }
    }

    /// 
    /// Gets the major, minor, revision and build numbers of the assembly.
    /// 
    public string Version
    {
        get
        {
            return myType.Assembly.GetName().Version.ToString();
        }
    }


    /// 
    /// Gets the assembly Title information.
    /// 
    public string Title
    {
        get
        {
            System.Type at = typeof(AssemblyTitleAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyTitleAttribute ct = (AssemblyTitleAttribute)r[0];
            return ct.Title;
        }
    }
    /// 
    /// Gets the assembly Product information.
    /// 
    public string Product
    {
        get
        {
            System.Type at = typeof(AssemblyProductAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyProductAttribute ct = (AssemblyProductAttribute)r[0];
            return ct.Product;
        }
    }
    /// 
    /// Gets the assembly Description information.
    /// 
    public string Description
    {
        get
        {
            System.Type at = typeof(AssemblyDescriptionAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyDescriptionAttribute ct = (AssemblyDescriptionAttribute)r[0];
            return ct.Description;
        }
    }

    /// 
    /// Gets the trademark information for the assembly.
    /// 
    public string Trademark
    {
        get
        {
            System.Type at = typeof(AssemblyTrademarkAttribute);
            object[] r = myType.Assembly.GetCustomAttributes(at, false);
            AssemblyTrademarkAttribute ct = (AssemblyTrademarkAttribute)r[0];
            return ct.Trademark;
        }
    }

}