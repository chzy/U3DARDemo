using System.Collections;
// using Com.Rokid.Base;
using UnityEngine;

namespace ARMazGlass.Scripts.Utils
{
    public class RDebug
    {
        private const int VERBOSE = 2;
        private const int DEBUG = 3;
        private const int INFO = 4;
        private const int WARN = 5;
        private const int ERROR = 6;

        private const string TAG = "Debug_ARMaz3-Lite:::";

        public static void V(string tag, string message)
        {
            Log(VERBOSE, tag, message);
        }
        
        public static void D(string tag, string message)
        {
            Log(DEBUG, tag, message);

        }
        
        public static void I(string tag, string message)
        {
            Log(INFO, tag, message);

        }
        
        public static void W(string tag, string message)
        {
            Log(WARN, tag, message);
        }
        
        public static void E(string tag, string message)
        {
            Log(ERROR, tag, message);
        }

        private static void Log(int level, string tag, string message)
        {
            Debug.Log($"{TAG} | {tag} | {message}");
        }
    }
}