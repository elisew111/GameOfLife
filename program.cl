// #define GLINTEROP


uint GetBit( int x, int y, uint pw, __global uint* second ) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }
void BitSet( int x, int y, uint pw, __global uint* pattern) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }

#ifdef GLINTEROP
__kernel void device_function( write_only image2d_t a, uint pw, uint h)
#else
__kernel void device_function( __global uint* a, uint pw, uint h)
#endif
{
	// adapted from inigo quilez - iq/2013
	int idx = get_global_id( 0 );
	int idy = get_global_id( 1 );
	int id = idx + 512 * idy;
	if (id >= (512 * 512)) return;
	float2 fragCoord = (float2)( (float)idx, (float)idy ), resolution = (float2)( 512, 512 );
	float3 col = (float3)( 0.f, 0.f, 0.f );
	uint w = pw *32;
	uint n = 0;

	

	n = GetBit( idx - 1, idy - 1, pw, a) + GetBit( idx, idy - 1, pw, a) + GetBit( idx + 1, idy - 1, pw, a) + GetBit( idx - 1, idy, pw, a) +
		GetBit( idx + 1, idy, pw, a) + GetBit( idx - 1, idy + 1, pw, a) + GetBit( idx, idy + 1, pw, a) + GetBit( idx + 1, idy + 1, pw, a);
    if ((GetBit( idx, idy, pw, a) == 1 && n ==2) || n == 3) BitSet( idx, idy, pw, a);



	//C# code:
	//uint n = GetBit( x - 1, y - 1 ) + GetBit( x, y - 1 ) + GetBit( x + 1, y - 1 ) + GetBit( x - 1, y ) +
    //                GetBit( x + 1, y ) + GetBit( x - 1, y + 1 ) + GetBit( x, y + 1 ) + GetBit( x + 1, y + 1 );
    //            if ((GetBit( x, y ) == 1 && n ==2) || n == 3) BitSet( x, y );
	// helper function for setting one bit in the pattern buffer

    //    void BitSet( uint x, uint y ) { pattern[y * pw + (x >> 5)] |= 1U << (int)(x & 31); }
    // helper function for getting one bit from the secondary pattern buffer
    //    uint GetBit( uint x, uint y ) { return (second[y * pw + (x >> 5)] >> (int)(x & 31)) & 1U; }



}

