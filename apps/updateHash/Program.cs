using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using updateHash;

namespace updateHash
{
    class Program
    {
        static private string Base;
        static private string appName;
        static private string coDeploymentManifest;

        static void Main(string[] args)
        {
            
            if (args.Length == 0)
            {
                Console.WriteLine("Error: No deployment manifest specified.");
                Usage();
            }
            else
            {
                coDeploymentManifest = args[0];
                if (File.Exists(coDeploymentManifest))
                {
                    ManifestHash manifest = new ManifestHash(coDeploymentManifest);
                    manifest.updateCOApplication(coDeploymentManifest);
                }
                else 
                {
                    Console.WriteLine("File " + coDeploymentManifest + " not found.");
                }

            }
            
        }

        static void Usage()
        {
            Console.WriteLine("updateHash path-to-DeployementManifest");
        }
    }
}
