// Author: Manuel Antony
// This will be only used in offline formatting.

using System;

namespace CshapInstrumenter
{

    class FormatterOffline
    {
        const string formatter = @"C:\\ATS\\formater\\formater-0.0.1-SNAPSHOT.jar";
        String stringToFormat;

        public String Format(String stringToFormat)
        {
            this.stringToFormat = stringToFormat;
            return FormatSignature();
        }

        private String FormatSignature()
        {
            String formatType = GetArgumentType("signature");
            ProcessRunner proceessRunner = new ProcessRunner();
            String argument = formatType + " " + stringToFormat;
            return proceessRunner.RunJar(formatter, argument);
        }

        private String GetArgumentType(String argument)
        {
            String type = null;

            switch (argument)
            {
                case "signature":
                    type = "SIG";
                    break;
                default:
                    break;
            }

            return type;
        }

    }
}
