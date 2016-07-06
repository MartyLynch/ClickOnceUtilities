using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Security.Cryptography;

namespace updateHash
{
    class ManifestHash
    {
        private string Base;

        public ManifestHash (string coApplication) {

            Base = coApplication.Substring(0, coApplication.LastIndexOf("/") + 1);
        }

        public void updateCOApplication(string deploymentManifest)
        {
            // update the Application manifest
            processApplicationManifest(deploymentManifest);

            // update the Deployment manifest hash in the application manifest
            processDeploymentManifest(deploymentManifest);
        }

        private void processApplicationManifest(string deploymentManifest)
        {
            List<string> applicationManifests;
            
            // Get location of application manifest
            applicationManifests = getApplicationManifestPaths(deploymentManifest);

            // Iterate through list of files
            foreach (string applicationManifest in applicationManifests)
            {
                processManifestEntries(applicationManifest);
            }
           
        }

        // Get a list of application manifests from the deployment manifest file.
        private List<string> getApplicationManifestPaths(string deploymentManifest) {
             List<string> manifests = new List<string>();
             XmlDocument appManifestXML;
             string manifestNodePath = "/asmv1:assembly/asmv2:dependency/asmv2:dependentAssembly";
             StringBuilder message;

             appManifestXML = new XmlDocument();
             if (File.Exists(deploymentManifest))
             {
                 appManifestXML.Load(deploymentManifest);
             }
             else
             {
                 message = new StringBuilder();
                 message.AppendFormat("File {0}  Not Found", deploymentManifest);
                 Log (message.ToString());
                 return manifests;
             }

             XmlNamespaceManager man = new XmlNamespaceManager(new NameTable());
             man.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
             man.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
             man.AddNamespace("dsig", "http://www.w3.org/2000/09/xmldsig#");

             XmlNodeList appManifestsNodes = appManifestXML.SelectNodes(manifestNodePath, man);
             foreach (XmlElement appManifestItem in appManifestsNodes)
             {
                 manifests.Add(appManifestItem.GetAttribute("codebase")); 
             }

             return (manifests);
        }

        private void processManifestEntries(string applicationManifest) {
            XmlDocument appManifestXML;
            string manifestNodePath = "/asmv1:assembly/asmv2:file";
            StringBuilder manifestFile = new StringBuilder();
            StringBuilder manifestDir = new StringBuilder();
            bool manifestUpdated;
            StringBuilder message;

            manifestFile.AppendFormat("{0}{1}", Base, applicationManifest);
            manifestDir.AppendFormat("{0}{1}", Base, applicationManifest.Substring(0, applicationManifest.LastIndexOf("\\")));
            appManifestXML = new XmlDocument();
            if (File.Exists(manifestFile.ToString()))
            {
                appManifestXML.Load(manifestFile.ToString());
            }
            else
            {
                 message = new StringBuilder();
                 message.AppendFormat("File {0} not Found", manifestFile);
                 Log (message.ToString());
                 return;
            }

            manifestUpdated = false;
            XmlNamespaceManager man = new XmlNamespaceManager(new NameTable());
            man.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            man.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
            man.AddNamespace("dsig", "http://www.w3.org/2000/09/xmldsig#");

            XmlNodeList appManifestsNodes = appManifestXML.SelectNodes(manifestNodePath, man);
            foreach (XmlElement appManifestItem in appManifestsNodes)
            {
                string file = appManifestItem.GetAttribute("name");
                StringBuilder applicationFile = new StringBuilder();
                applicationFile.AppendFormat("{0}\\{1}.deploy", manifestDir.ToString(), file);
                string fileSize = getFileSize(applicationFile.ToString());
                string hash = ComputeHash(applicationFile.ToString()); 
            
                if (appManifestItem.GetAttribute("size") != fileSize)
                {
                    // update size and hash attributes
                    appManifestItem.SetAttribute("size", fileSize);
                    message = new StringBuilder();
                    message.AppendFormat("Updated {0} entry {1}, size set to {2}", manifestFile, file, fileSize);
                    Log(message.ToString());
                    manifestUpdated = true;
                }

                XmlNode dsigDigestValueNode = appManifestItem.SelectSingleNode("asmv2:hash/dsig:DigestValue", man);
                if (dsigDigestValueNode.InnerXml != hash)
                {
                    // Set the computed hash value of the file. 
                    dsigDigestValueNode.InnerXml = hash;
                    message = new StringBuilder();
                    message.AppendFormat("Updated {0} entry {1}, computed hash set to {2}", manifestFile, file, hash);
                    Log(message.ToString());
                    manifestUpdated = true;
                }
            }
            if (manifestUpdated)
            {
                appManifestXML.Save(manifestFile.ToString());
            }        
        }

