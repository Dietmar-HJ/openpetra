//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2010 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using DDW;
using DDW.Collections;
using NamespaceHierarchy;
using Ict.Common;
using Ict.Tools.CodeGeneration;

namespace GenerateSharedCode
{
/// <summary>
/// create the interfaces for the shared code
/// </summary>
public class CreateInterfaces : AutoGenerationWriter
{
    /// <summary>
    /// this will return a SortedList, the key is the interface name,
    /// and the value is the class with the full namespace that implements that interface
    /// </summary>
    /// <param name="ACSFiles"></param>
    /// <returns></returns>
    private SortedList GetInterfaceNamesFromImplementation(List <CSParser>ACSFiles)
    {
        SortedList Result = new SortedList();

        foreach (CSParser CSFile in ACSFiles)
        {
            foreach (ClassNode t in CSFile.GetClasses())
            {
                foreach (IType ti in t.BaseClasses)
                {
                    if (CSParser.GetName(ti).StartsWith("I"))
                    {
                        Result.Add(CSParser.GetName(ti), CSFile.GetFullClassNameWithNamespace(t));
                    }
                }
            }
        }

        return Result;
    }

    /// <summary>
    /// return all classes in a given namespace that implement the interface
    /// </summary>
    /// <param name="ACSFiles"></param>
    /// <param name="AInterfaceName"></param>
    /// <param name="ANamespace">namespace name on the server</param>
    /// <returns></returns>
    private List <ClassNode>GetClassesThatImplementInterface(
        List <CSParser>ACSFiles, String AInterfaceName, String ANamespace)
    {
        List <ClassNode>ClassList = new List <ClassNode>();

        // find all classes in that server namespace, eg. Ict.Petra.Server.MPartner.Extracts.UIConnectors
        List <ClassNode>ConnectorClasses = CSParser.GetClassesInNamespace(ACSFiles, ANamespace);

        foreach (ClassNode t in ConnectorClasses)
        {
            if ((t.BaseClasses.Count == 0) && ANamespace.EndsWith("WebConnectors"))
            {
                ClassList.Add(t);
                break;
            }

            foreach (IType ti in t.BaseClasses)
            {
                if (CSParser.GetName(ti) == AInterfaceName)
                {
                    ClassList.Add(t);
                    break;
                }
            }
        }

        return ClassList;
    }

    /// <summary>
    /// reduce the length of the type name if the namespace is unique.
    /// </summary>
    /// <param name="ATypeName"></param>
    /// <param name="ANamespace"></param>
    /// <returns></returns>
    private string ShortenTypeName(string ATypeName, string ANamespace)
    {
        if (ATypeName == "System.Void")
        {
            ATypeName = "void";
        }

        // ReturnType sometimes has a very long name;
        // shorten it for readability
        // test whether the namespace is contained in the current namespace
        if (ATypeName.IndexOf(".") != -1)
        {
            String returnTypeNS = ATypeName.Substring(0, ATypeName.LastIndexOf("."));

            if (ANamespace.IndexOf(returnTypeNS) == 0)
            {
                ATypeName = ATypeName.Substring(ATypeName.LastIndexOf(".") + 1);
            }
        }

        return ATypeName;
    }

