using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ShopifyLinkChecker {
    class Program {
        static string localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShopifyChecker");

        static string saveFile = Path.Combine(localAppData, "save.json");

        static void Main(string[] args) {
            Console.Write("Shopify home link (https://www.rootlink.com): ");
            string url = Console.ReadLine();

            Console.Write("How often to check the page in seconds: ");
            double timeToCheckSeconds = double.Parse(Console.ReadLine());

            Console.Write("What is the variant name of the product (not the product name itself): ");
            string productVariantName = Console.ReadLine().ToLower();

            Console.Write("Open browser to this variant item in cart when found (chrome, firefox, opera, or no): ");
            string browser = Console.ReadLine().ToLower();

            Console.Write("Verbosity of program per [iteration, hour, none]: ");
            string verbosity = Console.ReadLine().ToLower();

            

            List<string> oldFiles = (List<string>)Directory.EnumerateFiles(localAppData);
            for(int i = 0; i < oldFiles.Count;  i++)
            {
                File.Delete(oldFiles[i]);
            }
            LinkChecker(url, timeToCheckSeconds, productVariantName, verbosity, browser).GetAwaiter().GetResult();

        }
        static async Task LinkChecker(string url, double timeDelay, string productVariantName, string verbosity, string browser) {
            
            using var client = new WebClient();
            JProperty productsJson;
            string currentJsonString;
            // title string
            // id long
            // handle string
            // created_at DateTime
            double totalTime = 0;
            int hour = 0;
            int nothing = 0;
            do {
                
                try {
                    client.Headers.Add("User-Agent: Other");
                    client.DownloadFile(url + "/products.json", saveFile);
                
                    currentJsonString = File.ReadAllText(saveFile);
                    productsJson = (JProperty)JObject.Parse(currentJsonString).First;

                    foreach (var j in productsJson.First) {
                        if (!File.Exists(Path.Combine(localAppData, $"{(long)j.First.First}.json"))) {


                            string jString = j.ToString();
                            jString = string.Join(Environment.NewLine, jString.Split(Environment.NewLine).Where(x => !x.Contains("body_html")));

                            File.WriteAllText(Path.Combine(localAppData, $"{(long)j.First.First}.json"), jString);


                            string send = $"\n{(long)j.First.First,-14} | {j.Value<string>("title"),-27} | {j.Value<string>("handle"), -26} | {j.Value<DateTime>("created_at")}\n";

                            Console.Write(send);
                            
                            if (browser != "no" && j["variants"].First["title"].ToString().ToLower() == productVariantName) {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() {
                                    Arguments = $"/c start {browser} {url}/cart/{j["variants"].First.First.First}:1",
                                    CreateNoWindow = false,
                                    FileName = "CMD.exe"
                                });
                            }
                        }
                    }
                    if (verbosity == "iteration" && totalTime == 0) {
                        Console.Write($"| ");
                    }
                    totalTime += timeDelay;

                    if(verbosity == "iteration") Console.Write($"{++nothing} ");
                    if (verbosity == "hour" && totalTime >= 3600) {
                        totalTime = 0;
                        nothing = 0;
                        Console.Write($"|");
                        Console.WriteLine($"--Hour {++hour}--");
                    }
                    
                } catch (Exception ex) { Console.WriteLine(ex.Message); }
                
                await Task.Delay((int)(timeDelay * 1000));
            } while (true);
        }
    }
}
