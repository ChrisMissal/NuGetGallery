﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NuGetGallery.Views.Packages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Mvc.Ajax;
    using System.Web.Mvc.Html;
    using System.Web.Routing;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using Microsoft.Web.Helpers;
    using NuGetGallery;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "1.2.0.0")]
    [System.Web.WebPages.PageVirtualPathAttribute("~/Views/Packages/Edit.cshtml")]
    public class Edit : System.Web.Mvc.WebViewPage<DisplayPackageViewModel>
    {
        public Edit()
        {
        }
        public override void Execute()
        {


            
            #line 2 "..\..\Views\Packages\Edit.cshtml"
  
    ViewBag.Tab = "Packages";


            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n<h1 class=\"page-heading\">Edit ");


            
            #line 7 "..\..\Views\Packages\Edit.cshtml"
                         Write(Model.Title);

            
            #line default
            #line hidden
WriteLiteral(" Package</h1>\r\n<p class=\"message\">\r\n    You may only edit certain package propert" +
"ies listed below. To modify or update any other package properties, <a href=\"");


            
            #line 9 "..\..\Views\Packages\Edit.cshtml"
                                                                                                                     Write(Url.UploadPackage());

            
            #line default
            #line hidden
WriteLiteral("\">reupload the package</a>.\r\n</p>\r\n");


            
            #line 11 "..\..\Views\Packages\Edit.cshtml"
 using (Html.BeginForm()) {

            
            #line default
            #line hidden
WriteLiteral("    <fieldset class=\"form\">\r\n        <legend>Edit ");


            
            #line 13 "..\..\Views\Packages\Edit.cshtml"
                Write(Model.Title);

            
            #line default
            #line hidden
WriteLiteral(" Package</legend>\r\n        ");


            
            #line 14 "..\..\Views\Packages\Edit.cshtml"
   Write(Html.AntiForgeryToken());

            
            #line default
            #line hidden
WriteLiteral("\r\n        <div class=\"form-field\">\r\n            ");


            
            #line 16 "..\..\Views\Packages\Edit.cshtml"
       Write(Html.EditorFor(package => package.Listed));

            
            #line default
            #line hidden
WriteLiteral("\r\n            <label for=\"Listed\" class=\"checkbox\">\r\n                List this pa" +
"ckage in search results.\r\n            </label>\r\n        </div>\r\n        <input t" +
"ype=\"submit\" value=\"Save\" title=\"Save Changes\" />\r\n        <a class=\"cancel\" hre" +
"f=\"");


            
            #line 22 "..\..\Views\Packages\Edit.cshtml"
                           Write(Url.Action("DisplayPackage"));

            
            #line default
            #line hidden
WriteLiteral("\" title=\"Cancel Changes and go back to package page.\">Cancel</a>\r\n    </fieldset>" +
"\r\n");


            
            #line 24 "..\..\Views\Packages\Edit.cshtml"
}
            
            #line default
            #line hidden

        }
    }
}
#pragma warning restore 1591