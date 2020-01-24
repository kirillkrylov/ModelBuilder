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
                    break;
                case MessageType.Detail:
                    break;
                case MessageType.Try:
                    break;



                default:
                    break;
            }
        }

        private static void OkMessage(string message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[ OK ]");
            Console.ResetColor();
            Console.Write("\t\t");
            Console.Write(message);
            Console.WriteLine();
            Console.ResetColor();
        }

        private static void ErrorMessage(string message) 
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ ERROR ]");
            Console.ResetColor();
            Console.Write("\t\t");
            Console.Write(message);
            Console.WriteLine();
            Console.ResetColor();
        }


        private static void TryMessage(string message)
        {
            Console.Write("[ TRY ]");
            Console.ResetColor();
            Console.Write("\t\t");
            Console.Write(message);
            Console.WriteLine();
            Console.ResetColor();
        }

    }
}