    /// <summary>
    /// write the interfaces for the methods that need to be reflected
    /// check connector files
    /// </summary>
    /// <param name="AMethodsAlreadyWritten">write methods only once</param>
    /// <param name="AConnectorClasses">the classes that are implementing the methods</param>
    /// <param name="AInterfaceName">the interface that is written at the moment</param>
    /// <param name="AInterfaceNamespace">only needed to shorten the type names to improve readability</param>
    /// <param name="AServerNamespace">for the comment in the autogenerated code</param>
    /// <returns></returns>
    private bool WriteConnectorMethods(
        ref StringCollection AMethodsAlreadyWritten,
        List <ClassNode>AConnectorClasses, String AInterfaceName, String AInterfaceNamespace, String AServerNamespace)
    {
        foreach (ClassNode t in AConnectorClasses)
        {
            string ConnectorClassName = CSParser.GetName(t.Name);

            foreach (PropertyNode p in CSParser.GetProperties(t))
            {
                // don't write namespace hierarchy here
                if ((CSParser.GetName(p.Type).IndexOf("Namespace") == -1)
                    && ((p.Modifiers & Modifier.Public) != 0)
                    && !p.DocComment.Contains("[NO-REMOTING]"))
                {
                    String returnType = ShortenTypeName(CSParser.GetName(p.Type), AInterfaceNamespace);

                    // this interface got implemented somewhere on the server
                    WriteLine("/// <summary>auto generated from Connector property (" + AServerNamespace + "." + ConnectorClassName + ")</summary>");
                    StartBlock(returnType + " " + CSParser.GetName(p.Names));

                    if (p.Getter != null)
                    {
                        WriteLine("get;");
                    }

                    if (p.Setter != null)
                    {
                        WriteLine("set;");
                    }

                    EndBlock();
                }
            }

            foreach (MethodNode m in CSParser.GetMethods(t))
            {
                string MethodName = CSParser.GetName(m.Names);

                if (MethodName.Equals("InitializeLifetimeService")
                    || MethodName.Equals("GetLifetimeService")
                    || MethodName.Equals("CreateObjRef")
                    || MethodName.Equals("GetType")
                    || MethodName.Equals("ToString")
                    || MethodName.Equals("Equals")
                    || MethodName.Equals("GetHashCode")
                    || ((m.Modifiers & Modifier.Public) == 0)
                    || m.DocComment.Contains("[NO-REMOTING]"))
                {
                    continue;
                }

                String formattedMethod = "";
                String returnType = ShortenTypeName(CSParser.GetName(m.Type), AInterfaceNamespace);

                int align = (returnType + " " + CSParser.GetName(m.Names)).Length + 1;

                // this interface got implemented somewhere on the server
                formattedMethod = "/// <summary> auto generated from Connector method(" + AServerNamespace + "." + ConnectorClassName +
                                  ")</summary>" + Environment.NewLine;
                formattedMethod += returnType + " " + CSParser.GetName(m.Names) + "(";

                bool firstParameter = true;

                foreach (ParamDeclNode p in m.Params)
                {
                    if (!firstParameter)
                    {
                        WriteLine(formattedMethod + ",");
                        formattedMethod = new String(' ', align);
                    }

                    firstParameter = false;
                    String parameterType = CSParser.GetName(p.Type);

                    if ((p.Modifiers & Modifier.Ref) != 0)
                    {
                        formattedMethod += "ref ";
                    }
                    else if ((p.Modifiers & Modifier.Out) != 0)
                    {
                        formattedMethod += "out ";
                    }

                    formattedMethod += parameterType + " " + p.Name;
                }

                formattedMethod += ");";
                AMethodsAlreadyWritten.Add(MethodName);
                WriteLine(formattedMethod);
            }
        }

        return true;
    }

