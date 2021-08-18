using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace OpenEEG
{
    public struct DataFrame
    {
        public int counter;
        public int[] samples;
    }

    public class OpenEEGInterface
    {
        // Events
        public event EventHandler DataFrameReady;
        public event EventHandler DataBlockReady;

        private SerialPort ComPort;
        private int index;
        private int lastx;
        private int blockLength;
        private int[] buffer = new int[16];
        private Queue<DataFrame> fifo = new Queue<DataFrame>();

        public OpenEEGInterface()
        {
            ComPortSelector cps = new ComPortSelector();
            cps.ShowDialog();    
            init(cps.selectedPort);
        }
        public OpenEEGInterface(int BlockLength)
        {
            ComPortSelector cps = new ComPortSelector();
            cps.ShowDialog();
            init(cps.selectedPort);
            SetBlockLength(BlockLength);
        }
        public OpenEEGInterface(String PortName, int BlockLength)
        {
            init(PortName);
            SetBlockLength(BlockLength);
        }
        public OpenEEGInterface(String PortName)
        {
            init(PortName);
        }
        ~OpenEEGInterface()
        {
            Console.WriteLine("Destroying");
            ComPort.DataReceived -= OnData;
            ComPort.Close();
            if (ComPort.IsOpen)
            {
                ComPort.Close();
                Console.WriteLine("Closed com port");
            }
        }

        void init(String PortName)
        {
            index = 100;
            lastx = -1;
            blockLength = 256;
            // Set up com port
            ComPort = new SerialPort();
            if (PortName.Length > 0)
            {
                ComPort.PortName = PortName;
                ComPort.BaudRate = 57600;
                ComPort.Parity = Parity.None;
                ComPort.DataBits = 8;
                ComPort.StopBits = StopBits.One;
                ComPort.Handshake = Handshake.None;
                // Set the read/write timeouts
                ComPort.ReadTimeout = 500;
                ComPort.WriteTimeout = 500;
                // Add event handler
                ComPort.DataReceived += OnData;
                try
                {
                    ComPort.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} Exception caught.", e);
                }
            }
        }

        private void OnData(object sender, System.EventArgs e)
        {
            while (ComPort.BytesToRead > 0)
            {
                int x = ComPort.ReadByte();

                /* Packet structure:
	                    uint8_t		sync0;		// = 0xA5
	                    uint8_t		sync1;		// = 0x5A
	                    uint8_t		version;	// = 2
	                    uint8_t		count;		// packet counter. Increases by 1 each packet
	                    uint16_t	data[6];	// 10-bit sample (= 0 - 1023) in big endian (Motorola) format
	                    uint8_t		switches;	// State of PD5 to PD2, in bits 3 to 0
                 */

                if (lastx == 165 && x == 90)    // Check for start of packet
                {
                    index = 0;
                }
                if (index >= 1 && index < 16)   // Write data to temporary buffer
                {
                    buffer[index - 1] = x;
                }

                // At end of frame, assemble all channels into a single dataframe and
                // push into the fifo.
                if (index == 15)
                {
                    DataFrame df;
                    df.samples = new int[6];
                    df.counter = buffer[1];
                    for (int n = 0; n < 6; n++)
                    {
                        df.samples[n] = buffer[n * 2 + 2] * 256 + buffer[n * 2 + 3];
                    }
                    fifo.Enqueue(df);
                    OnDataFrameReady();
                    if (fifo.Count == blockLength)
                        OnDataBlockReady();
                }
                index++;
                lastx = x;
            }
        }

        protected virtual void OnDataFrameReady()
        {
            EventHandler handler = DataFrameReady;
            if (handler != null)
            {
                handler(this, null);
            }
        }
        protected virtual void OnDataBlockReady()
        {
            EventHandler handler = DataBlockReady;
            if (handler != null)
            {
                handler(this, null);
            }
        }
        public int GetBlockLength()
        {
            return blockLength;
        }
        public void SetBlockLength(int length)
        {
            blockLength = length;
            if (fifo.Count >= length)
                OnDataBlockReady();
        }
        public void GetBlock(out DataFrame[] block)
        {
            block = new DataFrame[blockLength];
            for (int n = 0; n < blockLength; n++)
            {
                block[n] = fifo.Dequeue();
            }
        }
        public DataFrame GetSample()
        {
            return fifo.Dequeue();
        }
    }
}
