using System.Security.Cryptography;
using System.Reflection;
using System;
using System.Web.Services;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml.Schema;
using System.IO;
using System.Web.Services.Protocols;
using System.Net;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CSharp;

namespace DynamicWebService
{
    /// <summary>
    /// 动态调用
    /// </summary>
    public class WebServiceHelper
    {
        #region 动态调用WebService动态调用地址
        /// < summary>           
        /// 动态调用web服务         
        /// < /summary>          
        /// < param name="url">WSDL服务地址< /param> 
        /// < param name="methodname">方法名< /param>           
        /// < param name="args">参数< /param>           
        /// < returns>< /returns>          
        public static object InvokeWebService(string url, string methodname, object[] args)
        {
            return WebServiceHelper.InvokeWebService(url, null, methodname, args);
        }
        /// <summary>
        /// 动态调用web服务
        /// </summary>
        /// <param name="url">WSDL服务地址</param>
        /// <param name="classname">服务接口类名</param>
        /// <param name="methodname">方法名</param>
        /// <param name="args">参数值</param>
        /// <returns></returns>
        public static object InvokeWebService(string url, string classname, string methodname, object[] args)
        {

            string @namespace = "WebService.DynamicWebCalling";
            if ((classname == null) || (classname == ""))
            {
                classname = WebServiceHelper.GetWsClassName(url);
            }
            try
            {

                //获取WSDL   
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(url + "?WSDL");
                System.Web.Services.Description.ServiceDescription sd = System.Web.Services.Description.ServiceDescription.Read(stream);
                //注意classname一定要赋值获取 
                classname = sd.Services[0].Name;

                ServiceDescriptionImporter sdi = new ServiceDescriptionImporter();
                sdi.AddServiceDescription(sd, "", "");
                CodeNamespace cn = new CodeNamespace(@namespace);

                //生成客户端代理类代码          
                CodeCompileUnit ccu = new CodeCompileUnit();
                ccu.Namespaces.Add(cn);
                sdi.Import(cn, ccu);
                CSharpCodeProvider icc = new CSharpCodeProvider();


                //设定编译参数                 
                CompilerParameters cplist = new CompilerParameters();
                cplist.GenerateExecutable = false;
                cplist.GenerateInMemory = true;
                cplist.ReferencedAssemblies.Add("System.dll");
                cplist.ReferencedAssemblies.Add("System.XML.dll");
                cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
                cplist.ReferencedAssemblies.Add("System.Data.dll");
                //编译代理类                 
                CompilerResults cr = icc.CompileAssemblyFromDom(cplist, ccu);
                if (true == cr.Errors.HasErrors)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
                    {
                        sb.Append(ce.ToString());
                        sb.Append(System.Environment.NewLine);
                    }
                    throw new Exception(sb.ToString());
                }
                //生成代理实例，并调用方法                 
                System.Reflection.Assembly assembly = cr.CompiledAssembly;
                string newpath = System.Environment.CurrentDirectory + "\\" + getMd5Sum(url) + ".dll";
                //if (!File.Exists(newpath))
                //{
                //    File.Copy(cr.PathToAssembly, System.Environment.CurrentDirectory + "\\" + getMd5Sum(url) + ".dll");
                //}