        private void Log(string message)
        {
            Console.WriteLine(message);
        }

        private string getFileSize(string file) {
            FileInfo fileInfo = new FileInfo(file);
            string fileSize = fileInfo.Length.ToString();
            return (fileSize);
        }

        private string ComputeHash(string filePath)
        {
            string filePathNormalized = System.IO.Path.GetFullPath(filePath);
            SHA1 sha = new SHA1Managed();
            FileStream fs = new FileStream(filePathNormalized, FileMode.Open, FileAccess.Read);
            byte[] byteHash = sha.ComputeHash(fs);
            fs.Close();
            return Convert.ToBase64String(byteHash, 0, byteHash.Length);
        }
  
        private void processDeploymentManifest(string deploymentManifest)
        {
            XmlDocument depManifestXML;
            string manifestNodePath = "/asmv1:assembly/asmv2:dependency/asmv2:dependentAssembly";
            bool manifestUpdated;
            StringBuilder message;

            depManifestXML = new XmlDocument();
            if (File.Exists(deploymentManifest))
            {
                depManifestXML.Load(deploymentManifest);
            }
            else
            {
                message = new StringBuilder();
                message.AppendFormat("File {0} not Found", deploymentManifest);
                Log (message.ToString());
                return;
            }

            manifestUpdated = false;
            XmlNamespaceManager man = new XmlNamespaceManager(new NameTable());
            man.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            man.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
            man.AddNamespace("dsig", "http://www.w3.org/2000/09/xmldsig#");

            XmlNodeList depManifestsNodes = depManifestXML.SelectNodes(manifestNodePath, man);
            foreach (XmlElement depManifestItem in depManifestsNodes)
            {
                StringBuilder manifestFile = new StringBuilder();
                manifestFile.AppendFormat ("{0}{1}", Base, depManifestItem.GetAttribute("codebase"));
                string fileSize = getFileSize(manifestFile.ToString());
                string hash = ComputeHash(manifestFile.ToString());

                if (depManifestItem.GetAttribute("size") != fileSize)
                {
                    // update size and hash attributes
                    depManifestItem.SetAttribute("size", fileSize);
                    message = new StringBuilder();
                    message.AppendFormat("Updated {0} entry {1}, size set to {2}", deploymentManifest, manifestFile, fileSize);
                    Log(message.ToString());
                    manifestUpdated = true;
                }

                XmlNode dsigDigestValueNode = depManifestItem.SelectSingleNode("asmv2:hash/dsig:DigestValue", man);
                if (dsigDigestValueNode.InnerXml != hash)
                {
                    // Set the computed hash value of the file.
                    dsigDigestValueNode.InnerXml = hash;
                    message = new StringBuilder();
                    message.AppendFormat("Updated {0} entry {1}, computed hash set to {2}", deploymentManifest, manifestFile, hash);
                    Log(message.ToString());
                    manifestUpdated = true;
                }
            }

            if (manifestUpdated)
            {
                depManifestXML.Save(deploymentManifest);
            }
              
        }
    }
}
