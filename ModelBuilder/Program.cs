using ModelBuilder.Properties;
using Newtonsoft.Json;
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
        static async Task Main()
        {
            //RequestResponse rrr = Factory.Create<RequestResponse>();
            //rrr.ErrorMessage = "";
            //rrr.HttpStatusCode = HttpStatusCode.OK;
            //rrr.Result = File.ReadAllText(@"C:\metadata.xml");

            //await BuildModels(rrr.Result).ConfigureAwait(false);
            //return;


            Utils utils = Utils.Instance;
            ConsoleWriter.WriteMessage(MessageType.Try, Resources.TryMsgBuildConStr);
            try
            {
                utils.ConnectionString = GetConnectionString();
                ConsoleWriter.WriteMessage(MessageType.OK, Resources.OkMsgGotCS);
            }
            catch (FileNotFoundException e)
            {
                ConsoleWriter.WriteMessage(MessageType.Error, $"{e.Message} File: {e.FileName}");
                return;
            }

            if (utils.ConnectionString != null)
            {
                ConsoleWriter.WriteMessage(MessageType.Try, $"Attempting to connect to {utils.ConnectionString.Uri}");
                try
                {
                    utils.Login();
                    Console.ForegroundColor = ConsoleColor.Green;
                    ConsoleWriter.WriteMessage(MessageType.OK, Resources.OkMsgLoginOk);

                    Console.ForegroundColor = ConsoleColor.Yellow;                    
                    foreach (Cookie cookie in utils.AuthCookieContainer.GetCookies(new Uri(utils.ConnectionString.Uri))) {
                        int length = (cookie.Value.Length > 30) ? 30 : cookie.Value.Length;
                        //Console.WriteLine($"\t{cookie.Name}:\t{cookie.Value.Substring(0, length)}");
                        ConsoleWriter.WriteMessage(MessageType.Info, $"\t{cookie.Name}:\t{cookie.Value.Substring(0, length)}");
                    }
                    Console.WriteLine();
                    
                }
                catch (ModelBuilderException e)
                {
                    ConsoleWriter.WriteMessage(MessageType.Error, e.Message);
                    Console.WriteLine();
                    return;
                }
            }

            ConsoleWriter.WriteMessage(MessageType.Try, Resources.TryMsgAttempting);
            RequestResponse rr = await utils.GetMetadata().ConfigureAwait(false);
            await utils.LogoutAsync().ConfigureAwait(false);

            if (rr.ErrorMessage == null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                int count = CountXmlLines(rr?.Result??"");
                ConsoleWriter.WriteMessage(MessageType.OK, $"Obtained definition for {count} entities");
                Console.WriteLine();
                Console.WriteLine($"Would you like to create {count} models? Press any key to continue, <Esc> to exit");
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine(Resources.MsgExit);
                }
                else {
                    Console.WriteLine(Resources.MsgWait);
                    await BuildModels(rr.Result).ConfigureAwait(false);
                }
            }
            else
            {
                ConsoleWriter.WriteMessage(MessageType.Error, rr.ErrorMessage);
                Console.WriteLine();
                return;
            }
        }
        
        /// <summary>
        /// Get Username, Password and URI for Clio ActiveEnvironment
        /// </summary>
        /// <returns>object containing credentials/returns>
        private static IConnectionString GetConnectionString() {
            IConnectionString cs = Factory.Create<ConnectionString>();

            string appdata = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}{Resources.clioPath}";

            if (!File.Exists(appdata)) {
                throw new FileNotFoundException(Resources.MsgFileNotFound, appdata);
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
            string dir = Resources.SaveToPath;
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
}

