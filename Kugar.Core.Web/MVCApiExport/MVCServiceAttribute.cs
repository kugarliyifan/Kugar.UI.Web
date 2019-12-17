using System;

namespace Kugar.Core.Web.MVCApiExport
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MVCServiceAttribute:Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MVCServiceNoExportAttribute : Attribute
    {
        
    }
}
