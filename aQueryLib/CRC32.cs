namespace SteamLib
{
    [System.Diagnostics.DebuggerStepThrough()]
    public class CRC32
    {

        private int[] crc32Table;

        private const int BUFFER_SIZE = 1024;
        public int GetCrc32(System.IO.Stream stream)
        {
            uint crc32Result = 0xffffffff;

            byte[] buffer = new byte[BUFFER_SIZE + 1];
            int readSize = BUFFER_SIZE;
            int count = stream.Read(buffer, 0, readSize);
            int i = 0;
            int iLookup = 0;

            while ((count > 0))
            {
                for (i = 0; i <= count - 1; i++)
                {
                    iLookup = (int) ((crc32Result & 0xff) ^ buffer[i]);
                    crc32Result = ((crc32Result & 0xffffff00) / 0x100) & 0xffffff;
                    // nasty shr 8 with vb :/
                    crc32Result = (uint) (crc32Result ^ crc32Table[iLookup]);
                }
                count = stream.Read(buffer, 0, readSize);
            }
            return (int) ~crc32Result;
        }

        internal string GetCrc32String(ref System.IO.Stream stream)
        {
            return string.Format("{0:X8}", GetCrc32(stream));
        }

        internal CRC32()
        {
            // This is the official polynomial used by CRC32 in PKZip.
            // Often the polynomial is shown reversed (04C11DB7).
            uint dwPolynomial = 0xedb88320;
            int i = 0;
            int j = 0;

            crc32Table = new int[257];
            long dwCrc = 0;

            for (i = 0; i <= 255; i++)
            {
                dwCrc = i;
                for (j = 8; j >= 1; j += -1)
                {
                    if ((dwCrc & 1) > 0)
                    {
                        dwCrc = ((dwCrc & 0xfffffffe) / 2) & 0x7fffffff;
                        dwCrc = dwCrc ^ dwPolynomial;
                    }
                    else
                    {
                        dwCrc = ((dwCrc & 0xfffffffe) / 2) & 0x7fffffff;
                    }
                }
                crc32Table[i] = (int) dwCrc;
            }
        }
    }
}