                Type t = assembly.GetType(@namespace + "." + classname, true, true);
                object obj = Activator.CreateInstance(t);
                System.Reflection.MethodInfo mi = t.GetMethod(methodname);
                return mi.Invoke(obj, args);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException.Message, new Exception(ex.InnerException.StackTrace));
                // return "Error:WebService调用错误！" + ex.Message;
            }
        }
        /// <summary>                               
        /// 得到代理类类型名称                                 
        /// </summary>                                  
        private static void initTypeName(string _assPath)
        {
            Assembly serviceAsm = Assembly.LoadFrom(_assPath);
            Type[] types = serviceAsm.GetTypes();
            string objTypeName = "";
            foreach (Type t in types)
            {
                if (t.BaseType == typeof(SoapHttpClientProtocol))
                {
                    objTypeName = t.Name;
                    break;
                }
            }
            //  _typeName = serviceAsm.GetType(this._assName + "." + objTypeName);
        }
        private static string getMd5Sum(string str)
        {
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }
        private static string GetWsClassName(string wsUrl)
        {
            string[] parts = wsUrl.Split('/');
            string[] pps = parts[parts.Length - 1].Split('.');
            return pps[0];
        }
        #endregion
    }
    /// <summary>  
    /// WebServiceProxy 的摘要说明    
    /// 保存本地
    /// </summary>  
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。  
    // [System.Web.Script.Services.ScriptService]  
    public class WebServiceProxy : System.Web.Services.WebService
    {


        #region 私有变量和属性定义
        /// <summary>                   
        /// web服务地址                           
        /// </summary>                              
        private string _wsdlUrl = string.Empty;
        /// <summary>                   
        /// web服务名称                           
        /// </summary>                              
        private string _wsdlName = string.Empty;
        /// <summary>                   
        /// 代理类命名空间                           
        /// </summary>                              
        private string _wsdlNamespace = "DynamicWebServiceCalling.{0}";
        /// <summary>                   
        /// 代理类类型名称                           
        /// </summary>                              
        private Type _typeName = null;
        /// <summary>                   
        /// 程序集名称                             
        /// </summary>                              
        private string _assName = string.Empty;
        /// <summary>                   
        /// 代理类所在程序集路径                            
        /// </summary>                              
        private string _assPath = string.Empty;
        /// <summary>                   
        /// 代理类的实例                            
        /// </summary>                              
        private object _instance = null;
        /// <summary>                   
        /// 代理类的实例                            
        /// </summary>                              
        private object Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Activator.CreateInstance(_typeName);
                    return _instance;
                }
                else
                    return _instance;
            }
        }
        #endregion


        #region 构造函数
        public WebServiceProxy(string wsdlUrl)
        {
            this._wsdlUrl = wsdlUrl;
            string wsdlName = WebServiceProxy.getWsclassName(wsdlUrl);
            this._wsdlName = wsdlName;
            this._assName = string.Format(_wsdlNamespace, wsdlName);
            this._assPath = System.Environment.CurrentDirectory + "\\" + this._assName  + ".dll";


            //  MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, _assPath);
            if (wsdlUrl.Contains("?wsdl"))
            {
                this.CreateServiceAssembly();
            }
            else if (wsdlUrl.Contains(".dll"))
            {
                CreateDllAssembly();
            }
            else {
                if (!File.Exists(_assPath))
                {
                    this.CreateServiceAssembly();
                }
                CreateDllAssemblyPath(); }
        }


        #endregion


        #region 得到WSDL信息，生成本地代理类并编译为DLL，构造函数调用，类生成时加载
        /// <summary>                           
        /// 得到WSDL信息，生成本地代理类并编译为DLL                           
        /// </summary>                              
        private void CreateServiceAssembly()
        {
            if (this.checkCache())
            {
                this.initTypeName();
                return;
            }
            if (string.IsNullOrEmpty(this._wsdlUrl))
            {
                return;
            }
            try
            {
                //使用WebClient下载WSDL信息                         
                WebClient web = new WebClient();

                Stream stream = web.OpenRead(this._wsdlUrl);
                ServiceDescription description = ServiceDescription.Read(stream);//创建和格式化WSDL文档  
                ServiceDescriptionImporter importer = new ServiceDescriptionImporter();//创建客户端代理代理类  
                importer.ProtocolName = "Soap";
                importer.Style = ServiceDescriptionImportStyle.Client;  //生成客户端代理                         
                importer.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateNewAsync;
                importer.AddServiceDescription(description, null, null);//添加WSDL文档  
                                                                        //使用CodeDom编译客户端代理类                   


                //  MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "CodeNamespace");




                CodeNamespace nmspace = new CodeNamespace(_assName);    //为代理类添加命名空间                  
                CodeCompileUnit unit = new CodeCompileUnit();
                unit.Namespaces.Add(nmspace);
                this.checkForImports(this._wsdlUrl, importer);
                ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit);
                CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
                CompilerParameters parameter = new CompilerParameters();
                parameter.ReferencedAssemblies.Add("System.dll");
                parameter.ReferencedAssemblies.Add("System.XML.dll");
                parameter.ReferencedAssemblies.Add("System.Web.Services.dll");
                parameter.ReferencedAssemblies.Add("System.Data.dll");
                parameter.GenerateExecutable = false;
                parameter.GenerateInMemory = false;
                parameter.IncludeDebugInformation = false;
                CompilerResults result = provider.CompileAssemblyFromDom(parameter, unit);
                provider.Dispose();
                if (result.Errors.HasErrors)
                {
                    string errors = string.Format(@"编译错误:{0}错误！", result.Errors.Count);
                    foreach (CompilerError error in result.Errors)
                    {
                        errors += error.ErrorText;
                    }


                    //  MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, errors);
                }
                this.copyTempAssembly(result.PathToAssembly);
                this.initTypeName();
            }
            catch (Exception e)
            {
                //  MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, e.ToString());
            }
        }

        private void CreateDllAssemblyPath()
        {
            Assembly serviceAsm = Assembly.LoadFrom(this._assPath);
            _typeName = serviceAsm.GetType();// MISConfig.GetInstance().db.Database);
            this.initTypeName();
        }
        private void CreateDllAssembly()
        {
            Assembly serviceAsm = Assembly.LoadFrom(System.Environment.CurrentDirectory + "\\" + _wsdlUrl);
            _typeName = serviceAsm.GetType();// MISConfig.GetInstance().db.Database);
        }
        #endregion


        #region 执行Web服务方法
        /// <summary>                           
        /// 执行代理类指定方法，有返回值                                
        /// </summary>                                  
        /// <param   name="methodName">方法名称</param>                           
        /// <param   name="param">参数</param>                              
        /// <returns>object</returns>                                 
        public object ExecuteQuery(string methodName, object[] param)
        {
            object rtnObj = null;


            try
            {
                if (this._typeName == null)
                {
                    //记录Web服务访问类名错误日志代码位置 
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "服务访问类名【" + this._wsdlName + "】不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));


                    return null;
                }
                //调用方法  
                MethodInfo mi = this._typeName.GetMethod(methodName);
                if (mi == null)
                {
                    //记录Web服务方法名错误日志代码位置  
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "Web服务访问方法名【" + methodName + "】不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));


                }
                try
                {
                    if (param == null)
                        rtnObj = mi.Invoke(Instance, null);
                    else
                    {
                        rtnObj = mi.Invoke(Instance, param);
                    }
                }
                catch (TypeLoadException tle)
                {
                    //记录Web服务方法参数个数错误日志代码位置  
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "Web服务访问方法【" + methodName + "】参数个数不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));
                    //Log.GetInstance().WriteSYS(tle.ToString());
                }
            }
            catch (Exception ex)
            {
                //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, ex.ToString());
                //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));


            }
            return rtnObj;
        }


        /// <summary>                           
        /// 执行代理类指定方法，无返回值                                
        /// </summary>                                  
        /// <param   name="methodName">方法名称</param>                           
        /// <param   name="param">参数</param>                              
        public void ExecuteNoQuery(string methodName, object[] param)
        {
            try
            {
                if (this._typeName == null)
                {
                    //记录Web服务访问类名错误日志代码位置
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "Web服务访问类名【" + this._wsdlName + "】不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));
                    return;
                }
                //调用方法  
                MethodInfo mi = this._typeName.GetMethod(methodName);
                if (mi == null)
                {
                    //记录Web服务方法名错误日志代码位置  
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "Web服务访问方法名【" + methodName + "】不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));
                }
                try
                {
                    if (param == null)
                        mi.Invoke(Instance, null);
                    else
                        mi.Invoke(Instance, param);
                }
                catch (TypeLoadException tle)
                {
                    //记录Web服务方法参数个数错误日志代码位置  
                    //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, "Web服务访问方法【" + methodName + "】参数个数不正确，请检查！");
                    //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));
                    //Log.GetInstance().WriteSYS(tle.ToString());
                }
            }
            catch (Exception ex)
            {
                //MISLog.GetInstance().Write(MISLog.MSG_LEVEL.FLUSH, ex.ToString());
                //System.Windows.Forms.MessageBox.Show(MultiLanguage.GetString("MIS_Connect_FAIL"));
            }
        }
        #endregion


        #region 私有方法
        /// <summary>                               
        /// 得到代理类类型名称                                 
        /// </summary>                                  
        private void initTypeName()
        {
            Assembly serviceAsm = Assembly.LoadFrom(this._assPath);
            Type[] types = serviceAsm.GetTypes();
            string objTypeName = "";
            foreach (Type t in types)
            {
                if (t.BaseType == typeof(SoapHttpClientProtocol))
                {
                    objTypeName = t.Name;
                    break;
                }
            }
            _typeName = serviceAsm.GetType(this._assName + "." + objTypeName);
        }


        /// <summary>                       
        /// 根据web   service文档架构向代理类添加ServiceDescription和XmlSchema                             
        /// </summary>                                  
        /// <param   name="baseWSDLUrl">web服务地址</param>                           
        /// <param   name="importer">代理类</param>                              
        private void checkForImports(string baseWsdlUrl, ServiceDescriptionImporter importer)
        {
            DiscoveryClientProtocol dcp = new DiscoveryClientProtocol();
            dcp.DiscoverAny(baseWsdlUrl);
            dcp.ResolveAll();
            foreach (object osd in dcp.Documents.Values)
            {
                if (osd is ServiceDescription) importer.AddServiceDescription((ServiceDescription)osd, null, null); ;
                if (osd is XmlSchema) importer.Schemas.Add((XmlSchema)osd);
            }
        }


        /// <summary>                           
        /// 复制程序集到指定路径                                
        /// </summary>                                  
        /// <param   name="pathToAssembly">程序集路径</param>                              
        private void copyTempAssembly(string pathToAssembly)
        {
            File.Copy(pathToAssembly, this._assPath);
        }


        private string getMd5Sum(string str)
        {
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }


        /// <summary>                           
        /// 是否已经存在该程序集                                
        /// </summary>                                  
        /// <returns>false:不存在该程序集,true:已经存在该程序集</returns>                                
        private bool checkCache()
        {
            if (File.Exists(this._assPath))
            {
                return true;
            }
            return false;
        }


        //私有方法，默认取url入口的文件名为类名  
        private static string getWsclassName(string wsdlUrl)
        {
            string[] parts = wsdlUrl.Split('/');
            string[] pps = parts[parts.Length - 1].Split('.');
            return pps[0];
        }
        #endregion

     
    }
}
