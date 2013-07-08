using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace Bunny.Core
{
    class Log
    {
        private static TextWriter _textWriter;
        private static StreamWriter _streamWriter;
        private static volatile object _oLock;

        public static void Initialize()
        {
            _textWriter = Console.Out;
            _streamWriter = new StreamWriter("Bunny.txt", true);
            _oLock = new object();
        }

        public static void Write(string format, params object[] pParams)
        {
            var final = string.Format("[{0}] - {1} - ", DateTime.Now, new StackTrace().GetFrame(1).GetMethod().Name);
            lock (_oLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                _textWriter.Write(final);
                Console.ForegroundColor = ConsoleColor.Gray;
                _textWriter.WriteLine(format, pParams);

                _streamWriter.Write(final);
                _streamWriter.WriteLine(format, pParams);
				
				_streamWriter.Flush();
				_textWriter.Flush();
            }
        }

        public static void PacketLog(byte[] data, int index, int length)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var sDump = (length > 0 ? BitConverter.ToString(data, index, length) : "");
            var sDumpHex = sDump.Split('-');
            var lstDump = new List<string>();
            string sHex = "";
            string sAscii = "";
            char cByte;
            if (sDump.Length > 0)
            {
                for (Int32 iCount = 0; iCount < sDumpHex.Length; iCount++)
                {
                    cByte = Convert.ToChar(data[index + iCount]);
                    sHex += sDumpHex[iCount] + ' ';
                    if (char.IsWhiteSpace(cByte) || char.IsControl(cByte))
                    {
                        cByte = '.';
                    }
                    if (cByte == '{' || cByte == '}')
                        cByte = '.';
                    sAscii += cByte.ToString();
                    if ((iCount + 1) % 16 == 0)
                    {
                        lstDump.Add(sHex + " " + sAscii);
                        sHex = "";
                        sAscii = "";
                    }
                }
                if (sHex.Length > 0)
                {
                    if (sHex.Length < (16 * 3)) sHex += new string(' ', (16 * 3) - sHex.Length);
                    lstDump.Add(sHex + " " + sAscii);
                }
            }
            lock (_oLock)
            {
                for (Int32 iCount = 0, j = 0; iCount < lstDump.Count; iCount++, j++)
                {
                    _textWriter.WriteLine(lstDump[iCount]);
                }
            }
        }
    }
}