    /// <summary>
    /// write the interfaces for the Connectors
    /// parses the connector files
    /// </summary>
    /// <param name="AMethodsAlreadyWritten">a string list of method names that have been written</param>
    /// <param name="AConnectorClasses">all connector classes that implement the interface</param>
    /// <param name="AInterfaceName">the name of the interface we are writing to; it ends with ConnectorsNamespace</param>
    /// <param name="AInterfaceNamespace"></param>
    /// <param name="AServerNamespace">for the comment in the autogenerated code</param>
    /// <returns></returns>
    private bool WriteConnectorConstructors(
        ref StringCollection AMethodsAlreadyWritten,
        List <ClassNode>AConnectorClasses, String AInterfaceName, String AInterfaceNamespace, String AServerNamespace)
    {
        string ServerNamespace = AInterfaceNamespace.Replace("Ict.Petra.Shared.Interfaces", "Ict.Petra.Server");

        foreach (ClassNode t in AConnectorClasses)
        {
            string ConnectorClassName = CSParser.GetName(t.Name);
            MethodNode GetDataMethod = null;

            foreach (MethodNode m in t.Methods)
            {
                if (CSParser.GetName(m.Names) == "GetData")
                {
                    // there might be overloads of GetData; use the method with the most parameters
                    if ((GetDataMethod == null) || (GetDataMethod.Params.Count < m.Params.Count))
                    {
                        GetDataMethod = m;
                    }
                }
            }

            // cacheable will not work yet; careful: when building MethodName, connectorName does not have a plural s
            // but that cacheable constructor is not needed anyways???

            if (ConnectorClassName.StartsWith("T")
                && (ConnectorClassName.EndsWith("UIConnector")
                    || ConnectorClassName.EndsWith("LogicConnector")
                    || ConnectorClassName.EndsWith("Lookup")
                    || ConnectorClassName.EndsWith("Reader")))
            {
                // create a method for the interface, that returns the interface that the connector class is derived from,
                // and takes the parameters from the constructor of the connector class
                // it is named: remove first character T and trailing UIConnector/LogicConnector from the name of the connector class

                // get the connector name from the namespace
                StringCollection namespaceList = StringHelper.StrSplit(ServerNamespace, ".");

                // connectorName eg. UIConnectors, LogicConnectors, ServerLookups, Cacheable
                string connectorName = namespaceList[namespaceList.Count - 1];
                string MethodName = ConnectorClassName.Substring(1,
                    ConnectorClassName.Length - 1 - (connectorName.Length - 1));
                string MethodType = "";

                foreach (IType ti in t.BaseClasses)
                {
                    // problem, eg. in MCommon, TOfficeSpecificDataLabelsUIConnector implements 2 interfaces
                    if ((CSParser.GetName(ti) != "TConfigurableMBRObject")
                        && (CSParser.GetName(ti) == AInterfaceName))
                    {
                        MethodType = CSParser.GetName(ti);
                    }
                }

                if (MethodType.Length == 0)
                {
                    // no valid interface has been derived; eg. Hospitality not finished yet
                    continue;
                }

                if (t.Constructors.Count == 0)
                {
                    // will cause compile error if the constructor is missing, because it is not implementing the interface completely
                    throw new Exception("missing a connector constructor in " + ServerNamespace + "." + ConnectorClassName);

//                    WriteLine("/// <summary>auto generated - there was no Connector constructor (" + ServerNamespace + "." + ConnectorClassName + ")</summary>");
//                    AMethodsAlreadyWritten.Add(MethodName);
//                    string MethodDeclaration = MethodType + " " + MethodName + "();";
//                    WriteLine(MethodDeclaration);
                }
                else
                {
                    // find constructor and copy the parameters
                    foreach (ConstructorNode m in t.Constructors)
                    {
                        WriteLine(
                            "/// <summary>auto generated from Connector constructor (" + ServerNamespace + "." + ConnectorClassName + ")</summary>");
                        AMethodsAlreadyWritten.Add(MethodName);
                        string MethodDeclaration = MethodType + " " + MethodName + "(";
                        int align = MethodDeclaration.Length;
                        bool firstParameter = true;

                        foreach (ParamDeclNode p in m.Params)
                        {
                            AddParameter(ref MethodDeclaration, ref firstParameter, align, p.Name, p.Modifiers, p.Type);
                        }

                        MethodDeclaration += ");";
                        WriteLine(MethodDeclaration);

                        if (GetDataMethod != null)
                        {
                            WriteLine(
                                "/// <summary>auto generated from Connector constructor and GetData (" + ServerNamespace + "." + ConnectorClassName +
                                ")</summary>");
                            MethodDeclaration = MethodType + " " + MethodName + "(";
                            align = MethodDeclaration.Length;
                            firstParameter = true;

                            // first add the parameters of the constructor
                            foreach (ParamDeclNode p in m.Params)
                            {
                                AddParameter(ref MethodDeclaration, ref firstParameter, align, p.Name, p.Modifiers, p.Type);
                            }

                            // then add the return value of GetData as a ref parameter
                            AddParameter(ref MethodDeclaration, ref firstParameter, align, "ADataSet", Modifier.Ref, GetDataMethod.Type);

                            // then add the parameters of GetData, if they have not been added yet with the parameters of the constructor
                            foreach (ParamDeclNode p in GetDataMethod.Params)
                            {
                                AddParameter(ref MethodDeclaration, ref firstParameter, align, p.Name, p.Modifiers, p.Type);
                            }

                            MethodDeclaration += ");";
                            WriteLine(MethodDeclaration);
                        }
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// write the interfaces for the Connectors
    /// parses the instantiator files
    /// Although the instantiators are generated from the interfaces, they might contain ManualCode regions with specific functions
    /// </summary>
    /// <param name="AMethodsAlreadyWritten"></param>
    /// <param name="AInstantiatorClasses">all instantiator classes that implement the interface</param>
    /// <param name="AInterfaceName"></param>
    /// <param name="AInterfaceNamespace"></param>
    /// <param name="AServerNamespace">for the comment in the autogenerated code</param>
    /// <returns></returns>
    private bool WriteInstantiatorMethods(
        StringCollection AMethodsAlreadyWritten,
        List <ClassNode>AInstantiatorClasses, String AInterfaceName, String AInterfaceNamespace, String AServerNamespace)
    {
        foreach (ClassNode t in AInstantiatorClasses)
        {
            // there should only be one class, eg. TSubscriptionsCacheableNamespace
            foreach (MethodNode m in t.Methods)
            {
                // copy all public methods that are not constructor, destructor, or InitializeLifetimeService
                // and that are not created yet
                if ((CSParser.GetName(m.Names) != "InitializeLifetimeService")
                    && ((m.Modifiers & Modifier.Public) != 0)
                    && !AMethodsAlreadyWritten.Contains(CSParser.GetName(m.Names)))
                {
                    WriteLine("/// <summary>auto generated from Instantiator (" + AServerNamespace + "." + CSParser.GetName(t.Name) + ")</summary>");
                    string MethodDeclaration = CSParser.GetName(m.Type) + " " + CSParser.GetName(m.Names) + "(";
                    int align = MethodDeclaration.Length;

                    bool firstParameter = true;

                    foreach (ParamDeclNode p in m.Params)
                    {
                        AddParameter(ref MethodDeclaration, ref firstParameter, align, p.Name, p.Modifiers, p.Type);
                    }

                    MethodDeclaration += ");";
                    WriteLine(MethodDeclaration);
                }
            }
        }

        return true;
    }

    void WriteInterface(String ParentNamespace,
        String ParentInterfaceName,
        string AInterfaceName,
        TNamespace tn,
        TNamespace sn,
        List <TNamespace>children,
        SortedList InterfaceNames,
        List <CSParser>ACSFiles)
    {
        WriteLine("/// <summary>auto generated</summary>");
        StartBlock("public interface " + AInterfaceName + " : IInterface");

        foreach (TNamespace child in children)
        {
            WriteLine("/// <summary>access to sub namespace</summary>");
            StartBlock("I" + ParentInterfaceName + child.Name + "Namespace " + child.Name);
            WriteLine("get;");
            EndBlock();
        }

        //this should return the Connector classes; the instantiator classes are in a different namespace
        string ServerConnectorNamespace = ParentNamespace.Replace("Ict.Petra.Shared.Interfaces", "Ict.Petra.Server");

        // don't write methods twice, once from Connector, and then again from Instantiator
        StringCollection MethodsAlreadyWritten = new StringCollection();

        StringCollection InterfacesInNamespace = GetInterfacesInNamespace(ParentNamespace, InterfaceNames);

        foreach (string ChildInterface in InterfacesInNamespace)
        {
            List <ClassNode>ConnectorClasses = GetClassesThatImplementInterface(
                ACSFiles,
                ChildInterface,
                ServerConnectorNamespace);

            if (AInterfaceName.EndsWith("Namespace"))
            {
                WriteConnectorConstructors(
                    ref MethodsAlreadyWritten,
                    ConnectorClasses,
                    ChildInterface,
                    ParentNamespace,
                    ServerConnectorNamespace);
            }
        }

        List <ClassNode>ConnectorClasses2 = GetClassesThatImplementInterface(
            ACSFiles,
            AInterfaceName,
            ServerConnectorNamespace);

        WriteConnectorMethods(
            ref MethodsAlreadyWritten,
            ConnectorClasses2,
            AInterfaceName,
            ParentNamespace,
            ServerConnectorNamespace);

        // this is for the instantiator classes
        StringCollection NamespaceSplit = StringHelper.StrSplit(ParentNamespace, ".");

        // eg convert Ict.Petra.Shared.Interfaces.MPartner.Extracts.UIConnectors to Ict.Petra.Server.MPartner.Instantiator.Extracts.UIConnectors
        NamespaceSplit[2] = "Server";
        NamespaceSplit[3] = NamespaceSplit[4];
        NamespaceSplit[4] = "Instantiator";
        string ServerInstantiatorNamespace = StringHelper.StrMerge(NamespaceSplit, ".");
        List <ClassNode>InstantiatorClasses = GetClassesThatImplementInterface(
            ACSFiles,
            AInterfaceName,
            ServerInstantiatorNamespace);

        WriteInstantiatorMethods(
            MethodsAlreadyWritten,
            InstantiatorClasses,
            AInterfaceName,
            ParentNamespace,
            ServerInstantiatorNamespace);

        EndBlock();
    }

    private StringCollection GetInterfacesInNamespace(string ANamespace, SortedList AInterfaceNames)
    {
        // get all the interfaces in the current namespace
        StringCollection InterfacesInNamespace = new StringCollection();

        foreach (String InterfaceName in AInterfaceNames.GetKeyList())
        {
            // see if the class that is implementing the interface is in the current namespace (considering the difference of Shared and Server)
            if ((AInterfaceNames[InterfaceName].ToString().Substring(0,
                     AInterfaceNames[InterfaceName].ToString().LastIndexOf(".")).Replace("Instantiator.", "")
                 == ANamespace.Replace("Ict.Petra.Shared.Interfaces", "Ict.Petra.Server"))

                /*&& (InterfaceName != "I" + ANamespace + "Namespace")*/)
            {
                InterfacesInNamespace.Add(InterfaceName);
            }
        }

        return InterfacesInNamespace;
    }

    /// <summary>
    /// write the namespace for an interface
    /// this includes all the interfaces in this namespace
    /// it calls itself recursively for sub namespaces
    /// </summary>
    /// <param name="ParentNamespace"></param>
    /// <param name="ParentInterfaceName"></param>
    /// <param name="tn"></param>
    /// <param name="sn"></param>
    /// <param name="children"></param>
    /// <param name="InterfaceNames"></param>
    /// <param name="ACSFiles"></param>
    private void WriteNamespace(String ParentNamespace,
        String ParentInterfaceName,
        TNamespace tn,
        TNamespace sn,
        List <TNamespace>children,
        SortedList InterfaceNames,
        List <CSParser>ACSFiles)
    {
        WriteLine();
        StartBlock("namespace " + ParentNamespace);

        WriteInterface(ParentNamespace,
            ParentInterfaceName,
            "I" + ParentInterfaceName + "Namespace",
            tn, sn, children, InterfaceNames, ACSFiles);

        StringCollection InterfacesInNamespace = GetInterfacesInNamespace(ParentNamespace, InterfaceNames);

        // has been written already; we want to keep the order of the interfaces this way
        InterfacesInNamespace.Remove("I" + ParentInterfaceName + "Namespace");

        foreach (String InterfaceName in InterfacesInNamespace)
        {
            WriteInterface(ParentNamespace,
                ParentInterfaceName,
                InterfaceName,
                tn, sn, children, InterfaceNames, ACSFiles);
        }

        EndBlock();

        foreach (TNamespace child in children)
        {
            WriteNamespace(ParentNamespace + "." + child.Name,
                ParentInterfaceName + child.Name,
                tn,
                child,
                child.Children,
                InterfaceNames,
                ACSFiles);
        }
    }

    //other interfaces, e.g. IPartnerUIConnectorsPartnerEdit
    // we don't know the interfaces that are implemented, so need to look for the base classes
    // we need to know all the source files that are part of the UIConnector dll
    private void WriteNamespaces(TNamespace tn, SortedList AInterfaceNames, List <CSParser>ACSFiles)
    {
        // top level namespace
        StartBlock("namespace " + "Ict.Petra.Shared.Interfaces.M" + tn.Name);
        WriteLine("/// <summary>auto generated</summary>");
        StartBlock("public interface IM" + tn.Name + "Namespace : IInterface");

        foreach (TNamespace sn in tn.Children)
        {
            WriteLine("/// <summary>access to sub namespace</summary>");
            StartBlock("I" + sn.Name + "Namespace " + sn.Name);
            WriteLine("get;");
            EndBlock();
        }

        EndBlock();
        EndBlock();

        // parse Instantiator source code
        foreach (TNamespace sn in tn.Children)
        {
            string currentNamespaceOneLevelUp = "Ict.Petra.Shared.Interfaces.M" + tn.Name + "." + sn.Name;
            WriteNamespace("Ict.Petra.Shared.Interfaces.M" + tn.Name + "." + sn.Name, sn.Name, tn, sn, sn.Children, AInterfaceNames, ACSFiles);
        }
    }

    /// <summary>
    /// use CSParser to parse the Server files
    /// </summary>
    /// <param name="tn"></param>
    /// <param name="AOutputPath"></param>
    /// <param name="AXmlFileName"></param>
    private void WriteInterfaces(TNamespace tn, String AOutputPath, String AXmlFileName)
    {
        String OutputFile = AOutputPath + Path.DirectorySeparatorChar + tn.Name + ".Interfaces.cs";

        // open file
        Console.WriteLine("working on file " + OutputFile);

        OpenFile(OutputFile);
        WriteLine("// Auto generated with nant generateGlue, based on " +
            Path.GetFullPath(AXmlFileName).Substring(Path.GetFullPath(AXmlFileName).IndexOf("csharp")));
        WriteLine("// and the Server c# files (eg. UIConnector implementations)");
        WriteLine("// Do not change this file manually.");
        WriteLine("//");

        // load default header with license and copyright
        string templateDir = TAppSettingsManager.GetValueStatic("TemplateDir");
        WriteLine(ProcessTemplate.LoadEmptyFileComment(templateDir));

        WriteLine("using System;");
        WriteLine("using System.Collections;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Data;");
        WriteLine("using Ict.Common;");
        WriteLine("using Ict.Common.Verification;");

        foreach (TNamespace sn in tn.Children)
        {
            WriteLine("using Ict.Petra.Shared.Interfaces.M" + tn.Name + "." + sn.Name + ';');
        }

        foreach (TNamespace sn in tn.Children)
        {
            CommonNamespace.WriteUsingNamespace(this, "Ict.Petra.Shared.Interfaces.M" + tn.Name + "." + sn.Name, sn.Name, sn, sn.Children);
        }

        // get all csharp files that might hold implementations of remotable classes
        List <CSParser>CSFiles = new List <CSParser>();

        if (Directory.Exists(CSParser.ICTPath + "/Petra/Server/lib/M" + tn.Name))
        {
            // any class in the module can contain a webconnector
            string[] filePaths = Directory.GetFiles(CSParser.ICTPath + "/Petra/Server/lib/M" + tn.Name, "*.csproj",
                SearchOption.AllDirectories);

            foreach (string filePath in filePaths)
            {
                // excluding the data directory
                if (!filePath.Replace("\\", "/").Contains("/data/"))
                {
                    CSParser.GetCSFilesInProject(filePath, ref CSFiles);
                }
            }
        }

        SortedList InterfaceNames = GetInterfaceNamesFromImplementation(CSFiles);

        WriteNamespaces(tn, InterfaceNames, CSFiles);

        Close();
    }

    /// <summary>
    /// main function to create the interface files
    /// </summary>
    /// <param name="ANamespaces"></param>
    /// <param name="AOutputPath"></param>
    /// <param name="AXmlFileName"></param>
    public void CreateFiles(List <TNamespace>ANamespaces, String AOutputPath, String AXmlFileName)
    {
        foreach (TNamespace tn in ANamespaces)
        {
            // for testing
//        if (tn.Name != "SysMan") continue;
            WriteInterfaces(tn, AOutputPath, AXmlFileName);
        }
    }
}
}