using System;
using CshapInstrumenter.Services;

namespace CshapInstrumenter.Formatter
{
    class Formatter
    {
        String stringToFormat = null;

        public String Format(String stringToFormat)
        {
            this.stringToFormat = stringToFormat;
            return FormatSiganture();
        }

        private String FormatSiganture()
        {
            return new ServiceConnector().FormatService(stringToFormat);
        }
    }
}
