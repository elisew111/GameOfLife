﻿using Cloo;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Template
{
    class Game
    {
       
        string borderMode = "";
        string GoL_map = "";
        int generation = 0;
        double zoom = 1;
        uint pw, ph, w; // note: pw is in uints; width in bits is 32 this value.
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
                    yoffset = (uint)Math.Min(ph  - screen.height, Math.Max(0, offsetYStart - deltay));
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
        
        void BitSet(uint x, uint y) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }
        

        public void Init()
        {
            
            getInputs();

            kernel = new OpenCLKernel(ocl, borderMode);
            //gekopieerd uit gameoflife

            StreamReader sr = new StreamReader(GoL_map);
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
            if (Keyboard.IsKeyDown(Key.Up)) zoom -= 0.1;
            if (Keyboard.IsKeyDown(Key.Down)) zoom += 0.1;

            GL.Finish();
            timer.Restart();
            // clear the screen
            screen.Clear(0);
            // clear destination pattern
            for (int i = 0; i < pw * ph; i++) pattern[i] = 0;
            pBuffer.CopyToDevice();


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
                {
                    double celX = (x / zoom) - (x % zoom);
                    double celY = (y / zoom) - (y % zoom);
                    if (GetBit(x + xoffset, y + yoffset) == 1) screen.Plot((int)celX, (int)celY, 0xffffff);
                }
            // report performance
            Console.WriteLine("generation " + generation++ + ": " + timer.ElapsedMilliseconds + "ms");
        }

        public void getInputs()
        {
            Console.WriteLine("____________________________________________________________________________");
            Console.WriteLine("Welcome to the Game of Life!");
            
            //Getting choice of map
            Console.WriteLine("Which map would you like to use? (default: c4-orthogonal.rle)");
            Console.WriteLine("1: c4-orthogonal.rle");
            Console.WriteLine("2: turing_js_r.rle");
            Console.WriteLine("3: metapixel-galaxy.rle");
            Console.WriteLine("4: other (risky)");
            GoL_map = Console.ReadLine();

            if (GoL_map == "2") { GoL_map = "../../samples/turing_js_r.rle"; }
            else if (GoL_map == "3") { GoL_map = "../../samples/metapixel-galaxy.rle"; }
            else if (GoL_map == "4") { Console.WriteLine("Enter filepath:"); GoL_map = Console.ReadLine(); }
            else { GoL_map = "../../samples/c4-orthogonal.rle"; }

            //Getting choice of border control
            while (borderMode != "wrap_mode" && borderMode != "dead_mode")
            {
                Console.WriteLine("Border control: Would you like to use wrapping borders (wrap/w) or 'dead' borders (dead/d)?");
                borderMode = Console.ReadLine();
                if (borderMode == "w" || borderMode == "wrap") borderMode = "wrap_mode";
                if (borderMode == "d" || borderMode == "dead") borderMode = "dead_mode";
            }
            Console.WriteLine("Use Up and Down arrows to zoom. Drag the mouse to move the camera.");
        }
    } // class Game
} // namespace Template
