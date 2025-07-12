/*
# based on RTL872xDx ROM Bootloader Utility Ver 05.11.2020
# Created on: 10.10.2017
# Author: pvvx
# 
# 17/02/2025 - WaitResp 1000
# 12/07/2025 - C# port, fixes for BW16E
*/
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace RTL872xTool
{

    class Program
    {
        static void Main(string[] args)
        {
            string port = "COM3";
            int baud = 1500000;
            string operation = "wf"; // "rf", "wf", or "tf"
            int address = 0;
            int size = 0x400000;
         //  size = 0x4000;
            string filename = "dump.bin";
            filename = "BW16E-RTL8720DN-factory-p.kaczmarek-20250712.bin";


            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-p" && i + 1 < args.Length)
                {
                    port = args[++i];
                }
                else if (args[i] == "-b" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out baud);
                }
                else if (args[i] == "wf" || args[i] == "rf" || args[i] == "tf" || args[i] == "ef")
                {
                    operation = args[i];

                    if (i + 2 < args.Length)
                    {
                        int.TryParse(args[i + 1], out address);
                        if (operation == "ef" && i + 1 < args.Length)
                        {
                            if (args[i + 2].StartsWith("0x"))
                                size = Convert.ToInt32(args[i + 2], 16);
                            else
                                int.TryParse(args[i + 2], out size);
                        }
                        else
                        {
                            filename = args[i + 2];
                        }
                        i += 2;

                        if (operation == "rf" && i + 1 < args.Length)
                        {
                            int.TryParse(args[i - 1], out address); // 1st arg after "rf"
                            if (args[i].StartsWith("0x"))
                                size = Convert.ToInt32(args[i], 16);
                            else
                                int.TryParse(args[i], out size);
                            filename = args[i + 1];
                            i += 1;
                        }
                        else if (operation == "tf" && i + 1 < args.Length)
                        {
                            if (args[i].StartsWith("0x"))
                                size = Convert.ToInt32(args[i], 16);
                            else
                                int.TryParse(args[i], out size);
                            i += 1;
                        }
                    }
                }
            }


            RTLXMD rtl = new RTLXMD(port, 115200, 200);

            if (operation == "wf")
            {
                WriteFlash(rtl, baud, address, filename);
            }
            else if (operation == "rf")
            {
                ReadFlash(rtl, baud, address, size, filename);
            }
            else if (operation == "ef")
            {
                EraseFlash(rtl, baud, address, size);
            }
            else if (operation == "tf")
            {
                TestFlash(rtl, baud, address, size);
            }
            else
            {
                Console.WriteLine("Only 'rf', 'wf', and 'tf' operations are implemented.");
            }
        }

        static void WriteFlash(RTLXMD rtl, int baud, int address, string filename)
        {
            Console.WriteLine("=== WriteFlash ===");
            Console.WriteLine("Connecting...");
            if (!rtl.Connect())
            {
                Console.WriteLine("Failed to connect device!");
                return;
            }
            Console.WriteLine("Connected");
            if (!File.Exists(filename))
            {
                Console.WriteLine("File not found: " + filename);
                return;
            }

            FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            int size = (int)stream.Length;
            if (size < 1)
            {
                stream.Close();
                Console.WriteLine("Error: File size = 0!");
                return;
            }

         /*   if (null==rtl.SetFlashStatus((byte)0))
            {
                Console.WriteLine("Error: Set Flash Status!");
                return;
            }*/

            if (!rtl.Floader(baud))
            {
                stream.Close();
                rtl.RestoreBaud();
                return;
            }

            int count = (size + 4095) / 4096;
            int eraseSize = count * 4096;
            int eraseOffset = address & 0xfff000;
            Console.WriteLine("Erase Flash {0} sectors, data from 0x{1:X8} to 0x{2:X8}", count, eraseOffset, eraseOffset + eraseSize);

            if (!rtl.EraseSectorsFlash(eraseOffset, size))
            {
                Console.WriteLine("Error: Erase Flash sectors!");
                stream.Close();
                rtl.RestoreBaud();
                return;
            }

            int writeOffset = address & 0x00ffffff;
            writeOffset |= 0x08000000;
            Console.WriteLine("Write Flash data 0x{0:X8} to 0x{1:X8} from file: {2}", writeOffset, writeOffset + size, filename);

            if (!rtl.WriteBlockFlash(stream, writeOffset, size))
            {
                Console.WriteLine("Error: Write Flash!");
                stream.Close();
                rtl.RestoreBaud();
                return;
            }

            stream.Close();

            /*
            uint? checksum = rtl.FlashWrChkSum(writeOffset, size);
            if (checksum == null)
            {
                Console.WriteLine("Flash block checksum retrieval error!");
                rtl.RestoreBaud();
                return;
            }

            Console.WriteLine("Checksum of the written block in Flash: 0x{0:X8}", checksum.Value);*/


            rtl.RestoreBaud();
        }

        static void EraseFlash(RTLXMD rtl, int baud, int address, int size)
        {
            Console.WriteLine("=== EraseFlash ===");
            Console.WriteLine("Connecting...");
            if (!rtl.Connect())
            {
                Console.WriteLine("Failed to connect device!");
                return;
            }
            Console.WriteLine("Connected");
            /*   if (null==rtl.SetFlashStatus((byte)0))
               {
                   Console.WriteLine("Error: Set Flash Status!");
                   return;
               }*/

            if (!rtl.Floader(baud))
            {
                rtl.RestoreBaud();
                return;
            }

            int count = (size + 4095) / 4096;
            int eraseSize = count * 4096;
            int eraseOffset = address & 0xfff000;
            Console.WriteLine("Erase Flash {0} sectors, data from 0x{1:X8} to 0x{2:X8}", count, eraseOffset, eraseOffset + eraseSize);

            if (!rtl.EraseSectorsFlash(eraseOffset, size))
            {
                Console.WriteLine("Error: Erase Flash sectors!");
                rtl.RestoreBaud();
                return;
            }


            /*
            uint? checksum = rtl.FlashWrChkSum(writeOffset, size);
            if (checksum == null)
            {
                Console.WriteLine("Flash block checksum retrieval error!");
                rtl.RestoreBaud();
                return;
            }

            Console.WriteLine("Checksum of the written block in Flash: 0x{0:X8}", checksum.Value);*/


            rtl.RestoreBaud();
        }
        static void ReadFlash(RTLXMD rtl, int baud, int address, int size, string filename)
        {
            Console.WriteLine("=== ReadFlash ===");
            Console.WriteLine("Connecting...");
            if (!rtl.Connect())
            {
                Console.WriteLine("Failed to connect device!");
                return;
            }
            Console.WriteLine("Connected");
            if (size < 0 || address < 0 || address + size > 0x1000000)
            {
                Console.WriteLine("Bad parameters!");
                return;
            }

            if (!rtl.Floader(baud))
                return;

            int offset = address & 0x00ffffff;
            Console.WriteLine("Read Flash data from 0x{0:X8} to 0x{1:X8} in file: {2}", offset, offset + size, filename);

            FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write);
            if (!rtl.ReadBlockFlash(stream, offset, size))
            {
                stream.Close();
                rtl.RestoreBaud();
                return;
            }
            stream.Close();

            uint? checksum = rtl.FlashWrChkSum(0, size);
            if (checksum == null)
            {
                Console.WriteLine("Flash block checksum retrieval error!");
                rtl.RestoreBaud();
                return;
            }
            rtl.RestoreBaud();
            Console.WriteLine("Done!");
        }

        static void TestFlash(RTLXMD rtl, int baud, int address, int size)
        {
            string dirName = "Test_" + DateTime.Now.ToString("yyyyMMdd-HHmm");
            Directory.CreateDirectory(dirName);

            string original = Path.Combine(dirName, "original.bin");
            string random = Path.Combine(dirName, "random.bin");
            string randomRead = Path.Combine(dirName, "random_read.bin");

            Console.WriteLine("Step 1: Read original flash to " + original);
            ReadFlash(rtl, baud, address, size, original);

            Console.WriteLine("Step 2: Generate random flash data: " + random);
            byte[] data = new byte[size];
            //new Random().NextBytes(data);
            Random ra = new Random(DateTime.Now.Millisecond);
            int ofs = DateTime.Now.Millisecond;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)((i+ofs) % 255);
            }
            File.WriteAllBytes(random, data);
            Thread.Sleep(100);
            Console.WriteLine("Step 3: Write random flash");
            WriteFlash(rtl, baud, address, random);

            Thread.Sleep(100);
            Console.WriteLine("Step 4: Read back random flash to " + randomRead);
            ReadFlash(rtl, baud, address, size, randomRead);

            Console.WriteLine("Step 4.5: Compare written and read-back flash data");
            byte[] written = File.ReadAllBytes(random);
            byte[] readBack = File.ReadAllBytes(randomRead);

            bool match = written.Length == readBack.Length;
            if (match)
            {
                for (int i = 0; i < written.Length; i++)
                {
                    if (written[i] != readBack[i])
                    {
                        Console.WriteLine($"Mismatch at byte {i}: wrote 0x{written[i]:X2}, read 0x{readBack[i]:X2}");
                        match = false;
                        break;
                    }
                }
            }

            Console.WriteLine(match ? "Verification successful: data matches." : "Verification failed: data mismatch.");
            
            Thread.Sleep(100);
            Console.WriteLine("Step 5: Restore original flash");
            WriteFlash(rtl, baud, address, original);

            Console.WriteLine("Test flash operation complete.");
        }
    }
    public class RTLXMD
    {
        private SerialPort _port;
        private int _defaultTimeout;

        public RTLXMD(string portName, int baudRate, int timeoutMs)
        {
            try
            {
                _port = new SerialPort(portName, baudRate);
                _port.ReadTimeout = timeoutMs;
                _port.WriteTimeout = timeoutMs;
                _defaultTimeout = timeoutMs;
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Open {0}, {1} baud!", portName, baudRate);
                Environment.Exit(-1);
            }
        }

        public bool Connect()
        {
            _port.DtrEnable = false;
            _port.RtsEnable = true;
            Thread.Sleep(50);
            _port.DtrEnable = true;
            _port.RtsEnable = false;
            Thread.Sleep(50);
            _port.DtrEnable = false;
            return true;
        }
        public bool EraseSectorsFlash(int offset, int size)
        {
            int count = (size + 4095) / 4096;
            offset &= 0xfff000;

            if (count > 0 && count < 0x10000 && offset >= 0)
            {
                for (int i = 0; i < count; i++)
                {
                    byte[] pkt = new byte[6];
                    pkt[0] = 0x17; // CMD_EFS
                    pkt[1] = (byte)(offset & 0xFF);
                    pkt[2] = (byte)((offset >> 8) & 0xFF);
                    pkt[3] = (byte)((offset >> 16) & 0xFF);
                    pkt[4] = 0x01;
                    pkt[5] = 0x00;

                    if (!WriteCmd(pkt))
                        return false;

                    offset += 4096;
                }
                return true;
            }

            Console.WriteLine("Bad parameters!");
            return false;
        }
        public bool WriteBlockFlash(FileStream stream, int offset, int size)
        {
            return SendXmodem(stream, offset, size, 3);
        }

        public uint? FlashWrChkSum(int offset, int size)
        {
          //  size = 0x4000;
            byte[] pkt = new byte[7];
            pkt[0] = 0x27; // CMD_CRC
            pkt[1] = (byte)(offset & 0xFF);
            pkt[2] = (byte)((offset >> 8) & 0xFF);
            pkt[3] = (byte)(offset >> 16); 
            pkt[4] = (byte)(size & 0xFF);
            pkt[5] = (byte)((size >> 8) & 0xFF);
            pkt[6] = (byte)((size >> 16) & 0xFF);

            if (!WriteCmd(pkt, 0x27))
                return null;

            byte[] data = ReadBytes(4);
            if (data == null || data.Length != 4)
                return null;

            return (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        }

        private static readonly byte CMD_GFS = 0x21; // FLASH Read Status Register
        private static readonly byte CMD_SFS = 0x26; // FLASH Write Status Register
        public byte? GetFlashStatus(int num = 0)
        {
            byte[] blk;
            if (num == 0)
                blk = new byte[] { CMD_GFS, 0x05, 0x01 };
            else if (num == 1)
                blk = new byte[] { CMD_GFS, 0x35, 0x01 };
            else if (num == 2)
                blk = new byte[] { CMD_GFS, 0x15, 0x01 };
            else
                return null;

            if (!WriteCmd(blk, CMD_GFS))
                return null;

            try
            {
                int read = _port.ReadByte();
                if (read >= 0)
                    return (byte)read;
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        // Equivalent to Python SetFlashStatus(status, num=0)
        public byte? SetFlashStatus(byte status, int num = 0)
        {
            byte[] blk;
            byte statusByte = (byte)(status & 0xff);
            if (num == 0)
                blk = new byte[] { CMD_SFS, 0x01, 0x01, statusByte };
            else if (num == 1)
                blk = new byte[] { CMD_SFS, 0x31, 0x01, statusByte };
            else if (num == 2)
                blk = new byte[] { CMD_SFS, 0x11, 0x01, statusByte };
            else
                return null;

            if (WriteCmd(blk))
                return GetFlashStatus(num);

            return null;
        }
        public bool Floader(int baud)
        {
            if (!SetBaud(baud))
            {
                Console.WriteLine("Error Set Baud!");
                return false;
            }

            byte[] regs = ReadRegs(0x00082000, 4);
            if (regs == null || regs.Length != 4 || !(regs[0] == 33 && regs[1] == 32 && regs[2] == 8 && regs[3] == 0))
            {
                FileStream stream = new FileStream("imgtool_flashloader_amebad.bin", FileMode.Open, FileAccess.Read);
                long size = stream.Length;
                if (size < 1)
                {
                    stream.Close();
                    Console.WriteLine("Error: File size = 0!");
                    RestoreBaud();
                    return false;
                }
                int offset = 0x00082000;
                Console.WriteLine("Write SRAM at 0x{0:X8} to 0x{1:X8} from file: imgtool_flashloader_amebad.bin", offset, offset + (int)size);
                if (!WriteBlockMem(stream, offset, (int)size))
                {
                    stream.Close();
                    Console.WriteLine("Error Write!");
                    RestoreBaud();
                    return false;
                }
                stream.Close();
                SetComBaud(115200);
                if (!SetBaud(baud))
                {
                    Console.WriteLine("Error Set Baud!");
                    return false;
                }
            }

            return true;
        }

        public bool ReadBlockFlash(FileStream stream, int offset, int size)
        {
            int count = (size + 4095) / 4096;
            offset &= 0xffffff;

            if (count < 1 || count > 0x10000 || offset < 0)
            {
                Console.WriteLine("Bad parameters!");
                return false;
            }

            byte[] header = new byte[6];
            header[0] = 0x20;
            header[1] = (byte)(offset & 0xff);
            header[2] = (byte)((offset >> 8) & 0xff);
            header[3] = (byte)((offset >> 16) & 0xff);
            header[4] = (byte)(count & 0xFF);              // ushort (2B) - low byte
            header[5] = (byte)((count >> 8) & 0xFF);       // ushort (2B) - high byte

            try
            {
                _port.Write(header, 0, header.Length);
            }
            catch
            {
                Console.WriteLine("Error Write to COM Port!");
                return false;
            }

            count *= 4;
            for (int i = 0; i < count; i++)
            {
                if ((i & 63) == 0)
                {
                    Console.Write("Read block at 0x{0:X6}...", offset);
                }

                if (!WaitResp(0x02)) // STX
                {
                    Console.WriteLine("Error read block head id!");
                    return false;
                }

                byte[] hdr = ReadBytes(2);
                if (hdr == null || hdr.Length != 2 || hdr[0] != ((i + 1) & 0xff) || ((hdr[0] ^ 0xff) != hdr[1]))
                {
                    Console.WriteLine("Error read block head!");
                    return false;
                }

                byte[] data = ReadBytes(1025);
                if (data == null || data.Length != 1025)
                {
                    return false;
                }

                if (data[1024] != CalcChecksum(data, 0, 1024))
                {
                    WriteCmd(new byte[] { 0x18 }); // CAN
                    Console.WriteLine("Bad Checksum!");
                    return false;
                }

                if (size > 1024)
                {
                    _port.Write(new byte[] { 0x06 }, 0, 1); // ACK
                    stream.Write(data, 0, 1024);
                }
                else
                {
                    stream.Write(data, 0, size);
                    WriteCmd(new byte[] { 0x18 }); // CAN
                    if ((i & 63) == 0)
                        Console.WriteLine("ok");
                    return true;
                }

                size -= 1024;
                offset += 1024;
                if ((i & 63) == 0)
                    Console.WriteLine("ok");
            }

            return true;
        }

        public byte[] ReadRegs(int offset, int size)
        {
            MemoryStream ms = new MemoryStream();
            while (size > 0)
            {
                byte[] pkt = new byte[5];
                pkt[0] = 0x31;
                pkt[1] = (byte)(offset & 0xff);
                pkt[2] = (byte)((offset >> 8) & 0xff);
                pkt[3] = (byte)((offset >> 16) & 0xff);
                pkt[4] = (byte)((offset >> 24) & 0xff);

                try
                {
                    _port.Write(pkt, 0, 5);
                }
                catch
                {
                    Console.WriteLine("Error Write to COM Port!");
                    return null;
                }

                if (!WaitResp(0x31))
                {
                    Console.WriteLine("Error read data head id!");
                    return null;
                }

                byte[] data = ReadBytes(5);
                if (data == null || data.Length != 5 || data[4] != 0x15)
                    return null;

                ms.Write(data, 0, 4);
                size -= 4;
                offset += 4;
            }

            return ms.ToArray();
        }

        private byte CalcChecksum(byte[] data, int start, int length)
        {
            int sum = 0;
            for (int i = start; i < length; i++)
            {
                sum += data[i];
            }
            return (byte)(sum & 0xff);
        }

        private byte[] ReadBytes(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            int retries = 1000;

            while (offset < count && retries > 0)
            {
                try
                {
                    int read = _port.Read(buffer, offset, count - offset);
                    if (read > 0)
                    {
                        offset += read;
                    }
                    else
                    {
                        retries--;
                        Thread.Sleep(1);
                    }
                }
                catch
                {
                    return null;
                }
            }

            if (offset == count)
                return buffer;

            return null;
        }

        private bool WaitResp(byte code)
        {
            int retries = 1000;
            while (retries-- > 0)
            {
                try
                {
                    int val = _port.ReadByte();
                    if (false)
                    {
                        Console.WriteLine("Try " + retries + " wants " + code + " got " + val);
                    }
                    if (val == -1)
                        return false;
                    if ((byte)val == code)
                        return true;
                }
                catch
                {
                    Thread.Sleep(1);
                   // return false;
                }
            }
            return false;
        }

        private bool WriteCmd(byte[] cmd, byte ack = 0x06)
        {
            try
            {
                _port.Write(cmd, 0, cmd.Length);
                return WaitResp(ack); // ACK
            }
            catch
            {
                return false;
            }
        }

        private bool SetBaud(int baud)
        {
            if (_port.BaudRate != baud)
            {
                Console.WriteLine("Set baudrate " + baud);
                int x = 0x0D;
                int[] br = { 115200, 128000, 153600, 230400, 380400, 460800, 500000, 921600, 1000000, 1382400, 1444400, 1500000 };
                foreach (int el in br)
                {
                    if (el >= baud)
                    {
                        baud = el;
                        break;
                    }
                    x++;
                }

                byte[] pkt = new byte[2];
                pkt[0] = 0x05;
                pkt[1] = (byte)x;
                if (!WriteCmd(pkt))
                    return false;

                return SetComBaud(baud);
            }
            return true;
        }

        private bool SetComBaud(int baud)
        {
            try
            {
                _port.Close();
                _port.BaudRate = baud;
                _port.Open();
                _port.ReadTimeout = _defaultTimeout;
                _port.WriteTimeout = _defaultTimeout;
                Thread.Sleep(50);
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
            }
            catch
            {
                Console.WriteLine("Error: ReOpen COM port at " + baud);
                Environment.Exit(-1);
            }
            return true;
        }

        public bool RestoreBaud()
        {
            return SetBaud(115200);
        }

        public bool WriteBlockMem(FileStream stream, int offset, int size)
        {
            return SendXmodem(stream, offset, size, 3);
        }
        private bool SendXmodem(Stream stream, int offset, int size, int retry)
        {
            if (!WriteCmd(new byte[] { 0x07 })) // CMD_XMD
                return false;

            //this.chk32 = 0;
            int sequence = 1;

            while (size > 0)
            {
                int packetSize;
                byte cmd;
                if (size <= 128)
                {
                    packetSize = 128;
                    cmd = 0x01; // SOH
                }
                else
                {
                    packetSize = 1024;
                    cmd = 0x02; // STX
                }

                int rdsize = (size < packetSize) ? size : packetSize;
                byte[] data = new byte[rdsize];
                int read = stream.Read(data, 0, rdsize);
                if (read <= 0)
                {
                    Console.WriteLine("send: at EOF");
                    return false;
                }

                // Pad data to packetSize with 0xFF
                byte[] paddedData = new byte[packetSize];
                for (int i = 0; i < packetSize; i++)
                {
                    if (i < read)
                        paddedData[i] = data[i];
                    else
                        paddedData[i] = 0xFF;
                }

                // Construct packet
                byte[] pkt = new byte[3 + 4 + packetSize + 1];
                pkt[0] = cmd;
                pkt[1] = (byte)sequence;
                pkt[2] = (byte)(0xFF - sequence);
                pkt[3] = (byte)(offset & 0xFF);
                pkt[4] = (byte)((offset >> 8) & 0xFF);
                pkt[5] = (byte)((offset >> 16) & 0xFF);
                pkt[6] = (byte)((offset >> 24) & 0xFF);
                for (int i = 0; i < packetSize; i++)
                    pkt[7 + i] = paddedData[i];

                pkt[7 + packetSize] = CalcChecksum(pkt, 3, pkt.Length);

                if(false)
                {
                    Console.Write("Sending packet: ");
                    for (int i = 0; i < pkt.Length; i++)
                        Console.Write(pkt[i].ToString("X2") + " ");
                    Console.WriteLine();
                }

                // Retry logic
                int errorCount = 0;
                while (true)
                {
                    if (WriteCmd(pkt))
                    {
                        sequence = (sequence + 1) % 256;
                        offset += packetSize;
                        size -= rdsize;
                        break;
                    }
                    else
                    {
                        errorCount++;
                        if (errorCount > retry)
                            return false;
                    }
                }
            }

            return WriteCmd(new byte[] { 0x04 }); // EOT
        }

    }
}
