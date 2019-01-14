using Cloo;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Template
{
    class Game
    {
        // when GLInterop is set to true, the fractal is rendered directly to an OpenGL texture
        bool GLInterop = false;
        // load the OpenCL program; this creates the OpenCL context
        static OpenCLProgram ocl = new OpenCLProgram( "../../program.cl" );
        // find the kernel named 'device_function' in the program
        OpenCLKernel kernel = new OpenCLKernel( ocl, "device_function" );
        // create a regular buffer; by default this resides on both the host and the device
        OpenCLBuffer<int> buffer = new OpenCLBuffer<int>( ocl, 512 * 512 );
        // create an OpenGL texture to which OpenCL can send data


        OpenCLImage<int> image = new OpenCLImage<int>( ocl, 512, 512 );
        public Surface screen;
        Stopwatch timer = new Stopwatch();
        int generation = 0;
        // two buffers for the pattern: simulate reads 'second', writes to 'pattern'
        uint[] pattern;
        uint[] second;
        uint pw, ph; // note: pw is in uints; width in bits is 32 this value.
        // helper function for setting one bit in the pattern buffer
        void BitSet(uint x, uint y) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }
        // helper function for getting one bit from the secondary pattern buffer
        uint GetBit(uint x, uint y) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }
        uint xoffset = 0, yoffset = 0;

        float t = 21.5f;
        public void Init()
        {
            // nothing here
        }
        public void Tick()
        {
            //start timer
            timer.Restart();
            // clear the screen
            Simulate();
            // visualize current state (RENDER)
            screen.Clear( 0 );
            for( uint y = 0; y < screen.height; y++ ) for( uint x = 0; x < screen.width; x++ )
                if (GetBit( x + xoffset, y + yoffset ) == 1) screen.Plot( (int) x, (int) y, 0xffffff );
            // report performance
            Console.WriteLine( "generation " + generation++ + ": " + timer.ElapsedMilliseconds + "ms" );
        
        }

        void Simulate()
        {
            // do opencl stuff
            if (GLInterop) kernel.SetArgument(0, image);
            else kernel.SetArgument(0, buffer);
            kernel.SetArgument(1, t);
            t += 0.1f;
            // execute kernel == run the simulation, 1 step
            long[] workSize = { 512, 512 };
            long[] localSize = { 32, 4 };
            if (GLInterop)
            {
                // INTEROP PATH:
                // Use OpenCL to fill an OpenGL texture; this will be used in the
                // Render method to draw a screen filling quad. This is the fastest
                // option, but interop may not be available on older systems.
                // lock the OpenGL texture for use by OpenCL
                //kernel.LockOpenGLObject(image.texBuffer);
                // execute the kernel
                kernel.Execute(workSize, localSize);
                // unlock the OpenGL texture so it can be used for drawing a quad
                //kernel.UnlockOpenGLObject(image.texBuffer);
            }
            else
            {
                // NO INTEROP PATH:
                // Use OpenCL to fill a C# pixel array, encapsulated in an
                // OpenCLBuffer<int> object (buffer). After filling the buffer, it
                // is copied to the screen surface, so the template code can show
                // it in the window.
                // execute the kernel
                kernel.Execute(workSize, localSize);
                // get the data from the device to the host
                buffer.CopyFromDevice();
                // plot pixels using the data on the host
                for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
                    {
                        screen.pixels[x + y * screen.width] = buffer[x + y * 512];
                    }
            }
        }
        /*public void Render()
        {
            // use OpenGL to draw a quad using the texture that was filled by OpenCL
            if (GLInterop)
            {
                GL.LoadIdentity();
                GL.BindTexture( TextureTarget.Texture2D, image.OpenGLTextureID );
                GL.Begin( PrimitiveType.Quads );
                GL.TexCoord2( 0.0f, 1.0f ); GL.Vertex2( -1.0f, -1.0f );
                GL.TexCoord2( 1.0f, 1.0f ); GL.Vertex2(  1.0f, -1.0f );
                GL.TexCoord2( 1.0f, 0.0f ); GL.Vertex2(  1.0f,  1.0f );
                GL.TexCoord2( 0.0f, 0.0f ); GL.Vertex2( -1.0f,  1.0f );
                GL.End();
            }
        }*/
    } // class Game
} // namespace Template

