using System.Runtime.InteropServices;

namespace ChromeHtmlToPdfLib.EncodingTools.Multilang
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct tagUNICODERANGE
    {
        public ushort wcFrom;
        public ushort wcTo;
    }
}