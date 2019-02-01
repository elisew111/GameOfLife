using Cloo;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Template
{
    class Game
    {
        string borderMode = "";
        int generation = 0;
        // helper function for getting one bit from the secondary pattern buffer
        uint GetBit(uint x, uint y) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }
        // mouse handling: dragging functionality
        uint xoffset = 0, yoffset = 0;
        bool lastLButtonState = false;
        int dragXStart, dragYStart, offsetXStart, offsetYStart;
        public void SetMouseState(int x, int y, bool pressed)
        {
            if (pressed)
            {
                if (lastLButtonState)
                {
                    int deltax = x - dragXStart, deltay = y - dragYStart;
                    xoffset = (uint)Math.Min(pw * 32 - screen.width, Math.Max(0, offsetXStart - deltax));
                    yoffset = (uint)Math.Min(ph - screen.height, Math.Max(0, offsetYStart - deltay));
                }
                else
                {
                    dragXStart = x;
                    dragYStart = y;
                    offsetXStart = (int)xoffset;
                    offsetYStart = (int)yoffset;
                    lastLButtonState = true;
                }
            }
            else lastLButtonState = false;
        }


        // load the OpenCL program; this creates the OpenCL context
        static OpenCLProgram ocl = new OpenCLProgram("../../program.cl");
        // find the kernel  in the program

        OpenCLKernel kernel;
        // create a regular buffer; by default this resides on both the host and the device
        OpenCLBuffer<uint> pBuffer, sBuffer;
        // create an OpenGL texture to which OpenCL can send data
        OpenCLImage<int> image = new OpenCLImage<int>(ocl, 512, 512);
        public Surface screen;
        Stopwatch timer = new Stopwatch();
        float t = 21.5f;
        uint[] pattern;
        uint[] second;
        uint pw, ph, w; // note: pw is in uints; width in bits is 32 this value.
        void BitSet(uint x, uint y) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }

        public void Init()
        {
            while (borderMode != "wrap_mode" && borderMode != "dead_mode")
            {
                Console.WriteLine("Border control: Would you like to use wrapping borders (wrap/w) or 'dead' borders (dead/d)?");
                borderMode = Console.ReadLine();
                if (borderMode == "w" || borderMode == "wrap") borderMode = "wrap_mode";
                if (borderMode == "d" || borderMode == "dead") borderMode = "dead_mode";
            }
            kernel = new OpenCLKernel(ocl, borderMode);
            //gekopieerd uit gameoflife
            StreamReader sr = new StreamReader("../../c4-orthogonal.rle");
            uint state = 0, n = 0, x = 0, y = 0;
            while (true)
            {
                String line = sr.ReadLine();
                if (line == null) break; // end of file
                int pos = 0;
                if (line[pos] == '#') continue; /* comment line */
                else if (line[pos] == 'x') // header
                {
                    String[] sub = line.Split(new char[] { '=', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    pw = (UInt32.Parse(sub[1]) + 31) / 32;
                    ph = UInt32.Parse(sub[3]);
                    if (screen.height > ph) ph = (uint)screen.height;
                    pattern = new uint[pw * ph];
                    second = new uint[pw * ph];
                }
                else while (pos < line.Length)
                    {
                        Char c = line[pos++];
                        if (state == 0) if (c < '0' || c > '9') { state = 1; n = Math.Max(n, 1); } else n = (uint)(n * 10 + (c - '0'));
                        if (state == 1) // expect other character
                        {
                            if (c == '$') { y += n; x = 0; } // newline
                            else if (c == 'o') for (int i = 0; i < n; i++) BitSet(x++, y); else if (c == 'b') x += n;
                            state = n = 0;
                        }
                    }
                if (screen.height > ph) ph = (uint) screen.height;
            }

            // swap buffers
            for (int i = 0; i < pw * ph; i++) second[i] = pattern[i];
            pBuffer = new OpenCLBuffer<uint>(ocl, pattern);
            sBuffer = new OpenCLBuffer<uint>(ocl, second);

            pBuffer.CopyToDevice();
            sBuffer.CopyToDevice();
        }
        public void Tick()
        {
            GL.Finish();
            timer.Restart();
            // clear the screen
            screen.Clear(0);
            // clear destination pattern
            //Fix 1
            for (int i = 0; i < pw * ph; i++) pattern[i] = 0;
            pBuffer.CopyToDevice();
            //Fix 2
            //pattern = new uint[pw * ph];
            //pBuffer = new OpenCLBuffer<uint>(ocl, pattern);


            //pBuffer.CopyToDevice();
            kernel.SetArgument(0, pBuffer);
            kernel.SetArgument(1, sBuffer);
            kernel.SetArgument(2, pw);
            kernel.SetArgument(3, ph);
            t += 0.1f;

            // execute kernel
            long[] workSize = { 2048, 2048 };
            long[] localSize = { 32, 4 };
          
                kernel.Execute(workSize, localSize);
                // get the data from the device to the host
                pBuffer.CopyFromDevice();
                sBuffer.CopyFromDevice();
            
            for (int i = 0; i < pw * ph; i++) second[i] = pattern[i];

            Render();

            pBuffer.CopyToDevice();
            sBuffer.CopyToDevice();


        }
        public void Render()
        {
            screen.Clear(0);
            for (uint y = 0; y < Math.Min(ph, screen.height); y++) for (uint x = 0; x < screen.width; x++)
                    if (GetBit(x + xoffset, y + yoffset) == 1) screen.Plot((int)x, (int)y, 0xffffff);
            // report performance
            Console.WriteLine("generation " + generation++ + ": " + timer.ElapsedMilliseconds + "ms");
        }
    } // class Game
} // namespace Template
