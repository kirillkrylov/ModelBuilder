﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ModelBuilder
{
    class Program
    {
        const string clioJson = @"\creatio\clio\appsettings.json";

        public static object Propcess { get; private set; }

        static async Task Main()
        {

            //RequestResponse rrr = Factory.Create<RequestResponse>();
            //rrr.ErrorMessage = "";
            //rrr.HttpStatusCode = HttpStatusCode.OK;
            //rrr.Result = File.ReadAllText(@"C:\metadata.xml");

            //await BuildModels(rrr.Result).ConfigureAwait(false);
            //return;


            Utils utils = Utils.Instance;
            Console.WriteLine("[TRY]\t\tBuilding Connections string from Clio settings");
            try
            {
                utils.ConnectionString = GetConnectionString();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK]\t\tSuccessfully obtained connection string ...");
                Console.WriteLine();
                Console.ResetColor();
            }
            catch (FileNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ERROR]: \t");
                Console.ResetColor();
                Console.Write($"{e.Message} File: ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(e.FileName);
                Console.WriteLine();
                Console.ResetColor();
                return;
            }

            if (utils.ConnectionString != null)
            {
                Console.WriteLine($"[TRY]\t\tAttempting to connect to {utils.ConnectionString.Uri}");
                try
                {
                    utils.Login();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK]\t\tLogin Successful !!!");

                    Console.ForegroundColor = ConsoleColor.Yellow;                    
                    foreach (Cookie cookie in utils.AuthCookieContainer.GetCookies(new Uri(utils.ConnectionString.Uri))) {
                        int length = (cookie.Value.Length > 30) ? 30 : cookie.Value.Length;
                        Console.WriteLine($"\t{cookie.Name}:\t{cookie.Value.Substring(0, length)}");
                    }
                    Console.WriteLine();
                    Console.ResetColor();
                }
                catch (ModelBuilderException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[ERROR] \t");
                    Console.ResetColor();
                    Console.Write(e.Message);
                    Console.WriteLine();
                    Console.ResetColor();
                    return;
                }
            }

            Console.WriteLine($"[TRY]\t\tAttempting to get Enity Metadata... this might take a while !");
            RequestResponse rr = await utils.GetMetadata().ConfigureAwait(false);
            await utils.LogoutAsync().ConfigureAwait(false);

            if (rr.ErrorMessage == null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                int count = CountXmlLines(rr?.Result??"");
                Console.WriteLine($"[OK]\t\tObtained definition for {count} entities");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine($"Would you like to create {count} models? Press any key to continue, <Esc> to exit");
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Exiting");
                }
                else {
                    Console.WriteLine("Sit tight I am woking on it");
                    await BuildModels(rr.Result).ConfigureAwait(false);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[ERROR] \t");
                Console.ResetColor();
                Console.Write(rr.ErrorMessage);
                Console.WriteLine();
                Console.ResetColor();
                return;
            }
        }
        
        /// <summary>
        /// Get Username, Password and URI for Clio ActiveEnvironment
        /// </summary>
        /// <returns>object containing credentials/returns>
        private static IConnectionString GetConnectionString() {
            IConnectionString cs = Factory.Create<ConnectionString>();

            string appdata = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{clioJson}";

            if (!System.IO.File.Exists(appdata)) {
                throw new FileNotFoundException("File Not Found", appdata);
            }
            string jsonFile = File.ReadAllText(appdata);
            JObject j = JsonConvert.DeserializeObject<JObject>(jsonFile);
            JToken activeEnvironmentKey = j.SelectToken("ActiveEnvironmentKey");
            string env = activeEnvironmentKey.Value<string>();

            cs.Username = j.SelectToken($"Environments.{env}.Login").Value<string>();
            cs.Password = j.SelectToken($"Environments.{env}.Password").Value<string>();
            cs.Uri = j.SelectToken($"Environments.{env}.Uri").Value<string>();
            return cs;
        }
        private static int CountXmlLines(string input) {

            XDocument xDoc = XDocument.Parse(input);

            int q = (from c in xDoc.Descendants()
                     where c.Name.LocalName == "EntityType"
                     select c).Count();
            return q;
        }
        
        
        /// <summary>
        /// Creates cs files in C:\Models
        /// </summary>
        /// <param name="input">XMl content</param>
        /// <returns></returns>
        private static async Task BuildModels(string input) {

            if (string.IsNullOrEmpty(input))
                return;

            XDocument xDoc = XDocument.Parse(input);
            var nSpace = from c in xDoc.Descendants()
                         where c.Name.LocalName == "Schema"
                         select c.Attribute("Namespace").Value;
            
            //Create Directory
            string dir = @"C:\Models";
            Directory.CreateDirectory(dir);

            var associations = (from ent in xDoc.Descendants()
                               where ent.Name.LocalName == "Association"
                               select ent).ToList();


            var entities = from ent in xDoc.Descendants()
                           where ent.Name.LocalName == "EntityType"
                           select ent;

            foreach (XElement entity in entities) {

                IEnumerable<XElement> keys = from key in entities.Descendants()
                           where key.Name.LocalName == "PropertyRef"
                           select key;


                //string className = entity.Attribute("Name").Value;
                //Console.WriteLine(className);

                EntityBuilder eb = Factory.Create<EntityBuilder>();
                BaseModel bm = eb.Build(nSpace.FirstOrDefault(), entity, keys, associations); ;

                string fullPath = $"{dir}\\{bm.Class.Name}.cs";
                await CreateSourceFile(fullPath, bm.ToString()).ConfigureAwait(false);
            }
        }
        private static async Task CreateSourceFile(string fullPath, string content) {

            if (string.IsNullOrEmpty(content) ||  string.IsNullOrEmpty(fullPath))
                return;
            
            using StreamWriter sw = File.CreateText(fullPath);
            try
            {
                await sw.WriteAsync(content).ConfigureAwait(false);
                ConsoleWriter.WriteMessage(MessageType.OK, $"Created: {fullPath}");
            }
            catch (IOException ex)
            {
                ConsoleWriter.WriteMessage(MessageType.Error, ex.Message);
            }
            finally
            {
                await sw.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
    
    public class CObjectAttribute : Attribute
    {
        public string RootSchemname { get; set; } = "";
    }
    public class CPropertyAttribute : Attribute 
    {
        public string ColumnPath { get; set; }
        public bool IsKey { get; set; }
        public string Association { get; set; }
        public string Navigation { get; set; }
    }






}

