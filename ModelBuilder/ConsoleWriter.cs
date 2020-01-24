using ModelBuilder.Properties;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModelBuilder
{
    public static class ConsoleWriter
    {
        public static void WriteMessage(MessageType type, string message) {

            switch (type)
            {
                case MessageType.OK:
                    OkMessage(message);
                    break;
                case MessageType.Error:
                    ErrorMessage(message);
                    break;
                case MessageType.Warning:
                    break;
                case MessageType.Info:
                    InfoMessage(message);
                    break;
                case MessageType.Detail:
                    break;
                case MessageType.Try:
                    TryMessage(message);
                    break;

                default:
                    break;
            }
        }

        private static void OkMessage(string message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(Resources.ok);
            Console.ResetColor();
            Console.Write($"\t\t{message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void ErrorMessage(string message) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(Resources.error);
            Console.ResetColor();
            Console.Write($"\t\t{message}");
            Console.ResetColor();
            Console.WriteLine();
        }
        
        private static void TryMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Resources._try);
            Console.ResetColor();
            Console.Write($"\t\t{message}");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void InfoMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(Resources.info);
            Console.ResetColor();
            Console.Write($"\t\t{message}");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
