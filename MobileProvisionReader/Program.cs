using System;
using System.Xml;

namespace MobileProvisionReader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //Read mobileprovision contents
                MobileProvisionContents mpc = MobileProvisionContents.Read("D://Selfsigned.mobileprovision");

                //Dispaly some mobileprovision entries
                string uuid = mpc.GetUniqueId();
                Console.WriteLine("UUID {0}", uuid);

                string teamUniqueId;
                if (mpc.TryGetTeamUniqueId(out teamUniqueId))
                {
                    Console.WriteLine("TeamIdentifier: {0}", teamUniqueId);
                }
                else
                {
                    Console.WriteLine("TeamIdentifier: missing");
                }

                string bundleid = mpc.GetBundleIdentifier();
                Console.WriteLine("BundleIdentifier: {0}", bundleid);

            }
            catch (Exception e)
            {
                Console.WriteLine("Reading ERROR Exception: {0}", e.Message);
            }
        }
    }
}
